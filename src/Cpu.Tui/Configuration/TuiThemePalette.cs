namespace Cpu.Tui.Configuration;

public readonly record struct TuiThemePalette(
    ConsoleColor PanelFg,
    ConsoleColor PanelBg,
    ConsoleColor HeaderFg,
    ConsoleColor FrameFg,
    ConsoleColor ContentFg,
    ConsoleColor ContentBg)
{
    public static TuiThemePalette For(TuiTheme theme) => theme switch
    {
        TuiTheme.Neon => new(ConsoleColor.White, ConsoleColor.DarkMagenta,
            ConsoleColor.Yellow, ConsoleColor.Magenta, ConsoleColor.Gray, ConsoleColor.Black),
        TuiTheme.Monochrome => new(ConsoleColor.White, ConsoleColor.DarkGray,
            ConsoleColor.White, ConsoleColor.Gray, ConsoleColor.Gray, ConsoleColor.Black),
        _ => new(ConsoleColor.Gray, ConsoleColor.DarkBlue,
            ConsoleColor.Cyan, ConsoleColor.DarkCyan, ConsoleColor.Gray, ConsoleColor.Black)
    };
}
