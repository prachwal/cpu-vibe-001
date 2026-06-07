using Cpu.Module;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Configuration;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Modules;

public abstract class ModuleBase : IAppModule
{
    public abstract string Name { get; }
    public IAppModule? Parent { get; set; }
    private IAppModule? _child;
    public IAppModule? Child => _child;
    public bool IsActive { get; private set; }
    public virtual bool WantsMouse => true;
    protected ITuiAppConfiguration Config { get; }
    protected ErrorCollector? Errors { get; }
    protected const int PanelWidth = 30;
    private int _panelOriginX;

    public void SetChild(IAppModule? child)
    {
        _child = child;
        if (child != null) child.Parent = this as IAppModule;
    }

    public void CloseChild()
    {
        if (_child == null) return;
        _child.OnDeactivate();
        _child.Parent = null;
        _child = null;
    }

    protected ModuleBase(ITuiAppConfiguration config, ErrorCollector? errors = null)
    {
        Config = config;
        Errors = errors;
    }

    protected void SyncViewSettings(ITuiSettingsConsumer view) =>
        view.ApplySettings(Config.Current);

    protected TuiThemePalette ThemePalette => TuiThemePalette.For(Config.Current.Theme);

    protected FrameStyle ConfigFrameStyle => Config.Current.FrameStyle;

    public void OnActivate()
    {
        IsActive = true;
        OnActivateCore();
    }

    public void OnDeactivate()
    {
        IsActive = false;
        OnDeactivateCore();
    }

    protected virtual void OnActivateCore() { }
    protected virtual void OnDeactivateCore() { }

    public bool OnKey(ConsoleKeyInfo key) => OnKeyCore(key);
    protected abstract bool OnKeyCore(ConsoleKeyInfo key);

    public virtual bool OnMouse(MouseEvent e) => false;
    public virtual bool OnTick() => false;

    public virtual void OnRender(ITerminalRenderer r, int w, int h)
    {
        bool showPanel = w >= 80 + PanelWidth + 6;
        int contentH = h - 1;
        int panelX = 0;
        int renderX = 0;
        int renderW = w;

        if (showPanel)
        {
            bool panelLeft = Config.Current.PanelSide == PanelSide.Left;
            panelX = panelLeft ? 0 : w - PanelWidth;
            renderX = panelLeft ? PanelWidth + 2 : 0;
            renderW = w - PanelWidth - 2;
        }

        RenderContent(r, renderX, 0, renderW, contentH);

        if (showPanel)
        {
            _panelOriginX = panelX;
            var palette = ThemePalette;
            ClearPanel(r, panelX, 0, PanelWidth, contentH, palette.PanelFg, palette.PanelBg);
            int y = 1;
            RenderPanelHeader(r, PanelWidth, ref y, palette);
            RenderPanelInfo(r, PanelWidth, ref y);
            RenderPanelControls(r, PanelWidth, ref y);
        }
    }

    protected abstract void RenderContent(ITerminalRenderer r, int x, int y, int w, int h);

    protected virtual void RenderPanelHeader(ITerminalRenderer r, int w, ref int y, TuiThemePalette palette)
    {
        PanelLine(r, w, y++, $" {Name}", palette.HeaderFg, palette.PanelBg);
        y++;
    }

    protected abstract void RenderPanelInfo(ITerminalRenderer r, int w, ref int y);

    protected virtual void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        var palette = ThemePalette;
        if (y < 2) y = 2;
        if (y > 0) y++;
        PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan, palette.PanelBg);
        y++;
        PanelLine(r, w, y++, " Esc back", palette.PanelFg, palette.PanelBg);
    }

    protected void SetPanelOriginX(int x) => _panelOriginX = x;

    protected void PanelLine(ITerminalRenderer r, int w, int y, string text, ConsoleColor fg, ConsoleColor bg)
    {
        if (text.Length > w) text = text[..w];
        if (text.Length < w) text += new string(' ', w - text.Length);
        TermArea.Write(r, new TermRect(_panelOriginX, 0, w, 100), 0, y, text, fg, bg);
    }

    protected void PanelLine(ITerminalRenderer r, int w, int y, string text, ConsoleColor? fg = null)
    {
        var palette = ThemePalette;
        PanelLine(r, w, y, text, fg ?? palette.PanelFg, palette.PanelBg);
    }

    protected static void ClearPanel(ITerminalRenderer r, int x, int y, int w, int h,
        ConsoleColor fg, ConsoleColor bg)
    {
        for (int i = 0; i < h; i++)
            for (int c = 0; c < w; c++)
                r.SetCell(x + c, y + i, ' ', fg, bg);
    }
}
