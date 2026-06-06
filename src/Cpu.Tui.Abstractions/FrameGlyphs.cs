namespace Cpu.Tui.Rendering;

public enum FrameStyle
{
    Ascii,
    Unicode
}

public readonly record struct FrameGlyphs(
    char TopLeft,
    char TopRight,
    char BottomLeft,
    char BottomRight,
    char Horizontal,
    char Vertical)
{
    public static readonly FrameGlyphs Ascii = new('+', '+', '+', '+', '-', '|');
    public static readonly FrameGlyphs Unicode = new('┌', '┐', '└', '┘', '─', '│');
}
