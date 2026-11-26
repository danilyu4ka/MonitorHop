Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$assemblyPath = (Resolve-Path (Join-Path $PSScriptRoot '..\MonitorHop\bin\Release\net10.0-windows\MonitorHop.dll')).Path
$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$flags = [System.Reflection.BindingFlags]'Instance,NonPublic'

$settingsType = $assembly.GetType('MonitorHop.AppSettings', $true)
$invalid = [System.Activator]::CreateInstance($settingsType)
$invalid.HotkeyModifiers = ''
$invalid.IgnoredProcesses.Add($null)
$invalid.IgnoredProcesses.Add(' ')
$invalid.IgnoredProcesses.Add('C:\Apps\game.exe')
$invalid.IgnoredProcesses.Add('game.exe')
if (-not $invalid.IsIgnored('game')) { throw 'Ignoring a process must tolerate null entries.' }
$staticFlags = [System.Reflection.BindingFlags]'Static,NonPublic'
$normalize = $settingsType.GetMethod('NormalizeLoaded', $staticFlags)
$normalize.Invoke($null, @($invalid)) | Out-Null
if ($invalid.HotkeyModifiers -ne 'Ctrl+Shift+Alt' -or $invalid.IgnoredProcesses.Count -ne 1) {
    throw 'Invalid hotkeys or ignored process names were not normalized.'
}
foreach ($badModifier in @('Win', $null)) {
    $invalid.HotkeyModifiers = $badModifier
    $normalize.Invoke($null, @($invalid)) | Out-Null
    if ($invalid.HotkeyModifiers -ne 'Ctrl+Shift+Alt') { throw 'Unsafe hotkey modifier was accepted.' }
}
$settings = [System.Activator]::CreateInstance($settingsType)
$settings.OverlayOffsetX = 73
$settings.OverlayOffsetY = -25
$settingsFormType = $assembly.GetType('MonitorHop.SettingsForm', $true)
$settingsForm = [System.Activator]::CreateInstance($settingsFormType, @($settings))

$output = Join-Path $PSScriptRoot '..\artifacts'
New-Item -ItemType Directory -Force $output | Out-Null
$legacySettingsPath = Join-Path $output 'legacy-settings\settings.json'
$currentSettingsPath = Join-Path $output 'current-settings\settings.json'
New-Item -ItemType Directory -Force (Split-Path $legacySettingsPath) | Out-Null
Remove-Item -LiteralPath $currentSettingsPath -ErrorAction SilentlyContinue
Set-Content -LiteralPath $legacySettingsPath -Value '{"Language":"et"}' -Encoding utf8
$migrate = $settingsType.GetMethod('MigrateSettingsFile', $staticFlags)
$migrationArguments = [object[]]::new(2)
$migrationArguments[0] = [string]$legacySettingsPath
$migrationArguments[1] = [string]$currentSettingsPath
$migrate.Invoke($null, $migrationArguments) | Out-Null
if (-not (Test-Path $currentSettingsPath) -or (Test-Path $legacySettingsPath) -or
    (Test-Path (Split-Path $legacySettingsPath)) -or
    (Get-Content $currentSettingsPath -Raw | ConvertFrom-Json).Language -ne 'et') {
    throw 'Legacy settings were not moved to the new directory.'
}
New-Item -ItemType Directory -Force (Split-Path $legacySettingsPath) | Out-Null
Set-Content -LiteralPath $legacySettingsPath -Value '{"Language":"ru"}' -Encoding utf8
$migrate.Invoke($null, $migrationArguments) | Out-Null
if ((Get-Content $currentSettingsPath -Raw | ConvertFrom-Json).Language -ne 'et' -or
    -not (Test-Path $legacySettingsPath)) {
    throw 'Migration replaced existing settings.'
}
$settingsCheckPath = Join-Path $output 'settings-save-check.json'
$saveTo = $settingsType.GetMethod('SaveTo', $flags)
$saveArguments = [object[]]::new(1)
$saveArguments[0] = [string]$settingsCheckPath
$saveTo.Invoke($settings, $saveArguments) | Out-Null
$settings.Language = 'et'
$saveTo.Invoke($settings, $saveArguments) | Out-Null
if ((Get-Content $settingsCheckPath -Raw | ConvertFrom-Json).Language -ne 'et' -or
    (Test-Path ($settingsCheckPath + '.tmp'))) {
    throw 'Replacing the settings file failed or left a temporary file.'
}
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
        $settingsFormType.GetField('_save', $flags).GetValue($settingsForm).PerformClick()
        $result = $settingsFormType.GetProperty('Result').GetValue($settingsForm)
        if ($result.OverlayOffsetX -ne 73 -or $result.OverlayOffsetY -ne -25) {
            throw 'Saving settings lost the dragged overlay position.'
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
