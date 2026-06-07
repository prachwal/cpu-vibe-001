namespace Cpu.Tui;

public static class TerminalGraphicsModes
{
    /// <summary>
    /// Reference cell layout for FitImage and content scaling. All modes share the same
    /// terminal frame (cols × rows) and the same source scale; render density differs only
    /// inside each cell.
    /// </summary>
    public const double FrameLayoutPixelsPerCellColumn = 1.0;
    public const double FrameLayoutPixelsPerCellRow = 2.0;

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

    public static double LayoutPixelsPerCellRow(TerminalGraphicsMode _) =>
        FrameLayoutPixelsPerCellRow;

    public static double LayoutPixelsPerCellColumn(TerminalGraphicsMode _) =>
        FrameLayoutPixelsPerCellColumn;

    public static (int Width, int Height) FrameTargetPixelSize(int cols, int rows) =>
        (Math.Max(1, cols), Math.Max(1, (int)Math.Ceiling(rows * FrameLayoutPixelsPerCellRow)));

    public static bool PrefersBilinearResize(TerminalGraphicsMode mode) =>
        mode is TerminalGraphicsMode.TrueTone or TerminalGraphicsMode.BestGlyphTrueColor;

    public static (int Width, int Height) TargetPixelSize(int cols, int rows, TerminalGraphicsMode mode)
    {
        int width = Math.Max(1, (int)Math.Ceiling(cols * PixelsPerCellColumn(mode)));
        int height = Math.Max(1, (int)Math.Ceiling(rows * PixelsPerCellRow(mode)));
        return (width, height);
    }
}
