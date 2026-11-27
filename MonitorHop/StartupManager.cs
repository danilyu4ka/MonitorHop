#if STORE_BUILD
using Windows.ApplicationModel;
#else
using Microsoft.Win32;
#endif

namespace MonitorHop;

internal static class StartupManager
{
#if STORE_BUILD
    // Must match the StartupTask TaskId in store/AppxManifest.template.xml.
    private const string TaskId = "MonitorHopStartup";

    public static bool IsEnabled()
    {
        try
        {
            var task = StartupTask.GetAsync(TaskId).GetAwaiter().GetResult();
            return task.State == StartupTaskState.Enabled;
        }
        catch (Exception error) when (error is InvalidOperationException or
            System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void Apply(bool enabled, string? executablePath = null)
    {
        var task = StartupTask.GetAsync(TaskId).GetAwaiter().GetResult();
        if (!enabled)
        {
            task.Disable();
            return;
        }

        if (task.State == StartupTaskState.Enabled) return;
        if (task.State is StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy)
            throw new InvalidOperationException("Enable MonitorHop in Windows Startup apps.");
        if (task.RequestEnableAsync().GetAwaiter().GetResult() != StartupTaskState.Enabled)
            throw new InvalidOperationException("Windows did not enable the startup task.");
    }
#else
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
#endif
}
