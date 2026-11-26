namespace MonitorHop;

internal static class Texts
{
    private static readonly Dictionary<string, (string Ru, string En, string Et)> Values = new()
    {
        ["AppName"] = ("MonitorHop", "MonitorHop", "MonitorHop"),
        ["WaitingSecondMonitor"] = ("MonitorHop — ждёт второй монитор", "MonitorHop — waiting for a second monitor", "MonitorHop — ootab teist kuvarit"),
        ["AlreadyRunning"] = ("MonitorHop уже запущен. Настройки доступны через значок в трее.", "MonitorHop is already running. Open settings from the tray icon.", "MonitorHop juba töötab. Ava seaded süsteemisalve ikoonist."),
        ["UnexpectedErrorTitle"] = ("Ошибка MonitorHop", "MonitorHop error", "MonitorHopi viga"),
        ["UnexpectedErrorBody"] = ("Произошла непредвиденная ошибка. MonitorHop завершит работу. Запустите его снова через меню «Пуск».", "An unexpected error occurred. MonitorHop will close. Start it again from the Start menu.", "Tekkis ootamatu viga. MonitorHop sulgub. Käivita see uuesti menüüst Start."),
        ["Enabled"] = ("Включено", "Enabled", "Sisse lülitatud"),
        ["Settings"] = ("Настройки…", "Settings…", "Seaded…"),
        ["Exit"] = ("Выход", "Exit", "Välju"),
        ["SettingsTitle"] = ("MonitorHop — настройки", "MonitorHop — Settings", "MonitorHop — seaded"),
        ["EnabledSetting"] = ("Показывать кнопку и включить горячие клавиши", "Show button and enable hotkeys", "Kuva nupp ja luba kiirklahvid"),
        ["LanguageSetting"] = ("Язык интерфейса", "Interface language", "Kasutajaliidese keel"),
        ["HotkeySetting"] = ("Горячие клавиши: модификаторы + стрелка", "Hotkeys: modifiers + arrow key", "Kiirklahvid: muuteklahvid + nooleklahv"),
        ["StartupSetting"] = ("Запускать вместе с Windows", "Start with Windows", "Käivita koos Windowsiga"),
        ["FullscreenSetting"] = ("Автоматически игнорировать полноэкранные приложения", "Automatically ignore full-screen apps", "Eira täisekraanirakendusi automaatselt"),
        ["MonitorMapSetting"] = ("Показывать карту мониторов вместо стрелок", "Show monitor map instead of arrows", "Kuva noolte asemel kuvarite kaart"),
        ["UpdateCheckSetting"] = ("Проверять обновления при запуске", "Check for updates at startup", "Otsi käivitamisel uuendusi"),
        ["IconOpacitySetting"] = ("Прозрачность значка", "Icon opacity", "Ikooni läbipaistmatus"),
        ["IconSizeSetting"] = ("Размер значка", "Icon size", "Ikooni suurus"),
        ["UpdateTitle"] = ("Доступно обновление MonitorHop", "MonitorHop update available", "MonitorHopi uuendus on saadaval"),
        ["UpdatePrompt"] = ("Установлена версия {0}. Доступна версия {1}. Открыть страницу релиза на GitHub?", "Installed version: {0}. Version {1} is available. Open its GitHub release page?", "Paigaldatud versioon: {0}. Saadaval on versioon {1}. Kas avada GitHubi väljalaske leht?"),
        ["UpdateOpenFailed"] = ("Не удалось открыть страницу релиза.", "Could not open the release page.", "Väljalaske lehte ei õnnestunud avada."),
        ["IgnoredSetting"] = ("Игнорировать приложения (имя процесса, по одному на строку):", "Ignore apps (process name, one per line):", "Eira rakendusi (protsessi nimi, üks rea kohta):"),
        ["IgnoredHint"] = ("Имена процессов по одному на строку, например game.exe", "One process name per line, for example game.exe", "Üks protsessi nimi rea kohta, näiteks game.exe"),
        ["ProcessList"] = ("Приложения с видимыми окнами (отметьте для игнорирования)", "Apps with visible windows (check to ignore)", "Nähtavate akendega rakendused (märgi eiramiseks)"),
        ["SearchProcesses"] = ("Поиск процесса…", "Search processes…", "Otsi protsessi…"),
        ["Refresh"] = ("Обновить", "Refresh", "Värskenda"),
        ["Process"] = ("Процесс", "Process", "Protsess"),
        ["Instances"] = ("Экземпляров", "Instances", "Eksemplare"),
        ["Window"] = ("Окно", "Window", "Aken"),
        ["Advanced"] = ("Дополнительно (Advanced)", "Advanced", "Täpsemalt (Advanced)"),
        ["AddNames"] = ("Применить список", "Apply list", "Rakenda loend"),
        ["MonitorMapTitle"] = ("Выберите монитор", "Choose a monitor", "Vali kuvar"),
        ["Save"] = ("Сохранить", "Save", "Salvesta"),
        ["Cancel"] = ("Отмена", "Cancel", "Loobu"),
        ["MoveWindow"] = ("Перенести окно", "Move window", "Teisalda aken"),
        ["DragHint"] = ("Перетащите значок", "Drag icon to move", "Lohista ikooni"),
        ["Left"] = ("Влево", "Left", "Vasakule"),
        ["Right"] = ("Вправо", "Right", "Paremale"),
        ["Up"] = ("Вверх", "Up", "Üles"),
        ["Down"] = ("Вниз", "Down", "Alla"),
        ["IgnoreApp"] = ("Игнорировать приложение", "Ignore this app", "Eira seda rakendust"),
        ["IgnoredTitle"] = ("Приложение добавлено в игнор", "App added to ignore list", "Rakendus lisati eiratute hulka"),
        ["IgnoredBody"] = ("Настроить список можно через значок в трее.", "Manage the list from the tray icon.", "Halda loendit süsteemisalve ikoonist."),
        ["MoveFailedTitle"] = ("Не удалось переместить окно", "Could not move window", "Akent ei õnnestunud teisaldada"),
        ["MoveFailedBody"] = ("Приложение может ограничивать управление окном или иметь повышенные права.", "The app may restrict window movement or run with elevated rights.", "Rakendus võib piirata akna liigutamist või töötada kõrgendatud õigustega."),
        ["HotkeyFailedTitle"] = ("Горячие клавиши недоступны", "Hotkeys unavailable", "Kiirklahvid pole saadaval"),
        ["HotkeyFailedBody"] = ("Комбинация занята другим приложением. Выберите другую в настройках.", "Another app uses this shortcut. Choose another in Settings.", "Teine rakendus kasutab seda kombinatsiooni. Vali seadetes teine."),
        ["SaveFailedTitle"] = ("Настройки не сохранены", "Settings not saved", "Seadeid ei salvestatud"),
        ["SaveFailedBody"] = ("Нет доступа к папке настроек пользователя.", "Cannot access your settings folder.", "Seadete kaustale puudub juurdepääs."),
        ["StartupFailedTitle"] = ("Не удалось настроить автозапуск", "Could not set startup", "Automaatkäivitust ei õnnestunud seadistada"),
        ["StartupFailedBody"] = ("Windows не разрешила изменить автозапуск в профиле пользователя.", "Windows did not allow changing startup for this user.", "Windows ei lubanud selle kasutaja automaatkäivitust muuta."),
        ["InstallSuccess"] = ("MonitorHop установлен. Ярлыки созданы на рабочем столе и в меню «Пуск».", "MonitorHop is installed. Shortcuts were created on the desktop and in Start.", "MonitorHop on paigaldatud. Töölauale ja menüüsse Start loodi otseteed."),
        ["InstallError"] = ("Не удалось установить MonitorHop: ", "Could not install MonitorHop: ", "MonitorHopi paigaldamine ebaõnnestus: "),
        ["UninstallStarted"] = ("Удаление MonitorHop запущено.", "MonitorHop removal has started.", "MonitorHopi eemaldamine on alanud."),
        ["UninstallError"] = ("Не удалось удалить MonitorHop: ", "Could not remove MonitorHop: ", "MonitorHopi eemaldamine ebaõnnestus: "),
    };

    public static string SystemLanguage => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "ru" => "ru",
        "et" => "et",
        _ => "en"
    };

    public static string NormalizeLanguage(string? language) => language is "ru" or "en" or "et" ? language : "en";

    public static string Get(string? language, string key)
    {
        var text = Values[key];
        return NormalizeLanguage(language) switch
        {
            "en" => text.En,
            "et" => text.Et,
            _ => text.Ru
        };
    }
}
