namespace MonitorHop;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _enabled;
    private readonly CheckBox _startup;
    private readonly CheckBox _fullscreen;
    private readonly CheckBox _monitorMap;
    private readonly CheckBox _checkUpdates;
    private readonly ComboBox _language;
    private readonly ComboBox _modifiers;
    private readonly Label _languageLabel;
    private readonly Label _hotkeyLabel;
    private readonly Label _opacityLabel;
    private readonly Label _sizeLabel;
    private readonly TrackBar _opacity;
    private readonly TrackBar _size;
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
    private readonly Panel _content;
    private readonly Panel _footer;
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
        TopMost = true;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(650, 720);
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
        _checkUpdates = new CheckBox { Left = 22, Top = 273, Width = 600, Height = 26, Checked = settings.CheckForUpdates };
        _opacityLabel = new Label { Left = 22, Top = 305, Width = 280, Height = 23 };
        _sizeLabel = new Label { Left = 335, Top = 305, Width = 290, Height = 23 };
        _opacity = new TrackBar { Left = 15, Top = 329, Width = 290, Height = 45,
            Minimum = 30, Maximum = 100, TickFrequency = 10,
            Value = Math.Clamp(settings.IconOpacityPercent, 30, 100) };
        _size = new TrackBar { Left = 328, Top = 329, Width = 300, Height = 45,
            Minimum = 60, Maximum = 160, TickFrequency = 20,
            Value = Math.Clamp(settings.IconSizePercent, 60, 160) };
        _processLabel = new Label
        {
            Left = 22, Top = 382, AutoSize = true,
            MaximumSize = new Size(605, 0)
        };
        _search = new TextBox { Left = 22, Top = 408, Width = 480, Height = 28 };
        _refresh = new Button { Left = 512, Top = 406, Width = 115, Height = 30 };
        _processes = new ListView
        {
            Left = 22, Top = 445, Width = 605, Height = 133,
            View = View.Details, CheckBoxes = true, FullRowSelect = true,
            HideSelection = false, MultiSelect = false
        };
        _processes.Columns.Add("", 180);
        _processes.Columns.Add("", 105);
        _processes.Columns.Add("", 295);

        _advancedToggle = new Button { Left = 22, Top = 587, Width = 235, Height = 29 };
        _advanced = new TextBox
        {
            Left = 22, Top = 623, Width = 476, Height = 59,
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            Visible = false
        };
        _addNames = new Button { Left = 506, Top = 623, Width = 121, Height = 30, Visible = false };
        _hint = new Label
        {
            Left = 22, Top = 686, Width = 390, Height = 18,
            ForeColor = SystemColors.GrayText,
            Font = new Font("Segoe UI", 8.5f), Visible = false
        };
        _save = new Button { Left = 425, Top = 11, Width = 98, Height = 30, DialogResult = DialogResult.OK };
        _cancel = new Button { Left = 530, Top = 11, Width = 97, Height = 30, DialogResult = DialogResult.Cancel };
        _content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _footer = new Panel { Dock = DockStyle.Bottom, Height = 52 };

        _content.Controls.AddRange([_enabled, _languageLabel, _language, _hotkeyLabel, _modifiers,
            _startup, _fullscreen, _monitorMap, _checkUpdates,
            _opacityLabel, _sizeLabel, _opacity, _size,
            _processLabel, _search, _refresh, _processes,
            _advancedToggle, _advanced, _addNames, _hint]);
        _footer.Controls.AddRange([_save, _cancel]);
        Controls.AddRange([_content, _footer]);
        AcceptButton = _save;
        CancelButton = _cancel;

        _language.SelectedIndexChanged += (_, _) => ApplyLanguage();
        _opacity.ValueChanged += (_, _) => ApplyAppearanceLabels();
        _size.ValueChanged += (_, _) => ApplyAppearanceLabels();
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
            LayoutProcessSection();
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
                CheckForUpdates = _checkUpdates.Checked,
                IconOpacityPercent = _opacity.Value,
                IconSizePercent = _size.Value,
                OverlayOffsetX = Result.OverlayOffsetX,
                OverlayOffsetY = Result.OverlayOffsetY,
                Language = ((LanguageOption)_language.SelectedItem!).Code,
                HotkeyModifiers = (string)_modifiers.SelectedItem!,
                IgnoredProcesses = _ignoredNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Select(name => name + ".exe").ToList()
            };
        };

        ApplyLanguage();
        Shown += (_, _) => LayoutProcessSection();
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
        _checkUpdates.Text = T("UpdateCheckSetting");
        ApplyAppearanceLabels();
        _processLabel.Text = T("ProcessList");
        LayoutProcessSection();
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

    private void LayoutProcessSection()
    {
        _content.AutoScrollPosition = Point.Empty;
        _search.Top = _processLabel.Bottom + 9;
        _refresh.Top = _search.Top - 2;
        _processes.Top = Math.Max(_search.Bottom, _refresh.Bottom) + 9;
        _advancedToggle.Top = _processes.Bottom + 10;
        _advanced.Top = _advancedToggle.Bottom + 7;
        _addNames.Top = _advanced.Top;
        _hint.Top = _advanced.Bottom + 4;
        _content.AutoScrollMinSize = new Size(0,
            (_advanced.Visible ? _hint.Bottom : _advancedToggle.Bottom) + 14);
    }

    private void ApplyAppearanceLabels()
    {
        if (_language.SelectedItem is not LanguageOption language) return;
        _opacityLabel.Text = $"{Texts.Get(language.Code, "IconOpacitySetting")}: {_opacity.Value}%";
        _sizeLabel.Text = $"{Texts.Get(language.Code, "IconSizeSetting")}: {_size.Value}%";
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
