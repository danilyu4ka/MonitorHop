Add-Type -AssemblyName System.Drawing

$sourcePath = Join-Path $PSScriptRoot '..\assets\monitorhop.png'
$pngPath = Join-Path $PSScriptRoot '..\assets\monitorhop-icon.png'
$icoPath = Join-Path $PSScriptRoot '..\assets\monitorhop.ico'

$source = [System.Drawing.Bitmap]::new((Resolve-Path $sourcePath).Path)
try {
    $side = [Math]::Min($source.Width, $source.Height)
    $crop = [System.Drawing.Rectangle]::new([int](($source.Width - $side) / 2), [int](($source.Height - $side) / 2), $side, $side)
    $square = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($square)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0, 0, 512, 512), $crop, [System.Drawing.GraphicsUnit]::Pixel)
        } finally { $graphics.Dispose() }
        $square.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

        $sizes = @(16, 24, 32, 48, 64, 256)
        $images = @()
        foreach ($size in $sizes) {
            $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                $g = [System.Drawing.Graphics]::FromImage($bitmap)
                try {
                    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $g.DrawImage($square, 0, 0, $size, $size)
                } finally { $g.Dispose() }
                $memory = [System.IO.MemoryStream]::new()
                try {
                    $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
                    $images += ,$memory.ToArray()
                } finally { $memory.Dispose() }
            } finally { $bitmap.Dispose() }
        }

        $file = [System.IO.File]::Create($icoPath)
        try {
            $writer = [System.IO.BinaryWriter]::new($file)
            $writer.Write([uint16]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]$sizes.Count)
            $offset = 6 + 16 * $sizes.Count
            for ($i = 0; $i -lt $sizes.Count; $i++) {
                $writer.Write([byte]($sizes[$i] -band 255))
                $writer.Write([byte]($sizes[$i] -band 255))
                $writer.Write([byte]0)
                $writer.Write([byte]0)
                $writer.Write([uint16]1)
                $writer.Write([uint16]32)
                $writer.Write([uint32]$images[$i].Length)
                $writer.Write([uint32]$offset)
                $offset += $images[$i].Length
            }
            foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
            $writer.Flush()
        } finally { $file.Dispose() }
    } finally { $square.Dispose() }
} finally { $source.Dispose() }

Write-Output "Created $pngPath and $icoPath"
