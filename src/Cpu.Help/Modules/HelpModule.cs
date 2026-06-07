using Cpu.Help.Rendering.Views;
using Cpu.Tui;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;

namespace Cpu.Help.Modules;

public sealed class HelpModule : ModuleBase
{
    private readonly HelpView _view = new();
    public override string Name => "Help";

    public HelpModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }

    protected override bool OnKeyCore(ConsoleKeyInfo key) => false;

    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h)
    {
        SyncViewSettings(_view);
        _view.Render(r, new TermRect(x, y, w, h),
            new PresentationSession(r, new TermRect(x, y, w, h)));
    }

    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y) { }
    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y) { }
}
