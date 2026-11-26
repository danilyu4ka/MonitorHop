namespace MonitorHop;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _enabled;
    private readonly CheckBox _startup;
    private readonly CheckBox _fullscreen;
    private readonly CheckBox _monitorMap;
    private readonly ComboBox _language;
    private readonly ComboBox _modifiers;
    private readonly Label _languageLabel;
    private readonly Label _hotkeyLabel;
    private readonly Label _processLabel;
    private readonly TextBox _search;
    private readonly Button _refresh;
    private readonly ListView _processes;
    private readonly Button _advancedToggle;
    private readonly TextBox _advanced;
    private readonly Button _addNames;
    private readonly Label _hint;
    private readonly Button _save;
    private readonly Button _cancel;
    private readonly HashSet<string> _ignoredNames;
    private List<VisibleApp> _running = [];
    private bool _populating;

    public AppSettings Result { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        Result = settings;
        _ignoredNames = new HashSet<string>(settings.IgnoredProcesses
            .Select(NormalizeProcessName).Where(name => name.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(650, 673);
        Font = new Font("Segoe UI", 10);
        Icon = Branding.AppIcon;

        _enabled = new CheckBox { Left = 22, Top = 16, Width = 600, Height = 26, Checked = settings.Enabled };
        _languageLabel = new Label { Left = 22, Top = 50, Width = 590, Height = 23 };
        _language = new ComboBox { Left = 22, Top = 75, Width = 225, DropDownStyle = ComboBoxStyle.DropDownList };
        _language.Items.AddRange([
            new LanguageOption("ru", "Русский"),
            new LanguageOption("en", "English"),
            new LanguageOption("et", "Eesti")
        ]);
        _hotkeyLabel = new Label { Left = 22, Top = 115, Width = 600, Height = 23 };
        _modifiers = new ComboBox { Left = 22, Top = 140, Width = 225, DropDownStyle = ComboBoxStyle.DropDownList };
        _modifiers.Items.AddRange(AppSettings.AllowedModifiers);
        _modifiers.SelectedItem = settings.HotkeyModifiers;
        if (_modifiers.SelectedIndex < 0) _modifiers.SelectedIndex = 0;

        _startup = new CheckBox { Left = 22, Top = 180, Width = 600, Height = 26, Checked = settings.StartWithWindows };
        _fullscreen = new CheckBox { Left = 22, Top = 211, Width = 600, Height = 26, Checked = settings.IgnoreFullscreen };
        _monitorMap = new CheckBox { Left = 22, Top = 242, Width = 600, Height = 26, Checked = settings.ShowMonitorMap };
        _processLabel = new Label { Left = 22, Top = 280, Width = 600, Height = 23 };
        _search = new TextBox { Left = 22, Top = 307, Width = 480, Height = 28 };
        _refresh = new Button { Left = 512, Top = 305, Width = 115, Height = 30 };
        _processes = new ListView
        {
            Left = 22, Top = 344, Width = 605, Height = 178,
            View = View.Details, CheckBoxes = true, FullRowSelect = true,
            HideSelection = false, MultiSelect = false
        };
        _processes.Columns.Add("", 180);
        _processes.Columns.Add("", 105);
        _processes.Columns.Add("", 295);

        _advancedToggle = new Button { Left = 22, Top = 532, Width = 235, Height = 29 };
        _advanced = new TextBox
        {
            Left = 22, Top = 568, Width = 476, Height = 59,
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            Visible = false
        };
        _addNames = new Button { Left = 506, Top = 568, Width = 121, Height = 30, Visible = false };
        _hint = new Label
        {
            Left = 22, Top = 631, Width = 390, Height = 18,
            ForeColor = SystemColors.GrayText,
            Font = new Font("Segoe UI", 8.5f), Visible = false
        };
        _save = new Button { Left = 425, Top = 634, Width = 98, Height = 30, DialogResult = DialogResult.OK };
        _cancel = new Button { Left = 530, Top = 634, Width = 97, Height = 30, DialogResult = DialogResult.Cancel };

        Controls.AddRange([_enabled, _languageLabel, _language, _hotkeyLabel, _modifiers,
            _startup, _fullscreen, _monitorMap, _processLabel, _search, _refresh, _processes,
            _advancedToggle, _advanced, _addNames, _hint, _save, _cancel]);
        AcceptButton = _save;
        CancelButton = _cancel;

        _language.SelectedIndexChanged += (_, _) => ApplyLanguage();
        _language.SelectedItem = _language.Items.Cast<LanguageOption>()
            .First(option => option.Code == Texts.NormalizeLanguage(settings.Language));
        _search.TextChanged += (_, _) => PopulateProcessList();
        _refresh.Click += (_, _) => RefreshProcesses();
        _processes.ItemChecked += (_, e) =>
        {
            if (_populating || e.Item.Tag is not string name) return;
            if (e.Item.Checked) _ignoredNames.Add(name);
            else _ignoredNames.Remove(name);
            if (_advanced.Visible) SyncAdvancedText();
        };
        _advancedToggle.Click += (_, _) =>
        {
            bool show = !_advanced.Visible;
            if (show) SyncAdvancedText();
            else ApplyAdvancedNames();
            _advanced.Visible = show;
            _addNames.Visible = show;
            _hint.Visible = show;
        };
        _advanced.Leave += (_, _) => ApplyAdvancedNames();
        _addNames.Click += (_, _) => ApplyAdvancedNames();
        _save.Click += (_, _) =>
        {
            if (_advanced.Visible) ApplyAdvancedNames();
            Result = new AppSettings
            {
                Enabled = _enabled.Checked,
                StartWithWindows = _startup.Checked,
                IgnoreFullscreen = _fullscreen.Checked,
                ShowMonitorMap = _monitorMap.Checked,
                OverlayOffsetX = Result.OverlayOffsetX,
                OverlayOffsetY = Result.OverlayOffsetY,
                Language = ((LanguageOption)_language.SelectedItem!).Code,
                HotkeyModifiers = (string)_modifiers.SelectedItem!,
                IgnoredProcesses = _ignoredNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Select(name => name + ".exe").ToList()
            };
        };

        ApplyLanguage();
        RefreshProcesses();
    }

    private void ApplyLanguage()
    {
        string language = ((LanguageOption)_language.SelectedItem!).Code;
        string T(string key) => Texts.Get(language, key);
        Text = T("SettingsTitle");
        _enabled.Text = T("EnabledSetting");
        _languageLabel.Text = T("LanguageSetting");
        _hotkeyLabel.Text = T("HotkeySetting");
        _startup.Text = T("StartupSetting");
        _fullscreen.Text = T("FullscreenSetting");
        _monitorMap.Text = T("MonitorMapSetting");
        _processLabel.Text = T("ProcessList");
        _search.PlaceholderText = T("SearchProcesses");
        _refresh.Text = T("Refresh");
        _processes.Columns[0].Text = T("Process");
        _processes.Columns[1].Text = T("Instances");
        _processes.Columns[2].Text = T("Window");
        _advancedToggle.Text = T("Advanced");
        _addNames.Text = T("AddNames");
        _hint.Text = T("IgnoredHint");
        _save.Text = T("Save");
        _cancel.Text = T("Cancel");
    }

    private void RefreshProcesses()
    {
        _running = VisibleAppScanner.GetVisibleApps();
        PopulateProcessList();
    }

    private void PopulateProcessList()
    {
        if (_processes is null) return;
        _populating = true;
        _processes.BeginUpdate();
        try
        {
            _processes.Items.Clear();
            string search = _search.Text.Trim();
            foreach (var entry in _running)
            {
                if (search.Length > 0 &&
                    !entry.Name.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !entry.WindowTitle.Contains(search, StringComparison.OrdinalIgnoreCase))
                    continue;
                var item = new ListViewItem(entry.Name + ".exe")
                {
                    Tag = entry.Name,
                    Checked = _ignoredNames.Contains(entry.Name)
                };
                item.SubItems.Add(entry.Instances.ToString());
                item.SubItems.Add(entry.WindowTitle);
                _processes.Items.Add(item);
            }
        }
        finally
        {
            _processes.EndUpdate();
            _populating = false;
        }
    }

    private void ApplyAdvancedNames()
    {
        if (!_advanced.Visible) return;
        _ignoredNames.Clear();
        foreach (string line in _advanced.Lines)
        {
            string name = NormalizeProcessName(line);
            if (name.Length > 0) _ignoredNames.Add(name);
        }
        SyncAdvancedText();
        PopulateProcessList();
    }

    private void SyncAdvancedText() => _advanced.Text = string.Join(Environment.NewLine,
        _ignoredNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => name + ".exe"));

    private static string NormalizeProcessName(string value)
    {
        return AppSettings.NormalizeProcessName(value);
    }

    private sealed record LanguageOption(string Code, string Name)
    {
        public override string ToString() => Name;
    }

}
