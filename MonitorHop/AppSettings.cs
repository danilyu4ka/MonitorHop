using System.Text.Json;

namespace MonitorHop;

internal sealed class AppSettings
{
    public static readonly string[] AllowedModifiers =
        ["Ctrl+Shift+Alt", "Ctrl+Alt", "Ctrl+Shift", "Alt+Shift"];

    public bool Enabled { get; set; } = true;
    public bool IgnoreFullscreen { get; set; } = true;
    public bool ShowMonitorMap { get; set; }
    public bool StartWithWindows { get; set; }
    public int OverlayOffsetX { get; set; }
    public int OverlayOffsetY { get; set; }
    public int IconOpacityPercent { get; set; } = 78;
    public int IconSizePercent { get; set; } = 100;
    public string Language { get; set; } = Texts.SystemLanguage;
    public string HotkeyModifiers { get; set; } = "Ctrl+Shift+Alt";
    public List<string> IgnoredProcesses { get; set; } = [];

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonitorHop", "settings.json");
    // The directory used before version 1.4.2. Keep this only for migration.
    private static readonly string LegacyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "\u041C\u0435\u043D\u044F\u043B\u043A\u0430 \u044D\u043A\u0440\u0430\u043D\u0430", "settings.json");

    public static AppSettings Load()
    {
        MigrateSettingsFile(LegacyFilePath, FilePath);
        AppSettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch (IOException) { settings = new(); }
        catch (UnauthorizedAccessException) { settings = new(); }
        catch (JsonException) { settings = new(); }
        catch (System.Security.SecurityException) { settings = new(); }

        return NormalizeLoaded(settings);
    }

    internal static void MigrateSettingsFile(string legacyPath, string currentPath)
    {
        if (!File.Exists(legacyPath) || File.Exists(currentPath)) return;

        string currentDirectory = Path.GetDirectoryName(currentPath)!;
        string temporaryPath = currentPath + ".migration.tmp";
        try
        {
            Directory.CreateDirectory(currentDirectory);
            File.Copy(legacyPath, temporaryPath, true);
            File.Move(temporaryPath, currentPath);
        }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }
        catch (System.Security.SecurityException) { return; }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        try
        {
            File.Delete(legacyPath);
            Directory.Delete(Path.GetDirectoryName(legacyPath)!);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (System.Security.SecurityException) { }
    }

    internal static AppSettings NormalizeLoaded(AppSettings settings)
    {
        settings.Language = Texts.NormalizeLanguage(settings.Language);
        if (!AllowedModifiers.Contains(settings.HotkeyModifiers, StringComparer.Ordinal))
            settings.HotkeyModifiers = AllowedModifiers[0];
        settings.IgnoredProcesses = (settings.IgnoredProcesses ?? [])
            .Select(NormalizeProcessName)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => name + ".exe")
            .ToList();
        settings.StartWithWindows = StartupManager.IsEnabled();
        settings.OverlayOffsetX = Math.Clamp(settings.OverlayOffsetX, -300, 300);
        settings.OverlayOffsetY = Math.Clamp(settings.OverlayOffsetY, -150, 150);
        settings.IconOpacityPercent = Math.Clamp(settings.IconOpacityPercent, 30, 100);
        settings.IconSizePercent = Math.Clamp(settings.IconSizePercent, 60, 160);
        return settings;
    }

    public void Save() => SaveTo(FilePath);

    internal void SaveTo(string path)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            File.Move(temporary, path, true);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public bool IsIgnored(string processName) => (IgnoredProcesses ?? []).Any(item =>
        string.Equals(NormalizeProcessName(item), processName, StringComparison.OrdinalIgnoreCase));

    public static string NormalizeProcessName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string name = value.Trim();
        int lastSeparator = Math.Max(name.LastIndexOf('\\'), name.LastIndexOf('/'));
        if (lastSeparator >= 0) name = name[(lastSeparator + 1)..];
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        return name.Trim();
    }
}
