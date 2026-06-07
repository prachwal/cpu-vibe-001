using Cpu.Help.Rendering.Views;
using Cpu.Module;
using Cpu.Tui.Rendering;

namespace Cpu.Help.Modules;

public sealed class HelpModule : IAppModule
{
    private readonly HelpView _view = new();

    public string Name => "Help";
    public ConsoleKey? ActivateKey => ConsoleKey.F1;
    public string ActivateLabel => "F1 Help";
    public bool IsTransient => false;
    public bool ShowInBar => true;
    public bool IsActive { get; private set; }

    public void OnActivate() { IsActive = true; }
    public void OnDeactivate() { IsActive = false; }
    public bool OnKey(ConsoleKeyInfo key) => false;
    public bool OnTick() => false;

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        _view.Render(r, new TermRect(0, 0, w, h - 1));
    }
}
