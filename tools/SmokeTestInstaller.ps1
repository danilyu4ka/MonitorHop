# Runs only on a disposable GitHub-hosted Windows runner: installs and removes
# MonitorHop in that runner's user profile.
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Installer smoke test is CI-only.' }

$setup = (Resolve-Path (Join-Path $PSScriptRoot '..\release\MonitorHop-Setup.exe')).Path
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\MonitorHop'
$installedExe = Join-Path $installDirectory 'MonitorHop.exe'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (Test-Path $installedExe) { throw 'Test runner already has MonitorHop installed.' }

function Run-Hidden([string] $file, [string[]] $arguments) {
    $process = Start-Process -FilePath $file -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "$file returned exit code $($process.ExitCode)." }
}

function Read-Startup {
    Get-ItemPropertyValue -Path $runKey -Name MonitorHop -ErrorAction SilentlyContinue
}

$arguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/LANG=en')
Run-Hidden $setup ($arguments + '/TASKS=autostart')
if (-not (Test-Path $installedExe) -or -not (Read-Startup)) {
    throw 'Fresh installation did not create the app and requested startup entry.'
}

$app = Start-Process -FilePath $installedExe -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 2
if ($app.HasExited) { throw 'Installed app exited before the update test.' }
Run-Hidden $setup $arguments
if (-not $app.WaitForExit(15000)) { throw 'Silent update did not close the running app.' }
if (-not (Read-Startup)) { throw 'Silent update removed the enabled startup entry.' }

# Simulate disabling startup in app Settings, then upgrade again with a stale
# installer task choice. The user's current registry preference must win.
Remove-ItemProperty -Path $runKey -Name MonitorHop
Run-Hidden $setup ($arguments + '/TASKS=autostart')
if (Read-Startup) { throw 'Update re-enabled startup after the user disabled it.' }

$app = Start-Process -FilePath $installedExe -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 2
if ($app.HasExited) { throw 'Installed app exited before the uninstall test.' }
$uninstaller = Get-ChildItem -LiteralPath $installDirectory -Filter 'unins*.exe' |
    Select-Object -First 1
if ($null -eq $uninstaller) { throw 'Inno Setup uninstaller was not created.' }
Run-Hidden $uninstaller.FullName @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
if (-not $app.WaitForExit(15000) -or (Test-Path $installedExe)) {
    throw 'Silent uninstall did not close and remove the running app.'
}
Write-Output 'Silent install, two upgrades, startup preservation, and uninstall passed.'
