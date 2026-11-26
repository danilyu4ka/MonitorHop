using System.Threading;

namespace MonitorHop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--finish-uninstall", StringComparer.OrdinalIgnoreCase))
            return Installer.FinishUninstall();
        if (args.Contains("--uninstall-silent", StringComparer.OrdinalIgnoreCase))
            return Installer.BeginUninstall(true);
        if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
            return Installer.BeginUninstall(false);
        if (args.Contains("--install-silent", StringComparer.OrdinalIgnoreCase))
            return Installer.Install(true);
        if (args.Contains("--install", StringComparer.OrdinalIgnoreCase) ||
            Path.GetFileNameWithoutExtension(Application.ExecutablePath)
                .EndsWith("-Setup", StringComparison.OrdinalIgnoreCase))
        {
            ApplicationConfiguration.Initialize();
            using var wizard = new SetupWizardForm();
            Application.Run(wizard);
            return wizard.Installed ? 0 : 1;
        }

        using var mutex = new Mutex(true, "Local\\MonitorHop.SingleInstance", out bool firstInstance);
        if (!firstInstance && args.Contains("--restart", StringComparer.OrdinalIgnoreCase) &&
            EventWaitHandle.TryOpenExisting(@"Local\MonitorHop.ExitRequest", out var request))
        {
            using (request) request.Set();
            try { firstInstance = mutex.WaitOne(TimeSpan.FromSeconds(10)); }
            catch (AbandonedMutexException) { firstInstance = true; }
        }
        if (!firstInstance)
        {
            var language = AppSettings.Load().Language;
            MessageBox.Show(Texts.Get(language, "AlreadyRunning"),
                Texts.Get(language, "AppName"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
        mutex.ReleaseMutex();
        return 0;
    }
}
