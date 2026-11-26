Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$assemblyPath = (Resolve-Path (Join-Path $PSScriptRoot '..\MonitorHop\bin\Release\net10.0-windows\MonitorHop.dll')).Path
$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$flags = [System.Reflection.BindingFlags]'Instance,NonPublic'

$settingsType = $assembly.GetType('MonitorHop.AppSettings', $true)
$settings = [System.Activator]::CreateInstance($settingsType)
$settingsFormType = $assembly.GetType('MonitorHop.SettingsForm', $true)
$settingsForm = [System.Activator]::CreateInstance($settingsFormType, @($settings))

$output = Join-Path $PSScriptRoot '..\artifacts'
New-Item -ItemType Directory -Force $output | Out-Null
try {
    $settingsForm.Show()
    [System.Windows.Forms.Application]::DoEvents()
    $settingsBitmap = [System.Drawing.Bitmap]::new($settingsForm.Width, $settingsForm.Height)
    try {
        $settingsForm.DrawToBitmap($settingsBitmap, [System.Drawing.Rectangle]::new(0, 0, $settingsBitmap.Width, $settingsBitmap.Height))
        $settingsBitmap.Save((Join-Path $output 'settings-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $languages = $settingsFormType.GetField('_language', $flags).GetValue($settingsForm)
        $languages.SelectedIndex = 1
        [System.Windows.Forms.Application]::DoEvents()
        $settingsForm.DrawToBitmap($settingsBitmap, [System.Drawing.Rectangle]::new(0, 0, $settingsBitmap.Width, $settingsBitmap.Height))
        $settingsBitmap.Save((Join-Path $output 'settings-en-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $languages.SelectedIndex = 2
        [System.Windows.Forms.Application]::DoEvents()
        $settingsForm.DrawToBitmap($settingsBitmap, [System.Drawing.Rectangle]::new(0, 0, $settingsBitmap.Width, $settingsBitmap.Height))
        $settingsBitmap.Save((Join-Path $output 'settings-et-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $languages.SelectedIndex = 0
        $settingsFormType.GetField('_advancedToggle', $flags).GetValue($settingsForm).PerformClick()
        [System.Windows.Forms.Application]::DoEvents()
        $settingsForm.DrawToBitmap($settingsBitmap, [System.Drawing.Rectangle]::new(0, 0, $settingsBitmap.Width, $settingsBitmap.Height))
        $settingsBitmap.Save((Join-Path $output 'settings-advanced-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $settingsFormType.GetField('_advanced', $flags).GetValue($settingsForm).Text = 'example-app.exe'
        $settingsFormType.GetField('_addNames', $flags).GetValue($settingsForm).PerformClick()
        $ignoredNames = $settingsFormType.GetField('_ignoredNames', $flags).GetValue($settingsForm)
        $items = $settingsFormType.GetField('_processes', $flags).GetValue($settingsForm).Items
        $unexpectedRow = @($items | Where-Object { $_.Text -eq 'example-app.exe' })
        if (-not $ignoredNames.Contains('example-app') -or $unexpectedRow.Count -ne 0) {
            throw 'Advanced name or visible-process filtering did not work.'
        }
    } finally { $settingsBitmap.Dispose() }
} finally { $settingsForm.Dispose() }

$overlayType = $assembly.GetType('MonitorHop.OverlayForm', $true)
$overlay = [System.Activator]::CreateInstance($overlayType)
try {
    $overlayType.GetMethod('SetLanguage').Invoke($overlay, @('en')) | Out-Null
    $overlayType.GetField('_expanded', $flags).SetValue($overlay, $true)
    $overlayType.GetField('_source', $flags).SetValue($overlay, [System.Windows.Forms.Screen]::PrimaryScreen)
    $overlay.ClientSize = [System.Drawing.Size]::new(300, 277)
    $overlay.Show()
    [System.Windows.Forms.Application]::DoEvents()
    $overlayBitmap = [System.Drawing.Bitmap]::new($overlay.Width, $overlay.Height)
    try {
        $overlay.DrawToBitmap($overlayBitmap, [System.Drawing.Rectangle]::new(0, 0, $overlayBitmap.Width, $overlayBitmap.Height))
        $overlayBitmap.Save((Join-Path $output 'overlay-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $overlayType.GetMethod('SetDisplayMode').Invoke($overlay, @($true)) | Out-Null
        [System.Windows.Forms.Application]::DoEvents()
        $overlay.DrawToBitmap($overlayBitmap, [System.Drawing.Rectangle]::new(0, 0, $overlayBitmap.Width, $overlayBitmap.Height))
        $overlayBitmap.Save((Join-Path $output 'overlay-map-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $tiles = $overlayType.GetMethod('MonitorTiles', $flags).Invoke($overlay, @())
        if ($tiles.Count -ne [System.Windows.Forms.Screen]::AllScreens.Count) {
            throw 'Monitor map does not contain every active screen.'
        }
        for ($i = 0; $i -lt $tiles.Count; $i++) {
            $rect = $tiles[$i].Item2
            $center = [System.Drawing.Point]::new($rect.Left + [int]($rect.Width / 2), $rect.Top + [int]($rect.Height / 2))
            $hit = $overlayType.GetMethod('HitTest', $flags).Invoke($overlay, @($center))
            if ($hit -ne (100 + $i)) { throw "Monitor map hit test failed for screen $i." }
        }
    } finally { $overlayBitmap.Dispose() }
    $overlayType.GetField('_expanded', $flags).SetValue($overlay, $false)
    $overlay.ClientSize = [System.Drawing.Size]::new(58, 48)
    [System.Windows.Forms.Application]::DoEvents()
    $buttonBitmap = [System.Drawing.Bitmap]::new($overlay.Width, $overlay.Height)
    try {
        $overlay.DrawToBitmap($buttonBitmap, [System.Drawing.Rectangle]::new(0, 0, $buttonBitmap.Width, $buttonBitmap.Height))
        $buttonBitmap.Save((Join-Path $output 'button-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $buttonBitmap.Dispose() }
} finally { $overlay.Dispose() }

Write-Output "Saved previews to $output"
