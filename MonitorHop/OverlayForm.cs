using System.Drawing.Drawing2D;

namespace MonitorHop;

internal sealed class OverlayForm : Form
{
    private static readonly Direction[] Directions =
        [Direction.Left, Direction.Right, Direction.Up, Direction.Down];
    private static readonly string[] Glyphs = ["←", "→", "↑", "↓"];
    private const int ClosedWidth = 58;
    private const int ClosedHeight = 48;
    private const int MenuWidth = 300;
    private const int MenuHeight = 277;
    private const int MaxOffsetX = 300;
    private const int MaxOffsetY = 150;
    private readonly System.Windows.Forms.Timer _hoverTimer = new() { Interval = 350 };
    private bool _expanded;
    private bool _pointerDown;
    private bool _dragging;
    private bool _suppressClick;
    private Point _dragStartCursor;
    private Point _dragStartOffset;
    private Point _offset;
    private int _hovered = -1;
    private Rectangle _anchor;
    private IntPtr _window;
    private Screen? _source;
    private string _language = "ru";
    private bool _showMonitorMap;
    private int _iconOpacityPercent = 78;
    private int _iconSizePercent = 100;
    private string _monitorLayout = "";

    public event Action<Direction>? DirectionChosen;
    public event Action<Screen>? MonitorChosen;
    public event Action? IgnoreRequested;
    public event Action? SettingsRequested;
    public event Action<Point>? PositionOffsetChanged;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        Opacity = 0.78;
        BackColor = Color.FromArgb(20, 35, 54);
        ClientSize = new Size(ClosedWidth, ClosedHeight);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        _hoverTimer.Tick += (_, _) =>
        {
            _hoverTimer.Stop();
            if (!_pointerDown && !_dragging && ClientRectangle.Contains(PointToClient(Cursor.Position)))
                Expand();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hoverTimer.Dispose();
        base.Dispose(disposing);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= 0x08000000 | 0x00000080; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
            return parameters;
        }
    }

    public void SetLanguage(string language)
    {
        _language = Texts.NormalizeLanguage(language);
        Invalidate();
    }

    public void SetDisplayMode(bool showMonitorMap)
    {
        _showMonitorMap = showMonitorMap;
        Invalidate();
    }

    public void SetPositionOffset(int x, int y)
    {
        var next = new Point(Math.Clamp(x, -MaxOffsetX, MaxOffsetX),
            Math.Clamp(y, -MaxOffsetY, MaxOffsetY));
        if (_offset == next) return;
        _offset = next;
        Position();
    }

    public void SetAppearance(int opacityPercent, int sizePercent)
    {
        int opacity = Math.Clamp(opacityPercent, 30, 100);
        int size = Math.Clamp(sizePercent, 60, 160);
        if (_iconOpacityPercent == opacity && _iconSizePercent == size) return;
        _iconOpacityPercent = opacity;
        _iconSizePercent = size;
        if (_expanded) return;
        Opacity = _iconOpacityPercent / 100.0;
        ClientSize = ClosedSize;
        Position();
        Invalidate();
    }

    private Size ClosedSize => new(
        (int)Math.Round(ClosedWidth * _iconSizePercent / 100.0),
        (int)Math.Round(ClosedHeight * _iconSizePercent / 100.0));

    public void Attach(Rectangle window, Screen source, IntPtr hwnd)
    {
        string layout = string.Join("|", Screen.AllScreens.Select(screen =>
            $"{screen.DeviceName}:{screen.Bounds.Left},{screen.Bounds.Top},{screen.Bounds.Width},{screen.Bounds.Height}"));
        bool changed = _anchor != window || _window != hwnd ||
            _source?.DeviceName != source.DeviceName || _monitorLayout != layout;
        _anchor = window;
        _window = hwnd;
        _source = source;
        _monitorLayout = layout;
        if (changed) Position();
        if (changed) Invalidate();
        if (!Visible) Show();
    }

    public void Detach()
    {
        _hoverTimer.Stop();
        _pointerDown = false;
        _dragging = false;
        Capture = false;
        if (Visible) Hide();
        _expanded = false;
        _hovered = -1;
        Opacity = _iconOpacityPercent / 100.0;
        ClientSize = ClosedSize;
        _anchor = Rectangle.Empty;
        _window = IntPtr.Zero;
        _source = null;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Width < 20 || Height < 20) return;
        using var path = RoundedPath(new Rectangle(0, 0, Width, Height));
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!_expanded && !_pointerDown) _hoverTimer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverTimer.Stop();
        if (_pointerDown || _dragging) return;
        if (ClientRectangle.Contains(PointToClient(Cursor.Position))) return;
        Collapse();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || (_expanded && !DragHandle().Contains(e.Location))) return;
        _hoverTimer.Stop();
        _pointerDown = true;
        _dragging = false;
        _dragStartCursor = Cursor.Position;
        _dragStartOffset = _offset;
        Capture = true;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !_pointerDown) return;
        _pointerDown = false;
        Capture = false;
        if (!_dragging) return;
        _dragging = false;
        _suppressClick = true;
        if (_offset != _dragStartOffset) PositionOffsetChanged?.Invoke(_offset);
        if (_expanded && !ClientRectangle.Contains(PointToClient(Cursor.Position))) Collapse();
        if (IsHandleCreated) BeginInvoke(() => _suppressClick = false);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_pointerDown)
        {
            Point delta = new(Cursor.Position.X - _dragStartCursor.X,
                Cursor.Position.Y - _dragStartCursor.Y);
            if (!_dragging && (Math.Abs(delta.X) > 4 || Math.Abs(delta.Y) > 4))
            {
                _dragging = true;
                _hoverTimer.Stop();
            }
            if (_dragging)
            {
                double scale = _window == IntPtr.Zero ? 1 : Math.Max(1, Native.GetDpiForWindow(_window)) / 96.0;
                SetPositionOffset(_dragStartOffset.X + (int)Math.Round(delta.X / scale),
                    _dragStartOffset.Y + (int)Math.Round(delta.Y / scale));
            }
            return;
        }
        int hovered = HitTest(e.Location);
        if (hovered == _hovered) return;
        _hovered = hovered;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (_suppressClick) { _suppressClick = false; return; }
        if (e.Button != MouseButtons.Left) return;
        if (!_expanded) { Expand(); return; }
        int hit = HitTest(e.Location);
        if (hit == 4) { IgnoreRequested?.Invoke(); return; }
        if (hit == 5) { SettingsRequested?.Invoke(); return; }
        if (hit >= 100)
        {
            var screens = Screen.AllScreens;
            int index = hit - 100;
            if (index < screens.Length && screens[index].DeviceName != _source?.DeviceName)
                MonitorChosen?.Invoke(screens[index]);
            return;
        }
        if (hit is < 0 or > 3 || _source is null) return;
        var direction = Directions[hit];
        if (MonitorMover.FindNeighbor(_source, direction) is not null)
            DirectionChosen?.Invoke(direction);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var border = new Pen(Color.FromArgb(81, 172, 203), 1.3f);
        using var path = RoundedPath(new Rectangle(1, 1, Width - 3, Height - 3));
        graphics.DrawPath(border, path);

        if (!_expanded)
        {
            graphics.DrawImage(Branding.Image, new Rectangle(
                (int)Math.Round(7 * _iconSizePercent / 100.0),
                (int)Math.Round(3 * _iconSizePercent / 100.0),
                (int)Math.Round(44 * _iconSizePercent / 100.0),
                (int)Math.Round(42 * _iconSizePercent / 100.0)));
            return;
        }

        using var headingFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 10);
        using var arrowFont = new Font("Segoe UI Symbol", 19, FontStyle.Bold);
        DrawFittedText(graphics, Texts.Get(_language,
                _showMonitorMap ? "MonitorMapTitle" : "MoveWindow"), headingFont,
            new Rectangle(14, 15, 226, 27), Color.White, TextFormatFlags.Left);
        using var hintFont = new Font("Segoe UI", 8);
        DrawFittedText(graphics, Texts.Get(_language, "DragHint"), hintFont,
            new Rectangle(14, 39, 225, 16), Color.FromArgb(171, 206, 223),
            TextFormatFlags.Left);
        graphics.DrawImage(Branding.Image, new Rectangle(247, 3, 43, 43));

        if (_showMonitorMap)
            PaintMonitorMap(graphics, headingFont);
        else
        {
            for (int i = 0; i < 4; i++)
            {
                var rectangle = ButtonRect(i);
                bool available = _source is not null && MonitorMover.FindNeighbor(_source, Directions[i]) is not null;
                PaintButton(graphics, rectangle, available, _hovered == i);
                Color ink = available ? Color.White : Color.FromArgb(118, 137, 153);
                TextRenderer.DrawText(graphics, Glyphs[i], arrowFont,
                    new Rectangle(rectangle.Left + 12, rectangle.Top + 9, 36, 36), ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                DrawFittedText(graphics, Texts.Get(_language, Directions[i].ToString()), labelFont,
                    new Rectangle(rectangle.Left + 52, rectangle.Top + 13, 76, 31), ink,
                    TextFormatFlags.Left);
            }
        }

        var ignore = IgnoreRect();
        PaintButton(graphics, ignore, true, _hovered == 4);
        DrawFittedText(graphics, "⊘  " + Texts.Get(_language, "IgnoreApp"), labelFont,
            Rectangle.Inflate(ignore, -10, 0), Color.White, TextFormatFlags.HorizontalCenter);

        var settings = SettingsRect();
        PaintButton(graphics, settings, true, _hovered == 5);
        DrawFittedText(graphics, "⚙  " + Texts.Get(_language, "Settings"), labelFont,
            Rectangle.Inflate(settings, -10, 0), Color.White, TextFormatFlags.HorizontalCenter);
    }

    private static void DrawFittedText(Graphics graphics, string text, Font font,
        Rectangle bounds, Color color, TextFormatFlags alignment)
    {
        const TextFormatFlags flags = TextFormatFlags.SingleLine |
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
            TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping;
        Font? smaller = null;
        try
        {
            if (TextRenderer.MeasureText(graphics, text, font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width <= bounds.Width)
            {
                TextRenderer.DrawText(graphics, text, font, bounds, color, flags | alignment);
                return;
            }
            for (float size = font.Size - 0.5f; size >= 7; size -= 0.5f)
            {
                Font candidate = new(font.FontFamily, size, font.Style);
                if (TextRenderer.MeasureText(graphics, text, candidate, Size.Empty,
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width <= bounds.Width)
                {
                    smaller = candidate;
                    break;
                }
                candidate.Dispose();
            }
            TextRenderer.DrawText(graphics, text, smaller ?? font, bounds, color,
                flags | alignment);
        }
        finally { smaller?.Dispose(); }
    }

    private static void PaintButton(Graphics graphics, Rectangle rect, bool enabled, bool hovered)
    {
        using var fill = new SolidBrush(!enabled
            ? Color.FromArgb(29, 43, 59)
            : hovered ? Color.FromArgb(46, 101, 137) : Color.FromArgb(34, 62, 84));
        using var line = new Pen(hovered && enabled
            ? Color.FromArgb(117, 215, 244) : Color.FromArgb(72, 114, 140), 1);
        using var path = RoundedPath(rect, 12);
        graphics.FillPath(fill, path);
        graphics.DrawPath(line, path);
    }

    private static Rectangle ButtonRect(int index) => index switch
    {
        0 => new Rectangle(12, 57, 132, 58),
        1 => new Rectangle(156, 57, 132, 58),
        2 => new Rectangle(12, 125, 132, 58),
        _ => new Rectangle(156, 125, 132, 58)
    };

    private static Rectangle IgnoreRect() => new(12, 193, 276, 36);
    private static Rectangle SettingsRect() => new(12, 238, 276, 29);
    private static Rectangle DragHandle() => new(240, 0, 60, 54);

    private void PaintMonitorMap(Graphics graphics, Font font)
    {
        var tiles = MonitorTiles();
        for (int i = 0; i < tiles.Count; i++)
        {
            var (screen, rectangle) = tiles[i];
            bool current = screen.DeviceName == _source?.DeviceName;
            PaintButton(graphics, rectangle, !current, _hovered == 100 + i);
            if (current)
            {
                using var outline = new Pen(Color.FromArgb(112, 221, 246), 2);
                graphics.DrawRectangle(outline, rectangle);
            }
            TextRenderer.DrawText(graphics, (i + 1).ToString(), font, rectangle,
                current ? Color.FromArgb(117, 224, 247) : Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    private List<(Screen Screen, Rectangle Rect)> MonitorTiles()
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0) return [];
        Rectangle all = screens[0].Bounds;
        foreach (var screen in screens.Skip(1)) all = Rectangle.Union(all, screen.Bounds);
        var area = new Rectangle(12, 55, 276, 131);
        double scale = Math.Min((double)area.Width / Math.Max(1, all.Width),
            (double)area.Height / Math.Max(1, all.Height));
        double originX = area.Left + (area.Width - all.Width * scale) / 2;
        double originY = area.Top + (area.Height - all.Height * scale) / 2;
        var tiles = new List<(Screen Screen, Rectangle Rect)>();
        foreach (var screen in screens)
        {
            var bounds = screen.Bounds;
            var rect = new Rectangle(
                (int)Math.Round(originX + (bounds.Left - all.Left) * scale) + 3,
                (int)Math.Round(originY + (bounds.Top - all.Top) * scale) + 3,
                Math.Max(18, (int)Math.Round(bounds.Width * scale) - 6),
                Math.Max(18, (int)Math.Round(bounds.Height * scale) - 6));
            tiles.Add((screen, rect));
        }
        return tiles;
    }

    private int HitTest(Point point)
    {
        if (!_expanded) return -1;
        if (_showMonitorMap)
        {
            var tiles = MonitorTiles();
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i].Rect.Contains(point)) return 100 + i;
        }
        else
        {
        for (int i = 0; i < 4; i++)
            if (ButtonRect(i).Contains(point)) return i;
        }
        if (IgnoreRect().Contains(point)) return 4;
        return SettingsRect().Contains(point) ? 5 : -1;
    }

    private void Expand()
    {
        if (_expanded) return;
        _hoverTimer.Stop();
        _expanded = true;
        Opacity = 0.96;
        ClientSize = new Size(MenuWidth, MenuHeight);
        Position();
        Invalidate();
    }

    private void Collapse()
    {
        if (!_expanded) return;
        _expanded = false;
        _hovered = -1;
        Opacity = _iconOpacityPercent / 100.0;
        ClientSize = ClosedSize;
        Position();
        Invalidate();
    }

    private void Position()
    {
        if (_anchor.IsEmpty || _window == IntPtr.Zero) return;
        var work = Screen.FromHandle(_window).WorkingArea;
        uint dpi = Native.GetDpiForWindow(_window);
        double scale = dpi > 0 ? dpi / 96.0 : 1;
        int captionSpace = (int)Math.Round(3 * 46 * scale + 12 * scale);
        int collapsedRight = Math.Max(_anchor.Left + ClosedSize.Width + 8, _anchor.Right - captionSpace);
        int x = collapsedRight - Width + (int)Math.Round(_offset.X * scale);
        x = Math.Clamp(x, work.Left, Math.Max(work.Left, work.Right - Width));
        int y = Math.Clamp(_anchor.Top + 3 + (int)Math.Round(_offset.Y * scale),
            work.Top, Math.Max(work.Top, work.Bottom - Height));
        Location = new Point(x, y);
    }

    private static GraphicsPath RoundedPath(Rectangle rect, int radius = 14)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
