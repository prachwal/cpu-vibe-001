namespace Cpu.Tui.Layout;

public enum ScreenMode
{
    Rows24Cols40,
    Rows25Cols40,
    Rows25Cols80
}

public readonly record struct ScreenSize(int Rows, int Cols);

public readonly record struct TerminalLayout(int Width, int Height)
{
    public int ContentHeight => Math.Max(0, Height - 1);
    public bool CanRender => Width >= 40 && Height >= 25;

    public static TerminalLayout From(int width, int height) =>
        new(Math.Max(1, width), Math.Max(1, height));

    public ScreenSize Fit(ScreenSize requested)
    {
        int cols = Width >= 80 && requested.Cols == 80 ? 80 : 40;
        int rows = ContentHeight >= 25 && requested.Rows == 25 ? 25 : 24;
        return new ScreenSize(rows, cols);
    }
}
