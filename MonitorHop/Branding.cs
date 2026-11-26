using System.Reflection;

namespace MonitorHop;

internal static class Branding
{
    private static readonly Lazy<Image> ImageHolder = new(() =>
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("MonitorHop.IconImage")!;
        using var original = Image.FromStream(stream);
        return new Bitmap(original);
    });

    public static Image Image => ImageHolder.Value;

    private static readonly Lazy<Icon> AppIconHolder = new(() =>
        Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application);

    public static Icon AppIcon => AppIconHolder.Value;
}
