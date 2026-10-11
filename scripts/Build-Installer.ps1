[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $root 'Mimo\Mimo.csproj'
$scriptPath = Join-Path $root 'installer\Mimo.iss'
$publishPath = Join-Path $root 'publish\installer\app'
$setupPath = Join-Path $root 'publish\Mimo-Setup.exe'

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Mimo.csproj contains an invalid Version: $version"
}

$candidatePaths = @(
    [Environment]::GetEnvironmentVariable('INNO_SETUP_ISCC')
    (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\Inno Setup 6\ISCC.exe')
    (Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Inno Setup 6\ISCC.exe')
    (Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'Inno Setup 6\ISCC.exe')
)
$compiler = $candidatePaths | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } | Select-Object -First 1
if (-not $compiler) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        $compiler = $command.Source
    }
}
if (-not $compiler) {
    throw 'Inno Setup 6 compiler (ISCC.exe) was not found. Install JRSoftware.InnoSetup with winget.'
}

Write-Host "Publishing Mimo $version..."
& dotnet publish $projectPath -c Release -r win-x64 --self-contained true `
    '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' `
    '-p:DebugType=None' -o $publishPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host 'Compiling Windows installer...'
& $compiler "/DAppVersion=$version" $scriptPath
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
}
if (-not (Test-Path -LiteralPath $setupPath -PathType Leaf)) {
    throw "Installer output missing: $setupPath"
}

Write-Host "Created $setupPath"
