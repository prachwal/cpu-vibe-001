using Cpu.Module;
using Cpu.Tui.Diagnostics;
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
    protected ErrorCollector? Errors { get; }
    protected const int PanelWidth = 30;

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

    protected ModuleBase(ErrorCollector? errors = null)
    {
        Errors = errors;
    }

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

    public bool OnMouse(MouseEvent e) => false;
    public virtual bool OnTick() => false;

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        bool showPanel = w >= 80 + PanelWidth + 6;
        int renderX = showPanel ? PanelWidth + 2 : 0;
        int renderW = showPanel ? w - PanelWidth - 2 : w;

        int contentH = h - 1;
        RenderContent(r, renderX, 0, renderW, contentH);

        if (showPanel)
        {
            ClearPanel(r, 0, 0, PanelWidth, contentH);
            int y = 1;
            RenderPanelHeader(r, PanelWidth, ref y);
            RenderPanelInfo(r, PanelWidth, ref y);
            RenderPanelControls(r, PanelWidth, ref y);
        }
    }

    protected abstract void RenderContent(ITerminalRenderer r, int x, int y, int w, int h);

    protected virtual void RenderPanelHeader(ITerminalRenderer r, int w, ref int y)
    {
        PanelLine(r, w, y++, $" {Name}", ConsoleColor.Cyan);
        y++;
    }

    protected abstract void RenderPanelInfo(ITerminalRenderer r, int w, ref int y);

    protected virtual void RenderPanelControls(ITerminalRenderer r, int w, ref int y)
    {
        if (y < 2) y = 2;
        if (y > 0) y++;
        PanelLine(r, w, y++, " Controls", ConsoleColor.Cyan);
        y++;
        PanelLine(r, w, y++, " Esc back");
    }

    protected static void ClearPanel(ITerminalRenderer r, int x, int y, int w, int h)
    {
        for (int i = 0; i < h; i++)
            for (int c = 0; c < w; c++)
                r.SetCell(x + c, y + i, ' ', ConsoleColor.Gray, ConsoleColor.DarkBlue);
    }

    protected static void PanelLine(ITerminalRenderer r, int w, int y, string text, ConsoleColor? fg = null)
    {
        var color = fg ?? ConsoleColor.Gray;
        int max = w;
        if (text.Length > max) text = text[..max];
        if (text.Length < max) text += new string(' ', max - text.Length);
        TermArea.Write(r, new TermRect(0, 0, w, 100), 0, y, text, color, ConsoleColor.DarkBlue);
    }
}
