using Microsoft.Win32;

namespace MonitorHop;

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MonitorHop";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string command && command.Length > 0;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (System.Security.SecurityException) { return false; }
    }

    public static void Apply(bool enabled, string? executablePath = null)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true)
            ?? throw new UnauthorizedAccessException("Cannot open the Run key.");
        if (enabled)
            key.SetValue(ValueName, $"\"{executablePath ?? Application.ExecutablePath}\"", RegistryValueKind.String);
        else
            key.DeleteValue(ValueName, false);
    }
}
