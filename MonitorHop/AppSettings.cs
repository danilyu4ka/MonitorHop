using System.Text.Json;

namespace MonitorHop;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public bool IgnoreFullscreen { get; set; } = true;
    public bool ShowMonitorMap { get; set; }
    public bool StartWithWindows { get; set; }
    public string Language { get; set; } = Texts.SystemLanguage;
    public string HotkeyModifiers { get; set; } = "Ctrl+Shift+Alt";
    public List<string> IgnoredProcesses { get; set; } = [];

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Менялка экрана", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
            settings.IgnoredProcesses ??= [];
            settings.Language = Texts.NormalizeLanguage(settings.Language);
            settings.StartWithWindows = StartupManager.IsEnabled();
            return settings;
        }
        catch (IOException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
        catch (JsonException) { return new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    public bool IsIgnored(string processName) => IgnoredProcesses.Any(item =>
    {
        string name = Path.GetFileName(item.Trim());
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        return string.Equals(name, processName, StringComparison.OrdinalIgnoreCase);
    });
}
