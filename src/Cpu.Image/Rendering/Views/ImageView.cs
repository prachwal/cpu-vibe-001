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
    private TerminalGraphicsMode _loadedMode;
    private int _loadedMaxCols;
    private int _loadedMaxRows;
    private TerminalGraphicsMode _mode = TerminalGraphicsMode.HalfBlockColor;
    private FrameStyle _frameStyle = FrameStyle.Unicode;
    public override string Name => "Image Viewer";
    public string[] Paths { get => _paths; set { _paths = value; InvalidateLoad(); } }
    public int Index { get => _index; set { _index = value; InvalidateLoad(); } }
    public TerminalGraphicsMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            InvalidateLoad();
        }
    }

    public ImageView() { }
    protected override void Seed() { }

    public void ApplySettings(TuiAppSettings settings)
    {
        _frameStyle = settings.FrameStyle;
        if (_mode != settings.DefaultGraphicsMode)
        {
            _mode = settings.DefaultGraphicsMode;
            InvalidateLoad();
        }
    }

    private void InvalidateLoad()
    {
        _loaded = null;
        _loadedPath = null;
    }

    public void CycleMode() => Mode = TerminalGraphicsModes.Next(_mode);

    public void NextImage() { if (_paths.Length > 0) Index = (_index + 1) % _paths.Length; }
    public void PrevImage() { if (_paths.Length > 0) Index = (_index + _paths.Length - 1) % _paths.Length; }

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
        PanelText(r, x, panelW, y++, " F8  cycle mode");
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
        if (!JpegImageLoader.IsFfmpegAvailable())
        {
            session.Write(2, 2, "ffmpeg not found — install ffmpeg for JPG support", ConsoleColor.Yellow, ConsoleColor.Black);
            return;
        }
        try
        {
            string path = _paths[_index];
            int mc = Math.Max(1, area.W - 4), mr = Math.Max(1, area.H - 4);
            if (_loaded == null || _loadedPath != path || _loadedMode != _mode
                || _loadedMaxCols != mc || _loadedMaxRows != mr)
            {
                (int imgWidth, int imgHeight) = JpegImageLoader.ProbeSize(path);
                (int cols, int rows) = PresentationSession.FitImage(imgWidth, imgHeight, mc, mr, _mode);
                (int pixelWidth, int pixelHeight) = TerminalGraphicsModes.TargetPixelSize(cols, rows, _mode);
                _loaded = JpegImageLoader.Load(path, pixelWidth, pixelHeight);
                _loadedPath = path;
                _loadedMode = _mode;
                _loadedMaxCols = mc;
                _loadedMaxRows = mr;
            }
            if (_loaded == null) return;
            var (fitCols, fitRows) = PresentationSession.FitImage(_loaded, mc, mr, _mode);
            var frame = session.CenterFrame(fitCols, fitRows);
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
