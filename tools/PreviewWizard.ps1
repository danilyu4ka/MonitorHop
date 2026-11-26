Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$assemblyPath = (Resolve-Path (Join-Path $PSScriptRoot '..\MonitorHop\bin\Release\net10.0-windows\MonitorHop.dll')).Path
$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$type = $assembly.GetType('MonitorHop.SetupWizardForm', $true)
$flags = [System.Reflection.BindingFlags]'Instance,NonPublic'
$wizard = [System.Activator]::CreateInstance($type)
$output = Join-Path $PSScriptRoot '..\docs\images'
New-Item -ItemType Directory -Force $output | Out-Null

try {
    $wizard.Show()
    [System.Windows.Forms.Application]::DoEvents()
    $language = $type.GetField('_language', $flags).GetValue($wizard)
    $language.SelectedIndex = 0
    $pages = @(
        @{ Number = 0; Name = 'setup-language.png' },
        @{ Number = 1; Name = 'setup-welcome.png' },
        @{ Number = 2; Name = 'setup-options.png' },
        @{ Number = 4; Name = 'setup-finish.png' }
    )
    foreach ($page in $pages) {
        $type.GetMethod('PreviewPage', $flags).Invoke($wizard, @([int]$page.Number)) | Out-Null
        [System.Windows.Forms.Application]::DoEvents()
        $bitmap = [System.Drawing.Bitmap]::new($wizard.Width, $wizard.Height)
        try {
            $wizard.DrawToBitmap($bitmap, [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height))
            $bitmap.Save((Join-Path $output $page.Name), [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
    }
} finally { $wizard.Dispose() }

Write-Output "Saved wizard previews to $output"
