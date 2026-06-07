namespace Cpu.Tui;

public static class TerminalGraphicsModes
{
    public static TerminalGraphicsMode Next(TerminalGraphicsMode mode) => mode switch
    {
        TerminalGraphicsMode.HalfBlockColor => TerminalGraphicsMode.BrailleMono,
        TerminalGraphicsMode.BrailleMono => TerminalGraphicsMode.Grayscale,
        TerminalGraphicsMode.Grayscale => TerminalGraphicsMode.ColorShade,
        TerminalGraphicsMode.ColorShade => TerminalGraphicsMode.TrueTone,
        TerminalGraphicsMode.TrueTone => TerminalGraphicsMode.BestGlyph,
        TerminalGraphicsMode.BestGlyph => TerminalGraphicsMode.BestGlyphTrueColor,
        _ => TerminalGraphicsMode.HalfBlockColor
    };

    public static double PixelsPerCellColumn(TerminalGraphicsMode mode) =>
        mode is TerminalGraphicsMode.BrailleMono
            or TerminalGraphicsMode.BestGlyph
            or TerminalGraphicsMode.BestGlyphTrueColor
            ? 2.0
            : 1.0;

    public static double PixelsPerCellRow(TerminalGraphicsMode mode) => mode switch
    {
        TerminalGraphicsMode.HalfBlockColor => 2.0,
        TerminalGraphicsMode.BrailleMono => 4.0,
        TerminalGraphicsMode.Grayscale => 1.0,
        TerminalGraphicsMode.ColorShade => 1.0,
        TerminalGraphicsMode.TrueTone => 1.0,
        TerminalGraphicsMode.BestGlyph => 4.0,
        TerminalGraphicsMode.BestGlyphTrueColor => 4.0,
        _ => 1.0
    };

    /// <summary>
    /// Row density for FitImage cell layout. 1×1 render modes use 2.0 so the terminal
    /// cell grid matches HalfBlock framing (non-square character cells).
    /// </summary>
    public static double LayoutPixelsPerCellRow(TerminalGraphicsMode mode) => mode switch
    {
        TerminalGraphicsMode.HalfBlockColor => 2.0,
        TerminalGraphicsMode.BrailleMono => 4.0,
        TerminalGraphicsMode.Grayscale => 2.0,
        TerminalGraphicsMode.ColorShade => 2.0,
        TerminalGraphicsMode.TrueTone => 2.0,
        TerminalGraphicsMode.BestGlyph => 4.0,
        TerminalGraphicsMode.BestGlyphTrueColor => 4.0,
        _ => 1.0
    };

    public static double LayoutPixelsPerCellColumn(TerminalGraphicsMode mode) =>
        PixelsPerCellColumn(mode);

    public static bool PrefersBilinearResize(TerminalGraphicsMode mode) =>
        mode is TerminalGraphicsMode.TrueTone or TerminalGraphicsMode.BestGlyphTrueColor;

    public static (int Width, int Height) TargetPixelSize(int cols, int rows, TerminalGraphicsMode mode)
    {
        int width = Math.Max(1, (int)Math.Ceiling(cols * PixelsPerCellColumn(mode)));
        int height = Math.Max(1, (int)Math.Ceiling(rows * PixelsPerCellRow(mode)));
        return (width, height);
    }
}
