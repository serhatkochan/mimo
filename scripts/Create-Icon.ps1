Add-Type -AssemblyName System.Drawing

$assetDirectory = Join-Path $PSScriptRoot '..\Mimo\Assets'
New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null
$icoPath = Join-Path $assetDirectory 'mimo.ico'

$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$rounded = New-Object System.Drawing.Drawing2D.GraphicsPath
$rounded.AddArc(10, 10, 68, 68, 180, 90)
$rounded.AddArc(178, 10, 68, 68, 270, 90)
$rounded.AddArc(178, 178, 68, 68, 0, 90)
$rounded.AddArc(10, 178, 68, 68, 90, 90)
$rounded.CloseFigure()
$peach = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#F7B580'))
$ink = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#242331'))
$graphics.FillPath($peach, $rounded)

$sparkle = New-Object System.Drawing.Drawing2D.GraphicsPath
$points = [System.Drawing.PointF[]]@(
    [System.Drawing.PointF]::new(128, 43),
    [System.Drawing.PointF]::new(145, 102),
    [System.Drawing.PointF]::new(205, 128),
    [System.Drawing.PointF]::new(145, 151),
    [System.Drawing.PointF]::new(128, 212),
    [System.Drawing.PointF]::new(108, 151),
    [System.Drawing.PointF]::new(51, 128),
    [System.Drawing.PointF]::new(108, 102)
)
$sparkle.AddPolygon($points)
$graphics.FillPath($ink, $sparkle)
$pngStream = New-Object System.IO.MemoryStream
$bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $pngStream.ToArray()
$stream = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($stream)
$writer.Write([int16]0)
$writer.Write([int16]1)
$writer.Write([int16]1)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([int16]1)
$writer.Write([int16]32)
$writer.Write([int]$png.Length)
$writer.Write([int]22)
$writer.Write($png)
$writer.Flush()
[System.IO.File]::WriteAllBytes($icoPath, $stream.ToArray())

$writer.Dispose()
$stream.Dispose()
$pngStream.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
$rounded.Dispose()
$sparkle.Dispose()
$peach.Dispose()
$ink.Dispose()

Write-Output $icoPath
