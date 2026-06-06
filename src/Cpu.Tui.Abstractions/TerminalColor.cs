namespace Cpu.Tui.Rendering;

/// <summary>
/// Terminal color — either a ConsoleColor or a 24-bit RGB value.
/// </summary>
public readonly record struct TerminalColor
{
    public bool IsRgb { get; }
    public ConsoleColor ConsoleColor { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    private TerminalColor(ConsoleColor consoleColor)
    {
        IsRgb = false;
        ConsoleColor = consoleColor;
    }

    private TerminalColor(byte r, byte g, byte b)
    {
        IsRgb = true;
        R = r;
        G = g;
        B = b;
    }

    public static TerminalColor FromConsole(ConsoleColor color) => new(color);
    public static TerminalColor FromRgb(byte r, byte g, byte b) => new(r, g, b);

    public bool Equals(TerminalColor other)
    {
        if (IsRgb != other.IsRgb) return false;
        return IsRgb
            ? R == other.R && G == other.G && B == other.B
            : ConsoleColor == other.ConsoleColor;
    }

    public override int GetHashCode() => IsRgb
        ? HashCode.Combine(R, G, B)
        : ConsoleColor.GetHashCode();
}
