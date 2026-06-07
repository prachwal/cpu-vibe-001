namespace Cpu.Tui.Graphics;

/// <summary>
/// Maps RGB colors to ConsoleColor (16-color palette).
/// </summary>
public static class ColorQuantizer
{
    public static ConsoleColor NearestConsoleColor(byte r, byte g, byte b) =>
        TerminalPalette16.Nearest(r, g, b);

    public static (byte R, byte G, byte B) ConsoleColorToRgb(ConsoleColor c) =>
        TerminalPalette16.ToRgb(c);
}
