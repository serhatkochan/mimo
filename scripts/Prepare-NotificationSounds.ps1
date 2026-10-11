[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$soundRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'Mimo\Assets\Sounds'
$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source
$soundNames = @('short-break-start', 'short-break-end', 'long-break-start', 'long-break-end')

foreach ($soundName in $soundNames) {
    $sourcePath = Join-Path $soundRoot "Sources\$soundName.mp3"
    $outputPath = Join-Path $soundRoot "$soundName.wav"

    # Measure first so the second pass uses the same loudness target for every clip.
    $ErrorActionPreference = 'Continue'
    try {
        $measurementLog = (& $ffmpeg -hide_banner -nostdin -i $sourcePath `
            -af 'loudnorm=I=-14:TP=-1:LRA=7:print_format=json' -f null NUL 2>&1 |
            ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
        $measurementExitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = 'Stop' }
    if ($measurementExitCode -ne 0) { throw "Audio measurement failed: $sourcePath" }
    $json = [regex]::Match($measurementLog, '(?s)\{\s*"input_i".*?\}').Value
    if (-not $json) { throw "Loudness measurements missing: $sourcePath" }
    $measurement = $json | ConvertFrom-Json
    $filter = 'loudnorm=I=-14:TP=-1:LRA=7:linear=true' +
        ":measured_I=$($measurement.input_i):measured_TP=$($measurement.input_tp)" +
        ":measured_LRA=$($measurement.input_lra):measured_thresh=$($measurement.input_thresh)" +
        ":offset=$($measurement.target_offset)"

    & $ffmpeg -hide_banner -loglevel error -nostdin -y -i $sourcePath `
        -af $filter -ar 44100 -ac 1 -c:a pcm_s16le -map_metadata -1 $outputPath
    if ($LASTEXITCODE -ne 0) { throw "Audio conversion failed: $sourcePath" }
    Write-Host "Prepared $soundName.wav"
}

# A brief two-tone tap gives feedback without repeating a spoken phase announcement.
& $ffmpeg -hide_banner -loglevel error -nostdin -y -f lavfi `
    -i 'aevalsrc=0.65*sin(2*PI*880*t)+0.2*sin(2*PI*1760*t):s=44100:d=0.09' `
    -af 'afade=t=in:d=0.005,afade=t=out:st=0.025:d=0.065' `
    -c:a pcm_s16le -map_metadata -1 (Join-Path $soundRoot 'control.wav')
if ($LASTEXITCODE -ne 0) { throw 'Control sound generation failed.' }
Write-Host 'Prepared control.wav'
