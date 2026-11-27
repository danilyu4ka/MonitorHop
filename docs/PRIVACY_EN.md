# MonitorHop privacy policy

Last updated: 27 November 2026

MonitorHop is an open-source utility by [danilyu4ka](https://github.com/danilyu4ka). It moves windows between monitors and runs locally on your Windows PC.

## Data the app uses

To show its controls and move a window, MonitorHop reads the active window's title, bounds, process name, and monitor arrangement. It does not capture the screen or send this information to the developer. Ignore rules, language, hotkey, overlay appearance, and other preferences are saved locally in `%LOCALAPPDATA%\MonitorHop\settings.json`.

If an unexpected error occurs, the app writes a local `error.log` in the same folder. Exception details may include local file paths or process names. The log is never uploaded by the app. You can delete the settings and log files when the app is closed.

The Microsoft Store build does not contact GitHub to check for updates. Microsoft Store manages installation and updates. The app has no analytics, advertising, account system, or telemetry. Links you choose to open in a browser are handled by your browser and the destination website.

For privacy questions, open an issue in the [MonitorHop repository](https://github.com/danilyu4ka/MonitorHop/issues). The Microsoft Store and Windows have their own privacy practices, described by Microsoft.
