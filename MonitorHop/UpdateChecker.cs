using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Win32;

namespace MonitorHop;

internal static class UpdateChecker
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/danilyu4ka/MonitorHop/releases/latest";
    private const string ReleaseBase =
        "https://github.com/danilyu4ka/MonitorHop/releases/tag/";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };

    public static Version InstalledVersion => Normalize(
        typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0));

    // An administrator can disable release checks without modifying user settings.
    public static bool IsAllowedByPolicy()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\MonitorHop");
            return key?.GetValue("DisableUpdateCheck") is not int value || value != 1;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (System.Security.SecurityException) { return false; }
        catch (IOException) { return false; }
    }

    public static async Task<ReleaseInfo?> GetNewerReleaseAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
        request.Headers.UserAgent.ParseAdd($"MonitorHop/{InstalledVersion}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await Client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("tag_name", out var tagElement) ||
            tagElement.ValueKind != JsonValueKind.String)
            return null;

        string? tag = tagElement.GetString();
        if (!TryParseTag(tag, out var version) || version <= InstalledVersion) return null;
        return new ReleaseInfo(version, new Uri(ReleaseBase + tag));
    }

    internal static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrEmpty(tag)) return false;
        string numeric = tag.StartsWith('v') || tag.StartsWith('V') ? tag[1..] : tag;
        string[] parts = numeric.Split('.');
        if (parts.Length is < 2 or > 4 ||
            parts.Any(part => part.Length is < 1 or > 9 || !part.All(char.IsAsciiDigit)) ||
            !Version.TryParse(numeric, out var parsed))
            return false;
        version = Normalize(parsed);
        return true;
    }

    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
}

internal sealed record ReleaseInfo(Version Version, Uri Page);
