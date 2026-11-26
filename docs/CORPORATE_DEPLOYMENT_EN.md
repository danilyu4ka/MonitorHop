# MonitorHop in managed Windows environments

MonitorHop is a user-level window management utility. It does not try to hide from endpoint protection. An administrator can review this repository, the installer script, and the published SHA-256 checksums before allowing it in an organization.

## What setup changes

The installer is built with Inno Setup in non-administrative mode (`PrivilegesRequired=lowest`). It does not request UAC elevation. It installs the app under `%LOCALAPPDATA%\Programs\MonitorHop`, creates shortcuts on the current user's desktop and Start menu, and registers the standard per-user uninstall entry. The optional **Start with Windows** task creates an `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\MonitorHop` value. This task is off by default.

The selected installer language is written to the user settings file on a fresh installation. Existing settings are preserved on upgrade. The installer uses the normal Windows uninstall flow; there is no custom temporary EXE copy or deletion helper. Setup asks the user to close a running MonitorHop before updating or removing it.

## What the running app does

MonitorHop runs as a normal interactive user process. It checks the foreground window twice per second, reads its window bounds and process name, and displays a small overlay when at least two monitors are active. It uses standard Windows window APIs to move a selected window. Global hotkeys use `RegisterHotKey`; they can be turned off in Settings. The app does not install a service, driver, browser extension, global event hook, or code in another process. It has no network, update, telemetry, or screen capture feature.

Settings are stored in `%LOCALAPPDATA%\MonitorHop\settings.json`. On first launch after updating from an older version, MonitorHop moves an existing settings file from the former folder to this location. Uninstall removes the installed app, shortcuts, uninstall entry, and startup value. It keeps the settings file for a future reinstall.

## Review and trust

The release includes `SHA256SUMS.txt` to verify downloaded files. Builds are currently unsigned. Windows SmartScreen or a corporate EDR may warn about an unfamiliar unsigned executable; local policy determines whether it may run. Code signing with an organization or publisher certificate is the appropriate next step for broader managed deployment. MonitorHop does not bypass such controls or request elevation to work around them.

Both release variants bundle .NET 10. Windows Update does not patch their bundled runtime; maintainers must rebuild and publish a new release after relevant .NET security updates.

Source: [danilyu4ka/MonitorHop](https://github.com/danilyu4ka/MonitorHop). License: [MIT](../LICENSE).

The app's MIT License is separate from the [Inno Setup license](https://jrsoftware.org/files/is/license.txt), which permits commercial use. Inno Setup's authors [request a commercial compiler license](https://jrsoftware.org/inno-setup/purchase) from organizations that use the compiler; deploying an already-built installer does not count as compiler use.
