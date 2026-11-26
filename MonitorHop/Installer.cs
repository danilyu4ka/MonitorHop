using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MonitorHop;

internal static class Installer
{
    private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorHop";
    private const string ExitEventName = @"Local\MonitorHop.ExitRequest";

    public static string InstallDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs", "MonitorHop");

    public static string InstalledExecutable => Path.Combine(InstallDirectory, "MonitorHop.exe");

    private static string DesktopShortcut => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MonitorHop.lnk");

    private static string StartShortcut => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Programs), "MonitorHop.lnk");

    public static int Install(bool quiet)
    {
        string language = AppSettings.Load().Language;
        try
        {
            InstallCore();
            if (!quiet) MessageBox.Show(Texts.Get(language, "InstallSuccess"),
                Texts.Get(language, "AppName"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or COMException or InvalidOperationException)
        {
            if (!quiet) MessageBox.Show(Texts.Get(language, "InstallError") + error.Message,
                Texts.Get(language, "AppName"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    public static void InstallCore(string? language = null, bool? startWithWindows = null)
    {
        var settings = AppSettings.Load();
        Directory.CreateDirectory(InstallDirectory);
        string source = Path.GetFullPath(Application.ExecutablePath);
        string destination = Path.GetFullPath(InstalledExecutable);
        if (!string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(destination) && EventWaitHandle.TryOpenExisting(ExitEventName, out var request))
            {
                using (request) request.Set();
            }
            for (int attempt = 0; ; attempt++)
            {
                try { File.Copy(source, destination, true); break; }
                catch (IOException) when (attempt < 40) { Thread.Sleep(250); }
            }
        }

        CreateShortcut(DesktopShortcut, destination);
        CreateShortcut(StartShortcut, destination);
        RegisterUninstall(destination);

        if (language is not null) settings.Language = Texts.NormalizeLanguage(language);
        if (startWithWindows.HasValue) settings.StartWithWindows = startWithWindows.Value;
        StartupManager.Apply(settings.StartWithWindows, destination);
        settings.Save();
    }

    public static int BeginUninstall(bool quiet)
    {
        string language = AppSettings.Load().Language;
        try
        {
            if (EventWaitHandle.TryOpenExisting(ExitEventName, out var request))
            {
                using (request) request.Set();
            }

            string helper = Path.Combine(Path.GetTempPath(),
                "MonitorHop-uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
            File.Copy(Application.ExecutablePath, helper);
            _ = Process.Start(new ProcessStartInfo(helper, "--finish-uninstall")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }) ?? throw new InvalidOperationException("The uninstaller could not start.");

            if (!quiet) MessageBox.Show(Texts.Get(language, "UninstallStarted"),
                Texts.Get(language, "AppName"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            if (!quiet) MessageBox.Show(Texts.Get(language, "UninstallError") + error.Message,
                Texts.Get(language, "AppName"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    public static int FinishUninstall()
    {
        // The helper runs from the user's temp folder so Windows can release the installed EXE.
        bool removed = false;
        for (int attempt = 0; attempt < 80; attempt++)
        {
            try
            {
                if (File.Exists(InstalledExecutable)) File.Delete(InstalledExecutable);
                removed = true;
                break;
            }
            catch (IOException) { Thread.Sleep(250); }
            catch (UnauthorizedAccessException) { return 1; }
        }
        if (!removed) return 1;

        try
        {
            DeleteIfExists(DesktopShortcut);
            DeleteIfExists(StartShortcut);
            StartupManager.Apply(false);
            Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, false);
            if (Directory.Exists(InstallDirectory))
            {
                try { Directory.Delete(InstallDirectory); }
                catch (IOException) { /* Preserve unexpected files in the app folder. */ }
            }
            // A running Windows EXE cannot reliably delete itself. Ask Windows to
            // remove this temporary helper on the next reboot.
            MoveFileEx(Application.ExecutablePath, null, 0x00000004);
            return 0;
        }
        catch (IOException) { return 1; }
        catch (UnauthorizedAccessException) { return 1; }
        catch (System.Security.SecurityException) { return 1; }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void RegisterUninstall(string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath, true)
            ?? throw new UnauthorizedAccessException("Cannot register the uninstaller.");
        key.SetValue("DisplayName", "MonitorHop", RegistryValueKind.String);
        key.SetValue("DisplayVersion", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.3.0", RegistryValueKind.String);
        key.SetValue("Publisher", "MonitorHop", RegistryValueKind.String);
        key.SetValue("DisplayIcon", executable, RegistryValueKind.String);
        key.SetValue("InstallLocation", InstallDirectory, RegistryValueKind.String);
        key.SetValue("UninstallString", $"\"{executable}\" --uninstall", RegistryValueKind.String);
        key.SetValue("QuietUninstallString", $"\"{executable}\" --uninstall-silent", RegistryValueKind.String);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", (int)Math.Max(1, new FileInfo(executable).Length / 1024), RegistryValueKind.DWord);
    }

    private static void CreateShortcut(string shortcutPath, string executable)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable.");
        object shellObject = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Script Host could not start.");
        object? shortcutObject = null;
        try
        {
            dynamic shell = shellObject;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcutObject = shortcut;
            shortcut.TargetPath = executable;
            shortcut.Arguments = "--restart";
            shortcut.WorkingDirectory = InstallDirectory;
            shortcut.IconLocation = executable + ",0";
            shortcut.Description = "MonitorHop";
            shortcut.Save();
        }
        finally
        {
            if (shortcutObject is not null && Marshal.IsComObject(shortcutObject))
                Marshal.FinalReleaseComObject(shortcutObject);
            if (Marshal.IsComObject(shellObject)) Marshal.FinalReleaseComObject(shellObject);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(string existingName, string? newName, int flags);
}
