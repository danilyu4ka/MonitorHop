using System.Threading;

namespace MonitorHop;

internal static class Program
{
    private static int _handlingUiException;

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
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += OnThreadException;
        try { Application.Run(new TrayContext()); }
        finally
        {
            Application.ThreadException -= OnThreadException;
            mutex.ReleaseMutex();
        }
        return 0;
    }

    private static void OnThreadException(object? sender, ThreadExceptionEventArgs e) =>
        HandleUnexpectedError();

    internal static void HandleUnexpectedError()
    {
        if (Interlocked.Exchange(ref _handlingUiException, 1) != 0)
        {
            Application.Exit();
            return;
        }

        try
        {
            string language;
            try { language = AppSettings.Load().Language; }
            catch { language = Texts.SystemLanguage; }
            MessageBox.Show(Texts.Get(language, "UnexpectedErrorBody"),
                Texts.Get(language, "UnexpectedErrorTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { Application.Exit(); }
    }
}
