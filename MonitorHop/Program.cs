using System.Threading;

namespace MonitorHop;

internal static class Program
{
    private static int _handlingUiException;
    private static readonly object LogGate = new();

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--exit", StringComparer.OrdinalIgnoreCase))
        {
            if (EventWaitHandle.TryOpenExisting(@"Local\MonitorHop.ExitRequest", out var exitRequest))
                using (exitRequest) exitRequest.Set();
            return 0;
        }

        using var mutex = new Mutex(true, "Local\\MonitorHop.SingleInstance", out bool firstInstance);
        bool replaceRunningInstance = args.Contains("--restart", StringComparer.OrdinalIgnoreCase);
#if STORE_BUILD
        replaceRunningInstance = true; // Store launches take over a running classic copy.
#endif
        if (!firstInstance && replaceRunningInstance &&
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
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        try { Application.Run(new TrayContext()); }
        catch (Exception error)
        {
            HandleUnexpectedError(error);
            return 1;
        }
        finally
        {
            Application.ThreadException -= OnThreadException;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            mutex.ReleaseMutex();
        }
        return 0;
    }

    private static void OnThreadException(object? sender, ThreadExceptionEventArgs e) =>
        HandleUnexpectedError(e.Exception);

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        WriteErrorLog(e.ExceptionObject as Exception);

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteErrorLog(e.Exception);
        e.SetObserved();
    }

    internal static void HandleUnexpectedError(Exception? error = null)
    {
        WriteErrorLog(error);
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

    private static void WriteErrorLog(Exception? error)
    {
        try
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonitorHop");
            string path = Path.Combine(directory, "error.log");
            lock (LogGate)
            {
                Directory.CreateDirectory(directory);
                if (File.Exists(path) && new FileInfo(path).Length > 256 * 1024)
                    File.Move(path, path + ".old", true);
                File.AppendAllText(path,
                    $"[{DateTimeOffset.UtcNow:O}] {error?.ToString() ?? "Unknown error"}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch (Exception) { /* Error reporting must not cause another crash. */ }
    }
}
