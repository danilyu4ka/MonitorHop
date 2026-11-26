using System.Threading;

namespace MonitorHop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
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
