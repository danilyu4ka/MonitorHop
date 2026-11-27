param(
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$IdentityName,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$Publisher,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$PublisherDisplayName,
    [string]$AppDisplayName = 'MonitorHop',
    [string]$Version,
    [string]$OutputDirectory,
    [switch]$KeepStaging
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'artifacts\store' }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

if (-not $Version) {
    $Version = (& dotnet msbuild (Join-Path $projectRoot 'MonitorHop\MonitorHop.csproj') -getProperty:Version).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not read the application version.' }
}
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw 'Version must have three or four numeric parts, for example 1.5.4.'
}
$packageVersion = if ($Version.Split('.').Length -eq 3) { "$Version.0" } else { $Version }
if ($IdentityName -notmatch '^[A-Za-z0-9][A-Za-z0-9.\-]*$') {
    throw 'IdentityName contains invalid characters. Copy the exact Name from Partner Center.'
}
if ($Publisher -notmatch '^CN=.+') {
    throw 'Publisher must be the full Publisher value from Partner Center, starting with CN=.'
}

$stage = Join-Path $outputRoot ("staging-" + [guid]::NewGuid().ToString('N'))
$stageFull = [IO.Path]::GetFullPath($stage)
if (-not $stageFull.StartsWith($outputRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Refusing to use a staging path outside the output directory.'
}

function Write-ResizedPng([string]$sourcePath, [string]$targetPath, [int]$width, [int]$height) {
    Add-Type -AssemblyName System.Drawing
    $source = [Drawing.Image]::FromFile($sourcePath)
    $target = [Drawing.Bitmap]::new($width, $height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [Drawing.Graphics]::FromImage($target)
        try {
            $graphics.Clear([Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.DrawImage($source, 0, 0, $width, $height)
        } finally { $graphics.Dispose() }
        $target.Save($targetPath, [Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $target.Dispose()
        $source.Dispose()
    }
}

try {
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    $project = Join-Path $projectRoot 'MonitorHop\MonitorHop.csproj'
    & dotnet publish $project -c Release -r win-x64 --self-contained true '-p:StoreBuild=true' "-p:Version=$Version" '-p:PublishSingleFile=false' '-p:PublishTrimmed=false' '-p:DebugType=None' '-p:DebugSymbols=false' -o $stage
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
    if (-not (Test-Path (Join-Path $stage 'MonitorHop.exe'))) { throw 'Published executable is missing.' }

    $assetDirectory = Join-Path $stage 'Assets'
    New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null
    $sourceIcon = Join-Path $projectRoot 'assets\monitorhop-icon.png'
    Write-ResizedPng $sourceIcon (Join-Path $assetDirectory 'StoreLogo.png') 50 50
    Write-ResizedPng $sourceIcon (Join-Path $assetDirectory 'Square44x44Logo.png') 44 44
    Write-ResizedPng $sourceIcon (Join-Path $assetDirectory 'Square150x150Logo.png') 150 150
    Write-ResizedPng $sourceIcon (Join-Path $outputRoot 'Store-listing-logo-300x300.png') 300 300

    [xml]$manifest = Get-Content (Join-Path $projectRoot 'store\AppxManifest.template.xml') -Raw
    $ns = [Xml.XmlNamespaceManager]::new($manifest.NameTable)
    $ns.AddNamespace('m', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $identity = $manifest.SelectSingleNode('/m:Package/m:Identity', $ns)
    $identity.SetAttribute('Name', $IdentityName)
    $identity.SetAttribute('Publisher', $Publisher)
    $identity.SetAttribute('Version', $packageVersion)
    $manifest.SelectSingleNode('/m:Package/m:Properties/m:PublisherDisplayName', $ns).InnerText = $PublisherDisplayName
    $manifest.SelectSingleNode('/m:Package/m:Properties/m:DisplayName', $ns).InnerText = $AppDisplayName
    $visual = $manifest.SelectSingleNode('/m:Package/m:Applications/m:Application/*[local-name()="VisualElements"]', $ns)
    $visual.SetAttribute('DisplayName', $AppDisplayName)
    $manifestPath = Join-Path $stage 'AppxManifest.xml'
    $manifest.Save($manifestPath)

    $kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $makeAppx = Get-ChildItem $kits -Filter makeappx.exe -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -match '[\\/]x64$' } |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    if (-not $makeAppx) { throw 'Windows SDK MakeAppx.exe was not found. Install the Windows 10/11 SDK.' }

    $packagePath = Join-Path $outputRoot ("MonitorHop_${packageVersion}_x64.msix")
    & $makeAppx pack /d $stage /p $packagePath /o
    if ($LASTEXITCODE -ne 0) { throw 'MakeAppx pack failed.' }
    # MakeAppx validates the manifest and package while packing.
    $hash = (Get-FileHash $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path $packagePath -Leaf)" | Set-Content (Join-Path $outputRoot 'SHA256SUMS.txt') -Encoding ascii
    Write-Host "Store upload package: $packagePath"
    Write-Host 'The package is unsigned; upload it to Partner Center. Do not distribute it directly.'
    if ($KeepStaging) { Write-Host "Development registration layout: $stage" }
} finally {
    if (-not $KeepStaging -and (Test-Path -LiteralPath $stageFull)) {
        if (-not $stageFull.StartsWith($outputRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Refusing to delete a staging path outside the output directory.'
        }
        Remove-Item -LiteralPath $stageFull -Recurse -Force
    }
}
