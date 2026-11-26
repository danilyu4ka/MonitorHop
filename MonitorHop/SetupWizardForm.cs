using System.Diagnostics;

namespace MonitorHop;

internal sealed class SetupWizardForm : Form
{
    private readonly ComboBox _language = new() { Left = 188, Top = 220, Width = 245, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _bodyTitle = new() { Left = 188, Top = 115, Width = 440, Height = 34, Font = new Font("Segoe UI", 16, FontStyle.Bold) };
    private readonly Label _bodyText = new() { Left = 190, Top = 164, Width = 420, Height = 68 };
    private readonly Label _languageLabel = new() { Left = 188, Top = 193, Width = 430, Height = 24 };
    private readonly Label _path = new() { Left = 204, Top = 236, Width = 415, Height = 45, ForeColor = Color.FromArgb(65, 65, 65) };
    private readonly CheckBox _startup = new() { Left = 188, Top = 302, Width = 425, Height = 30 };
    private readonly CheckBox _launch = new() { Left = 188, Top = 276, Width = 430, Height = 30, Checked = true };
    private readonly ProgressBar _progress = new() { Left = 188, Top = 240, Width = 420, Height = 23, Style = ProgressBarStyle.Marquee };
    private readonly Button _back = new() { Left = 346, Top = 391, Width = 95, Height = 30 };
    private readonly Button _next = new() { Left = 451, Top = 391, Width = 95, Height = 30 };
    private readonly Button _cancel = new() { Left = 555, Top = 391, Width = 95, Height = 30 };
    private readonly Label _sideTitle = new() { Left = 15, Top = 240, Width = 125, Height = 62, ForeColor = Color.White, Font = new Font("Segoe UI", 15, FontStyle.Bold) };
    private readonly Label _sideStep = new() { Left = 17, Top = 318, Width = 120, Height = 42, ForeColor = Color.FromArgb(215, 231, 255) };
    private readonly Label _headerTag = new()
    {
        Left = 26, Top = 56, Width = 300, Height = 18,
        Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(83, 110, 157)
    };
    private int _page;
    private bool _busy;

    public bool Installed { get; private set; }

    public SetupWizardForm()
    {
        Text = "MonitorHop Setup";
        Icon = Branding.AppIcon;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(680, 445);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(245, 245, 245);

        var header = new Panel { Left = 0, Top = 0, Width = 680, Height = 83, BackColor = Color.White };
        header.Controls.Add(new Label
        {
            Text = "MonitorHop", Left = 23, Top = 18, Width = 380, Height = 37,
            Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.FromArgb(27, 51, 92)
        });
        header.Controls.Add(_headerTag);
        header.Controls.Add(new PictureBox
        {
            Image = Branding.Image, SizeMode = PictureBoxSizeMode.Zoom,
            Left = 597, Top = 11, Width = 66, Height = 66
        });
        var separator = new Panel { Left = 0, Top = 82, Width = 680, Height = 1, BackColor = Color.FromArgb(171, 171, 171) };
        var side = new Panel { Left = 0, Top = 83, Width = 155, Height = 290, BackColor = Color.FromArgb(35, 67, 117) };
        side.Controls.Add(new PictureBox
        {
            Image = Branding.Image, SizeMode = PictureBoxSizeMode.Zoom,
            Left = 27, Top = 31, Width = 100, Height = 100
        });
        _sideTitle.Top = 157;
        _sideStep.Top = 231;
        side.Controls.AddRange([_sideTitle, _sideStep]);
        var footerLine = new Panel { Left = 0, Top = 373, Width = 680, Height = 1, BackColor = Color.FromArgb(171, 171, 171) };

        _language.Items.AddRange(["English", "Русский", "Eesti"]);
        _language.SelectedIndex = Texts.SystemLanguage switch { "ru" => 1, "et" => 2, _ => 0 };
        _language.SelectedIndexChanged += (_, _) => RenderPage();
        _startup.Checked = AppSettings.Load().StartWithWindows;
        _back.Click += (_, _) => { if (_page > 0 && !_busy) { _page--; RenderPage(); } };
        _next.Click += (_, _) => Next();
        _cancel.Click += (_, _) => Close();

        Controls.AddRange([header, separator, side, _bodyTitle, _bodyText, _languageLabel,
            _language, _path, _startup, _launch, _progress, footerLine, _back, _next, _cancel]);
        AcceptButton = _next;
        CancelButton = _cancel;
        RenderPage();
    }

    private string Language => _language.SelectedIndex switch { 1 => "ru", 2 => "et", _ => "en" };

    private string T(string en, string ru, string et) => Language switch
    {
        "ru" => ru,
        "et" => et,
        _ => en
    };

    private void Next()
    {
        if (_busy) return;
        if (_page == 2) { _ = BeginInstall(); return; }
        if (_page == 4)
        {
            if (_launch.Checked)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(Installer.InstalledExecutable, "--restart")
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Installer.InstallDirectory
                    });
                }
                catch (System.ComponentModel.Win32Exception error)
                {
                    MessageBox.Show(error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            Close();
            return;
        }
        _page++;
        RenderPage();
    }

    private async Task BeginInstall()
    {
        _busy = true;
        _page = 3;
        RenderPage();
        string language = Language;
        bool startWithWindows = _startup.Checked;
        var completion = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                Installer.InstallCore(language, startWithWindows);
                completion.SetResult(null);
            }
            catch (Exception error) { completion.SetResult(error); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Exception? failure = await completion.Task;
        _busy = false;
        if (failure is null)
        {
            Installed = true;
            _page = 4;
            RenderPage();
        }
        else
        {
            _page = 2;
            RenderPage();
            MessageBox.Show(this, Texts.Get(Language, "InstallError") + failure.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal void PreviewPage(int page)
    {
        _page = page;
        RenderPage();
    }

    private void RenderPage()
    {
        Text = T("MonitorHop Setup", "Установка MonitorHop", "MonitorHopi paigaldus");
        _headerTag.Text = T("SETUP WIZARD", "МАСТЕР УСТАНОВКИ", "PAIGALDUSVIISARD");
        _sideTitle.Text = T("Setup", "Установка", "Paigaldus");
        _sideStep.Text = T($"Step {Math.Min(_page + 1, 4)} of 4", $"Шаг {Math.Min(_page + 1, 4)} из 4", $"Samm {Math.Min(_page + 1, 4)} / 4");
        _back.Text = T("< Back", "< Назад", "< Tagasi");
        _next.Text = _page == 4 ? T("Finish", "Готово", "Valmis") :
            _page == 2 ? T("Install", "Установить", "Paigalda") : T("Next >", "Далее >", "Edasi >");
        _cancel.Text = T("Cancel", "Отмена", "Loobu");
        _back.Enabled = _page is 1 or 2 && !_busy;
        _next.Enabled = !_busy;
        _cancel.Enabled = !_busy && _page != 4;
        _cancel.Visible = _page != 4;
        _language.Visible = _page == 0;
        _languageLabel.Visible = _page == 0;
        _path.Visible = _page == 2;
        _startup.Visible = _page == 2;
        _launch.Visible = _page == 4;
        _progress.Visible = _page == 3;
        _bodyText.Height = _page == 1 ? 115 : 68;

        switch (_page)
        {
            case 0:
                _bodyTitle.Text = T("Setup language", "Язык установки", "Paigalduse keel");
                _bodyText.Text = T("Select the language to use during setup and in MonitorHop.",
                    "Выберите язык мастера и приложения MonitorHop.",
                    "Vali paigalduse ja MonitorHopi keel.");
                _languageLabel.Text = T("Language:", "Язык:", "Keel:");
                break;
            case 1:
                _bodyTitle.Text = T("Welcome to MonitorHop", "Добро пожаловать", "Tere tulemast");
                _bodyText.Text = T("This wizard installs MonitorHop for your Windows account.\n\nSelect Next to continue.",
                    "Мастер установит MonitorHop для вашей учётной записи Windows.\n\nНажмите «Далее».",
                    "See viisard paigaldab MonitorHopi sinu Windowsi kontole.\n\nJätkamiseks vali „Edasi“.");
                break;
            case 2:
                _bodyTitle.Text = T("Ready to install", "Готово к установке", "Valmis paigaldama");
                _bodyText.Text = T("MonitorHop will be installed here. Desktop and Start menu shortcuts will be created.",
                    "MonitorHop будет установлен по указанному пути. Будут созданы ярлыки на рабочем столе и в меню «Пуск».",
                    "MonitorHop paigaldatakse siia. Töölauale ja menüüsse Start luuakse otseteed.");
                _path.Text = @"%LOCALAPPDATA%\Programs\MonitorHop";
                _startup.Text = T("Start MonitorHop with Windows", "Запускать MonitorHop вместе с Windows", "Käivita MonitorHop koos Windowsiga");
                break;
            case 3:
                _bodyTitle.Text = T("Installing MonitorHop", "Установка MonitorHop", "MonitorHopi paigaldamine");
                _bodyText.Text = T("Please wait while files and shortcuts are installed.",
                    "Подождите, пока создаются файлы и ярлыки.",
                    "Palun oota, kuni failid ja otseteed paigaldatakse.");
                break;
            case 4:
                _bodyTitle.Text = T("Setup complete", "Установка завершена", "Paigaldamine on lõpetatud");
                _bodyText.Text = T("MonitorHop is ready. Open it from the desktop, Start menu, or notification area.",
                    "MonitorHop готов. Откройте его с рабочего стола, из меню «Пуск» или области уведомлений.",
                    "MonitorHop on valmis. Ava see töölaualt, menüüst Start või süsteemisalvest.");
                _launch.Text = T("Launch MonitorHop now", "Запустить MonitorHop сейчас", "Käivita MonitorHop kohe");
                break;
        }
    }
}
