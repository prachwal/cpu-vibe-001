namespace Cpu.Tui.Rendering;

/// <summary>
/// Single terminal cell state. Supports both ConsoleColor and truecolor RGB.
/// </summary>
public readonly record struct TerminalCell(char Ch, TerminalColor Fg, TerminalColor Bg)
{
    public static readonly TerminalCell Unknown =
        new('\0', TerminalColor.FromConsole(ConsoleColor.Black), TerminalColor.FromConsole(ConsoleColor.Black));
    public static readonly TerminalCell Empty =
        new(' ', TerminalColor.FromConsole(ConsoleColor.Gray), TerminalColor.FromConsole(ConsoleColor.Black));

    public TerminalCell(char ch, ConsoleColor fg, ConsoleColor bg)
        : this(ch, TerminalColor.FromConsole(fg), TerminalColor.FromConsole(bg))
    {
    }
}
