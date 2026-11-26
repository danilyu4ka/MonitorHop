# Rebuild the illustrated README assets with Windows' built-in drawing and GIF codecs.
Add-Type -AssemblyName System.Drawing, PresentationCore, WindowsBase

$imageDirectory = Join-Path $PSScriptRoot '..\docs\images'
New-Item -ItemType Directory -Force -Path $imageDirectory | Out-Null
$gifPath = Join-Path $imageDirectory 'monitor-hop-demo.gif'
$stillPath = Join-Path $imageDirectory 'monitor-map-example.png'

function Color([string] $hex) {
    [System.Drawing.ColorTranslator]::FromHtml($hex)
}

function RoundedPath([float] $x, [float] $y, [float] $width, [float] $height, [float] $radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    $path
}

function Box($graphics, [float] $x, [float] $y, [float] $width, [float] $height,
    [float] $radius, [string] $fill, [string] $border, [float] $borderWidth = 1) {
    $path = RoundedPath $x $y $width $height $radius
    $brush = [System.Drawing.SolidBrush]::new((Color $fill))
    $pen = [System.Drawing.Pen]::new((Color $border), $borderWidth)
    try {
        $graphics.FillPath($brush, $path)
        $graphics.DrawPath($pen, $path)
    } finally {
        $brush.Dispose()
        $pen.Dispose()
        $path.Dispose()
    }
}

function Label($graphics, [string] $value, [float] $x, [float] $y,
    [float] $size, [string] $color, [bool] $bold = $false) {
    $style = if ($bold) { [System.Drawing.FontStyle]::Bold } else { [System.Drawing.FontStyle]::Regular }
    $font = [System.Drawing.Font]::new('Segoe UI', $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = [System.Drawing.SolidBrush]::new((Color $color))
    try { $graphics.DrawString($value, $font, $brush, $x, $y) }
    finally { $brush.Dispose(); $font.Dispose() }
}

function Draw-Monitor($graphics, [int] $number, [int] $x, [int] $y, [int] $width,
    [int] $height, [bool] $selected) {
    $border = if ($selected) { '#55D9F5' } else { '#4C6A80' }
    $weight = if ($selected) { 3 } else { 2 }
    Box $graphics ($x + 5) ($y + 6) $width $height 12 '#0A1422' '#0A1422'
    Box $graphics $x $y $width $height 12 '#1C3043' $border $weight
    $line = [System.Drawing.Pen]::new((Color '#38546A'), 1)
    try { $graphics.DrawLine($line, $x + 1, $y + 35, $x + $width - 1, $y + 35) }
    finally { $line.Dispose() }
    Label $graphics "MONITOR $number" ($x + 16) ($y + 10) 13 '#BFD3DF' $true
    Label $graphics "$number" ($x + 16) ($y + $height - 47) 31 '#6B8CA3' $true
}

function Draw-Window($graphics, [float] $x, [float] $y, [bool] $iconActive) {
    Box $graphics ($x + 5) ($y + 6) 160 92 8 '#0A1422' '#0A1422'
    Box $graphics $x $y 160 92 8 '#EEF5F8' '#75A6BA' 2
    Box $graphics ($x + 2) ($y + 2) 156 29 6 '#D9E9EF' '#D9E9EF'
    Label $graphics 'Project notes' ($x + 12) ($y + 8) 12 '#213A4C' $true
    Label $graphics '—  □  ×' ($x + 103) ($y + 7) 13 '#29475B'
    foreach ($row in 0..2) {
        Box $graphics ($x + 15) ($y + 41 + $row * 14) (120 - $row * 20) 5 2 '#AFC9D5' '#AFC9D5'
    }
    if ($iconActive) {
        Box $graphics ($x + 104) ($y - 7) 33 28 7 '#19415B' '#67DAF1' 2
        Label $graphics '⇄' ($x + 110) ($y - 8) 22 '#DDFBFF' $true
    }
}

function Draw-Cursor($graphics, [float] $x, [float] $y) {
    $points = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($x, $y),
        [System.Drawing.PointF]::new($x + 2, $y + 18),
        [System.Drawing.PointF]::new($x + 7, $y + 13),
        [System.Drawing.PointF]::new($x + 12, $y + 24),
        [System.Drawing.PointF]::new($x + 17, $y + 21),
        [System.Drawing.PointF]::new($x + 12, $y + 11),
        [System.Drawing.PointF]::new($x + 20, $y + 10)
    )
    $brush = [System.Drawing.SolidBrush]::new((Color '#FFFFFF'))
    $pen = [System.Drawing.Pen]::new((Color '#102335'), 2)
    try { $graphics.FillPolygon($brush, $points); $graphics.DrawPolygon($pen, $points) }
    finally { $brush.Dispose(); $pen.Dispose() }
}

function Draw-MapMenu($graphics, [bool] $targetSelected) {
    Box $graphics 267 150 172 131 11 '#10283C' '#74D4EC' 2
    Label $graphics 'Choose a monitor' 279 159 13 '#F1FCFF' $true
    Box $graphics 283 213 43 31 5 '#244C65' '#598DA7'
    Box $graphics 339 186 59 38 5 $(if ($targetSelected) { '#286A84' } else { '#244C65' }) $(if ($targetSelected) { '#72E5FF' } else { '#598DA7' }) 2
    Box $graphics 400 228 26 28 5 '#244C65' '#598DA7'
    Label $graphics '1' 298 215 17 '#E2F7FF' $true
    Label $graphics '2' 360 192 20 '#E2F7FF' $true
    Label $graphics '3' 408 231 15 '#E2F7FF' $true
    Label $graphics 'Actual screen layout' 281 255 11 '#A5C1D0'
}

function Draw-Frame([int] $stage) {
    $bitmap = [System.Drawing.Bitmap]::new(960, 520)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear((Color '#0D1B2B'))
    Box $graphics 16 15 928 490 20 '#14263A' '#36556A' 2
    Label $graphics 'MonitorHop' 46 30 29 '#ECFAFF' $true
    Label $graphics 'Move windows across an uneven monitor layout' 48 68 15 '#9BB9CA'
    Draw-Monitor $graphics 1 70 229 320 190 $false
    Draw-Monitor $graphics 2 440 86 340 190 ($stage -ge 3)
    Draw-Monitor $graphics 3 678 309 240 151 $false

    if ($stage -ge 4 -and $stage -le 8) {
        $pathPen = [System.Drawing.Pen]::new((Color '#6FDCED'), 3)
        $pathPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
        try { $graphics.DrawBezier($pathPen, 248, 312, 315, 244, 409, 196, 508, 173) }
        finally { $pathPen.Dispose() }
    }

    $progress = if ($stage -lt 4) { 0 } elseif ($stage -gt 8) { 1 } else { ($stage -3) / 5.0 }
    $windowX = 105 + 375 * $progress
    $windowY = 299 - 149 * $progress
    Draw-Window $graphics $windowX $windowY ($stage -le 3)
    if ($stage -ge 2 -and $stage -le 3) { Draw-MapMenu $graphics ($stage -eq 3) }
    if ($stage -le 1) { Draw-Cursor $graphics 224 288 }
    if ($stage -eq 3) { Draw-Cursor $graphics 367 203 }

    Box $graphics 70 466 850 27 7 '#1B3549' '#1B3549'
    $steps = @('1  Hover over the icon', '2  Choose monitor 2', '3  Window moves to the same relative position')
    $positions = @(86, 310, 536)
    for ($i = 0; $i -lt 3; $i++) {
        $active = switch ($i) { 0 { $stage -le 1 } 1 { $stage -ge 2 -and $stage -le 3 } 2 { $stage -ge 4 } }
        Label $graphics $steps[$i] $positions[$i] 469 12 $(if ($active) { '#74E6F5' } else { '#A6C0CE' }) $active
    }
    $graphics.Dispose()
    $bitmap
}

$encoder = [System.Windows.Media.Imaging.GifBitmapEncoder]::new()
$durations = [int[]]@(70, 45, 65, 55, 13, 13, 13, 13, 13, 55, 65, 65)
for ($stage = 0; $stage -lt $durations.Length; $stage++) {
    $bitmap = Draw-Frame $stage
    try {
        if ($stage -eq 3) { $bitmap.Save($stillPath, [System.Drawing.Imaging.ImageFormat]::Png) }
        $memory = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
            $memory.Position = 0
            $frame = [System.Windows.Media.Imaging.BitmapFrame]::Create($memory,
                [System.Windows.Media.Imaging.BitmapCreateOptions]::None,
                [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
            $encoder.Frames.Add($frame)
        } finally { $memory.Dispose() }
    } finally { $bitmap.Dispose() }
}

$stream = [System.IO.File]::Create($gifPath)
try { $encoder.Save($stream) } finally { $stream.Dispose() }

# WPF emits zero GIF frame delays and no loop extension. Set both explicitly.
$bytes = [System.IO.File]::ReadAllBytes($gifPath)
$frameIndex = 0
for ($i = 0; $i -lt $bytes.Length - 7; $i++) {
    if ($bytes[$i] -eq 0x21 -and $bytes[$i + 1] -eq 0xF9 -and
        $bytes[$i + 2] -eq 0x04 -and $bytes[$i + 7] -eq 0x00) {
        if ($frameIndex -ge $durations.Length) { throw 'Unexpected extra GIF frame.' }
        $delay = $durations[$frameIndex]
        $bytes[$i + 4] = [byte]($delay -band 0xFF)
        $bytes[$i + 5] = [byte](($delay -shr 8) -band 0xFF)
        $frameIndex++
        $i += 7
    }
}
if ($frameIndex -ne $durations.Length) { throw "Expected $($durations.Length) GIF frames; found $frameIndex." }

$globalTableSize = if (($bytes[10] -band 0x80) -ne 0) {
    3 * [int][Math]::Pow(2, ($bytes[10] -band 7) + 1)
} else { 0 }
$insertAt = 13 + $globalTableSize
$loop = [byte[]]@(0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50,
    0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00)
$result = [byte[]]::new($bytes.Length + $loop.Length)
[Array]::Copy($bytes, 0, $result, 0, $insertAt)
[Array]::Copy($loop, 0, $result, $insertAt, $loop.Length)
[Array]::Copy($bytes, $insertAt, $result, $insertAt + $loop.Length, $bytes.Length - $insertAt)
[System.IO.File]::WriteAllBytes($gifPath, $result)

$checkStream = [System.IO.File]::OpenRead($gifPath)
try {
    $decoder = [System.Windows.Media.Imaging.GifBitmapDecoder]::new($checkStream,
        [System.Windows.Media.Imaging.BitmapCreateOptions]::None,
        [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    if ($decoder.Frames.Count -ne $durations.Length) { throw 'GIF frame count changed after encoding.' }
    for ($i = 0; $i -lt $durations.Length; $i++) {
        if ($decoder.Frames[$i].Metadata.GetQuery('/grctlext/Delay') -ne $durations[$i]) {
            throw "GIF frame $i has the wrong delay."
        }
    }
} finally { $checkStream.Dispose() }

Write-Output "Created $gifPath and $stillPath"
