using Cpu.Tui.Rendering;

namespace Cpu.Tui;

public sealed class TuiAppSettings
{
    public TuiTheme Theme { get; set; } = TuiTheme.Classic;
    public FrameStyle FrameStyle { get; set; } = FrameStyle.Unicode;
    public PanelSide PanelSide { get; set; } = PanelSide.Left;
    public TerminalGraphicsMode DefaultGraphicsMode { get; set; } = TerminalGraphicsMode.HalfBlockColor;

    public static TuiAppSettings Default { get; } = new();

    public TuiAppSettings Clone() => new()
    {
        Theme = Theme,
        FrameStyle = FrameStyle,
        PanelSide = PanelSide,
        DefaultGraphicsMode = DefaultGraphicsMode
    };
}
