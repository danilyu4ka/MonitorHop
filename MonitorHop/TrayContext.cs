using System.Diagnostics;

namespace MonitorHop;

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly OverlayForm _overlay;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly EventWaitHandle _exitRequest;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _exitItem;
    private IntPtr _target;
    private IntPtr _cachedProcessWindow;
    private uint _cachedProcessId;
    private string _cachedProcessName = "";
    private AppSettings _settings;

    public TrayContext()
    {
        _settings = AppSettings.Load();
        _exitRequest = new EventWaitHandle(false, EventResetMode.AutoReset,
            @"Local\MonitorHop.ExitRequest");
        _overlay = new OverlayForm();
        _overlay.DirectionChosen += Move;
        _overlay.MonitorChosen += MoveToMonitor;
        _overlay.IgnoreRequested += IgnoreCurrent;
        _overlay.SettingsRequested += OpenSettings;
        _overlay.PositionOffsetChanged += offset =>
        {
            _settings.OverlayOffsetX = offset.X;
            _settings.OverlayOffsetY = offset.Y;
            PersistSettings();
        };
        _hotkeyWindow = new HotkeyWindow(Move);

        var menu = new ContextMenuStrip();
        _enabledItem = new ToolStripMenuItem("Включено")
        {
            Checked = _settings.Enabled, CheckOnClick = true
        };
        _enabledItem.CheckedChanged += (_, _) =>
        {
            if (_settings.Enabled == _enabledItem.Checked) return;
            _settings.Enabled = _enabledItem.Checked;
            PersistSettings();
            ApplySettings();
        };
        menu.Items.Add(_enabledItem);
        _settingsItem = new ToolStripMenuItem("Настройки…", null, (_, _) => OpenSettings());
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        _exitItem = new ToolStripMenuItem("Выход", null, (_, _) => ExitThread());
        menu.Items.Add(_exitItem);

        _tray = new NotifyIcon
        {
            Icon = Branding.AppIcon,
            Text = "MonitorHop",
            ContextMenuStrip = menu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => OpenSettings();

        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) =>
        {
            if (_exitRequest.WaitOne(0)) ExitThread();
            else RefreshOverlay();
        };
        _timer.Start();
        ApplySettings();
    }

    private void RefreshOverlay()
    {
        bool multipleMonitors = Screen.AllScreens.Length >= 2;
        string tooltip = multipleMonitors ? T("AppName") : T("WaitingSecondMonitor");
        if (_tray.Text != tooltip) _tray.Text = tooltip;
        if (!_settings.Enabled || !multipleMonitors)
        {
            _target = IntPtr.Zero;
            _overlay.Detach();
            return;
        }

        var hwnd = Native.GetForegroundWindow();
        if (!IsEligible(hwnd, out var bounds))
        {
            _target = IntPtr.Zero;
            _overlay.Detach();
            return;
        }

        _target = hwnd;
        _overlay.Attach(bounds, Screen.FromHandle(hwnd), hwnd);
    }

    private bool IsEligible(IntPtr hwnd, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd) ||
            !Native.IsWindowVisible(hwnd) || Native.IsIconic(hwnd) ||
            Native.GetAncestor(hwnd, Native.GaRoot) != hwnd ||
            (Native.GetWindowLong(hwnd, Native.GwlExstyle) & Native.WsExToolWindow) != 0 ||
            !Native.GetWindowRect(hwnd, out var rect))
            return false;

        bounds = rect.ToRectangle();
        if (Native.DwmGetWindowAttribute(hwnd, 9, out var visibleRect,
                System.Runtime.InteropServices.Marshal.SizeOf<Native.Rect>()) == 0)
        {
            var visible = visibleRect.ToRectangle();
            if (visible.Width > 0 && visible.Height > 0) bounds = visible;
        }
        if (bounds.Width < 170 || bounds.Height < 100) return false;

        // Borderless full-screen apps and games should not be covered.
        var monitor = Screen.FromHandle(hwnd).Bounds;
        if (_settings.IgnoreFullscreen && !Native.IsZoomed(hwnd) &&
            bounds.Left <= monitor.Left + 1 && bounds.Top <= monitor.Top + 1 &&
            bounds.Right >= monitor.Right - 1 && bounds.Bottom >= monitor.Bottom - 1)
            return false;

        Native.GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId == 0 || processId == Environment.ProcessId) return false;
        if (_cachedProcessWindow == hwnd && _cachedProcessId == processId)
            return !_settings.IsIgnored(_cachedProcessName);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            _cachedProcessWindow = hwnd;
            _cachedProcessId = processId;
            _cachedProcessName = process.ProcessName;
            return !_settings.IsIgnored(_cachedProcessName);
        }
        catch (ArgumentException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private void Move(Direction direction)
    {
        if (!_settings.Enabled) return;
        RefreshOverlay();
        if (_target == IntPtr.Zero || Native.GetForegroundWindow() != _target) return;
        if (!MonitorMover.Move(_target, direction) &&
            MonitorMover.FindNeighbor(Screen.FromHandle(_target), direction) is not null)
        {
            _tray.ShowBalloonTip(3000, T("MoveFailedTitle"), T("MoveFailedBody"),
                ToolTipIcon.Warning);
        }
        RefreshOverlay();
    }

    private void MoveToMonitor(Screen monitor)
    {
        if (!_settings.Enabled) return;
        RefreshOverlay();
        if (_target == IntPtr.Zero || Native.GetForegroundWindow() != _target) return;
        if (!MonitorMover.MoveTo(_target, monitor) &&
            Screen.FromHandle(_target).DeviceName != monitor.DeviceName)
            _tray.ShowBalloonTip(3000, T("MoveFailedTitle"), T("MoveFailedBody"),
                ToolTipIcon.Warning);
        RefreshOverlay();
    }

    private void IgnoreCurrent()
    {
        if (_target == IntPtr.Zero || Native.GetForegroundWindow() != _target) return;
        Native.GetWindowThreadProcessId(_target, out uint processId);
        if (processId == 0) return;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            string name = process.ProcessName + ".exe";
            if (!_settings.IsIgnored(process.ProcessName))
                _settings.IgnoredProcesses.Add(name);
            PersistSettings();
            RefreshOverlay();
            _tray.ShowBalloonTip(2500, T("IgnoredTitle"), T("IgnoredBody"),
                ToolTipIcon.Info);
        }
        catch (ArgumentException) { RefreshOverlay(); }
        catch (System.ComponentModel.Win32Exception) { RefreshOverlay(); }
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settings);
        if (dialog.ShowDialog() != DialogResult.OK) return;
        _settings = dialog.Result;
        try { StartupManager.Apply(_settings.StartWithWindows); }
        catch (UnauthorizedAccessException) { ShowStartupError(); }
        catch (System.Security.SecurityException) { ShowStartupError(); }
        _enabledItem.Checked = _settings.Enabled;
        PersistSettings();
        ApplySettings();
    }

    private void PersistSettings()
    {
        try { _settings.Save(); }
        catch (IOException) { ShowSaveError(); }
        catch (UnauthorizedAccessException) { ShowSaveError(); }
    }

    private void ShowSaveError() => _tray.ShowBalloonTip(3000,
        T("SaveFailedTitle"), T("SaveFailedBody"), ToolTipIcon.Warning);

    private void ShowStartupError()
    {
        _settings.StartWithWindows = StartupManager.IsEnabled();
        _tray.ShowBalloonTip(3000, T("StartupFailedTitle"), T("StartupFailedBody"),
            ToolTipIcon.Warning);
    }

    private string T(string key) => Texts.Get(_settings.Language, key);

    private void ApplySettings()
    {
        _enabledItem.Text = T("Enabled");
        _settingsItem.Text = T("Settings");
        _exitItem.Text = T("Exit");
        _overlay.SetLanguage(_settings.Language);
        _overlay.SetDisplayMode(_settings.ShowMonitorMap);
        _overlay.SetPositionOffset(_settings.OverlayOffsetX, _settings.OverlayOffsetY);
        _hotkeyWindow.Configure(_settings.Enabled ? _settings.HotkeyModifiers : null);
        if (_settings.Enabled && !_hotkeyWindow.Registered)
            _tray.ShowBalloonTip(3000, T("HotkeyFailedTitle"), T("HotkeyFailedBody"),
                ToolTipIcon.Warning);
        RefreshOverlay();
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();
        _hotkeyWindow.Dispose();
        _exitRequest.Dispose();
        _overlay.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        base.ExitThreadCore();
    }

    private sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        private readonly Action<Direction> _move;
        public bool Registered { get; private set; }

        public HotkeyWindow(Action<Direction> move)
        {
            _move = move;
            CreateHandle(new CreateParams());
        }

        public void Configure(string? modifiers)
        {
            Unregister();
            if (modifiers is null) return;
            uint flags = Native.ModNoRepeat;
            if (modifiers.Contains("Ctrl")) flags |= Native.ModControl;
            if (modifiers.Contains("Shift")) flags |= Native.ModShift;
            if (modifiers.Contains("Alt")) flags |= Native.ModAlt;
            var keys = new[] { Native.VkLeft, 0x26, 0x28, Native.VkRight };
            for (int i = 0; i < keys.Length; i++)
            {
                if (Native.RegisterHotKey(Handle, i + 1, flags, (uint)keys[i])) continue;
                Unregister();
                return;
            }
            Registered = true;
        }

        private void Unregister()
        {
            for (int i = 1; i <= 4; i++) Native.UnregisterHotKey(Handle, i);
            Registered = false;
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == Native.WmHotkey && message.WParam.ToInt32() is >= 1 and <= 4)
                _move((Direction)(message.WParam.ToInt32() - 1));
            base.WndProc(ref message);
        }

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }
    }
}
