using System.Diagnostics;
using System.Text;

namespace MonitorHop;

internal sealed record VisibleApp(string Name, int Instances, string WindowTitle);

internal static class VisibleAppScanner
{
    public static List<VisibleApp> GetVisibleApps()
    {
        var screens = Screen.AllScreens.Select(screen => screen.Bounds).ToArray();
        var groups = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);

        Native.EnumWindowsProc callback = (hwnd, _) =>
        {
            if (!Native.IsWindowVisible(hwnd) || Native.IsIconic(hwnd) ||
                Native.GetAncestor(hwnd, Native.GaRoot) != hwnd ||
                Native.GetWindowTextLength(hwnd) == 0 ||
                !Native.GetWindowRect(hwnd, out var nativeRect))
                return true;

            Rectangle rect = nativeRect.ToRectangle();
            if (rect.Width < 120 || rect.Height < 80 ||
                !screens.Any(screen => screen.IntersectsWith(rect)))
                return true;

            var className = new StringBuilder(128);
            Native.GetClassName(hwnd, className, className.Capacity);
            if (className.ToString() is "Progman" or "WorkerW" or
                "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
                return true;

            if (Native.DwmGetWindowAttributeInt(hwnd, Native.DwmwaCloaked,
                    out int cloaked, sizeof(int)) == 0 && cloaked != 0)
                return true;

            Native.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0 || processId == Environment.ProcessId) return true;

            try
            {
                using var process = Process.GetProcessById((int)processId);
                string name = process.ProcessName;
                if (name.Length == 0) return true;
                int length = Math.Min(1024, Native.GetWindowTextLength(hwnd) + 1);
                var titleBuffer = new StringBuilder(length);
                Native.GetWindowText(hwnd, titleBuffer, length);
                string title = titleBuffer.ToString();

                if (!groups.TryGetValue(name, out var group))
                {
                    group = new Group();
                    groups[name] = group;
                }
                group.ProcessIds.Add(processId);
                if (group.WindowTitle.Length == 0) group.WindowTitle = title;
            }
            catch (ArgumentException) { /* Process closed during enumeration. */ }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            return true;
        };

        Native.EnumWindows(callback, IntPtr.Zero);
        return groups.Select(item => new VisibleApp(item.Key,
                item.Value.ProcessIds.Count, item.Value.WindowTitle))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class Group
    {
        public HashSet<uint> ProcessIds { get; } = [];
        public string WindowTitle { get; set; } = "";
    }
}
