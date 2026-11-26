namespace MonitorHop;

internal enum Direction { Left, Up, Down, Right }

internal static class MonitorMover
{
    public static Screen? FindNeighbor(Screen source, Direction direction)
    {
        var sourceCenter = Center(source.Bounds);
        return Screen.AllScreens
            .Where(screen => screen.DeviceName != source.DeviceName)
            .Select(screen => (screen, center: Center(screen.Bounds)))
            .Where(item => direction switch
            {
                Direction.Left => item.center.X < sourceCenter.X,
                Direction.Right => item.center.X > sourceCenter.X,
                Direction.Up => item.center.Y < sourceCenter.Y,
                Direction.Down => item.center.Y > sourceCenter.Y,
                _ => false
            })
            .OrderBy(item => direction is Direction.Left or Direction.Right
                ? Math.Abs(item.center.X - sourceCenter.X) +
                  Math.Abs(item.center.Y - sourceCenter.Y) * 0.4
                : Math.Abs(item.center.Y - sourceCenter.Y) +
                  Math.Abs(item.center.X - sourceCenter.X) * 0.4)
            .Select(item => item.screen)
            .FirstOrDefault();
    }

    public static Rectangle MapWindow(Rectangle window, Rectangle source, Rectangle target)
    {
        int width = Math.Min(window.Width, target.Width);
        int height = Math.Min(window.Height, target.Height);

        double availableSourceX = Math.Max(1, source.Width - window.Width);
        double availableSourceY = Math.Max(1, source.Height - window.Height);
        double relativeX = Math.Clamp((window.Left - source.Left) / availableSourceX, 0, 1);
        double relativeY = Math.Clamp((window.Top - source.Top) / availableSourceY, 0, 1);

        int x = target.Left + (int)Math.Round(relativeX * Math.Max(0, target.Width - width));
        int y = target.Top + (int)Math.Round(relativeY * Math.Max(0, target.Height - height));
        return new Rectangle(x, y, width, height);
    }

    public static bool Move(IntPtr hwnd, Direction direction)
    {
        if (!Native.IsWindow(hwnd)) return false;
        var source = Screen.FromHandle(hwnd);
        var target = FindNeighbor(source, direction);
        return target is not null && MoveTo(hwnd, target);
    }

    public static bool MoveTo(IntPtr hwnd, Screen target)
    {
        if (!Native.IsWindow(hwnd) || !Native.GetWindowRect(hwnd, out _) ||
            !Screen.AllScreens.Any(screen => screen.DeviceName == target.DeviceName))
            return false;

        var source = Screen.FromHandle(hwnd);
        if (source.DeviceName == target.DeviceName) return false;

        bool maximized = Native.IsZoomed(hwnd);
        if (maximized) Native.ShowWindow(hwnd, Native.SwRestore);
        if (!Native.GetWindowRect(hwnd, out var current)) return false;

        // Keep the same relative position inside each monitor's usable area.
        var destination = MapWindow(current.ToRectangle(), source.WorkingArea, target.WorkingArea);
        bool moved = Native.SetWindowPos(hwnd, IntPtr.Zero,
            destination.Left, destination.Top, destination.Width, destination.Height,
            Native.SwpNoZOrder | Native.SwpNoActivate);
        if (maximized) Native.ShowWindow(hwnd, Native.SwMaximize);
        return moved;
    }

    private static Point Center(Rectangle rectangle) =>
        new(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2);
}
