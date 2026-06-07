using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Image.Rendering.Views;

public class ImageView : BaseTermView, ITuiSettingsConsumer
{
    private string[] _paths = [];
    private int _index;
    private PixelBuffer? _loaded;
    private string? _loadedPath;
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    public override string Name => "Image Viewer";
    public string[] Paths { get => _paths; set { _paths = value; _loaded = null; _loadedPath = null; } }
    public int Index { get => _index; set { _index = value; _loaded = null; _loadedPath = null; } }
    public TerminalGraphicsMode Mode { get => _mode; set => _mode = value; }

    public ImageView() { }
    protected override void Seed() { }

    public void ApplySettings(TuiAppSettings settings) => _frameStyle = settings.FrameStyle;

    public void NextImage() { if (_paths.Length > 0) Index = (_index + 1) % _paths.Length; }
    public void PrevImage() { if (_paths.Length > 0) Index = (_index + _paths.Length - 1) % _paths.Length; }

    public void CycleMode()
    {
        _mode = _mode switch
        {
            TerminalGraphicsMode.HalfBlockColor => TerminalGraphicsMode.BrailleMono,
            TerminalGraphicsMode.BrailleMono => TerminalGraphicsMode.Grayscale,
            TerminalGraphicsMode.Grayscale => TerminalGraphicsMode.BestGlyph,
            TerminalGraphicsMode.BestGlyph => TerminalGraphicsMode.BestGlyphTrueColor,
            _ => TerminalGraphicsMode.HalfBlockColor
        };
    }

    public void RenderPanel(ITerminalRenderer r, int w, int h, int panelW)
    {
        int x = w - panelW;

        for (int i = 0; i < h; i++)
            for (int c = 0; c < panelW; c++)
                r.SetCell(x + c, i, ' ', ConsoleColor.Gray, ConsoleColor.DarkBlue);

        string name = _paths.Length > 0 ? Path.GetFileName(_paths[_index]) : "(none)";
        string dim = _loaded != null ? $"{_loaded.Width}x{_loaded.Height}" : "-";
        string idx = _paths.Length > 0 ? $"{_index + 1}/{_paths.Length}" : "0/0";
        string mode = _mode.ToString();

        int y = 1;
        PanelText(r, x, panelW, y++, " Image", ConsoleColor.Cyan);
        y++;
        PanelText(r, x, panelW, y++, $" File: {name}");
        PanelText(r, x, panelW, y++, $" Size: {dim}");
        PanelText(r, x, panelW, y++, $" Mode: {mode}");
        PanelText(r, x, panelW, y++, $" Idx: {idx}");
        y++;
        PanelText(r, x, panelW, y++, " Keys", ConsoleColor.Cyan);
        y++;
        PanelText(r, x, panelW, y++, " <-  prev image");
        PanelText(r, x, panelW, y++, " ->  next image");
        PanelText(r, x, panelW, y++, " Up  cycle mode");
        PanelText(r, x, panelW, y++, " Dn  cycle mode");
    }

    private static void PanelText(ITerminalRenderer r, int x, int w, int y, string text, ConsoleColor? fg = null)
    {
        var color = fg ?? ConsoleColor.Gray;
        int max = w;
        if (text.Length > max) text = text[..max];
        if (text.Length < max) text += new string(' ', max - text.Length);
        TermArea.Write(r, new TermRect(0, 0, w, 100), x, y, text, color, ConsoleColor.DarkBlue);
    }

    public override void Render(ITerminalRenderer r, TermRect area, PresentationSession session)
    {
        if (_paths.Length == 0)
        {
            session.Write(2, 2, "No JPG files found in samples/", ConsoleColor.Yellow, ConsoleColor.Black);
            return;
        }
        try
        {
            string path = _paths[_index];
            if (_loaded == null || _loadedPath != path)
            {
                _loaded = JpegImageLoader.Load(path);
                _loadedPath = path;
            }
            if (_loaded == null) return;
            int mc = Math.Max(1, area.W - 4), mr = Math.Max(1, area.H - 4);
            var (cols, rows) = PresentationSession.FitImage(_loaded, mc, mr, _mode);
            var frame = session.CenterFrame(cols, rows);
            session.Clear();
            session.DrawFrame(frame, _frameStyle, $"{Path.GetFileName(path)} {_loaded.Width}x{_loaded.Height} {_mode}");
            session.RenderCanvas(_loaded, _mode, frame.Inner);
        }
        catch (Exception ex)
        {
            session.Write(2, 2, "Image render failed", ConsoleColor.White, ConsoleColor.DarkRed);
            string msg = ex.Message;
            if (msg.Length > area.W - 4) msg = msg[..(area.W - 4)];
            session.Write(2, 4, msg, ConsoleColor.Yellow, ConsoleColor.Black);
        }
    }
}
