# MonitorHop

A small Windows 10/11 utility for moving the active window between monitors. [Download the installer](https://github.com/danilyu4ka/MonitorHop/releases/latest/download/MonitorHop-Setup.exe) or follow the [illustrated installation guide](docs/INSTALL_GUIDE_EN.md).

Created by [danilyu4ka](https://github.com/danilyu4ka). Licensed under [MIT](LICENSE). See the [corporate deployment notes](docs/CORPORATE_DEPLOYMENT_EN.md) for the install footprint and security details.

## How it works

When at least two monitors are active, a small translucent button appears beside the upper-right controls of the active window. Hover over it to open the move menu. Choose a direction or switch to a map that shows the actual monitor arrangement. MonitorHop places the window at approximately the same relative position on the destination monitor. Maximized windows remain maximized.

With one monitor, MonitorHop stays in the tray and hides the overlay until another display is connected. If the button covers a window control, drag it to a nearby position. You can also drag the open menu by its upper-right logo. The offset is saved and applied to other windows.

## Install and run

- [MonitorHop-Setup.exe](https://github.com/danilyu4ka/MonitorHop/releases/latest/download/MonitorHop-Setup.exe) is a classic Inno Setup wizard. It offers English, Russian, and Estonian. The system language is selected when supported; otherwise English is selected. It creates desktop and Start menu shortcuts and a standard entry in **Windows Settings → Apps → Installed apps**.
- [MonitorHop.exe](https://github.com/danilyu4ka/MonitorHop/releases/latest/download/MonitorHop.exe) is a portable, self-contained single executable.

The installer works for the current user without administrator rights and installs under `%LOCALAPPDATA%\Programs\MonitorHop`. Use the shortcuts to start or restart the app. Double-click the tray icon for Settings; right-click it to disable the overlay or exit. Windows uninstall removes the app, shortcuts, startup entry, and uninstall entry. Settings remain for a future reinstall.

## Controls and settings

The default shortcut is **Ctrl+Shift+Alt + arrow key**. Settings lets you change the modifiers, select the interface language, start with Windows, ignore full-screen apps, or show the monitor map instead of arrows. Full-screen apps are ignored by default.

Use **Ignore this app** in the overlay to ignore the current application. Settings lists applications with visible windows on the connected displays; tick the ones to ignore. **Advanced** accepts process names manually, including apps that are not currently open. An ignored process name applies to all its instances.

Settings are stored in `%LOCALAPPDATA%\MonitorHop\settings.json`. Version 1.4.2 and later move an existing settings file from the previous folder on first launch. Some elevated apps or apps that manage their own windows may resist movement. Moving a snapped window can remove its Snap placement.

## Build from source

Install the .NET 10 SDK and Inno Setup 6, then run:

```powershell
dotnet publish MonitorHop/MonitorHop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o install-payload
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer/MonitorHop.iss
dotnet publish MonitorHop/MonitorHop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o release
```

The installed build keeps the .NET files under the app directory, while the portable build is one executable.
