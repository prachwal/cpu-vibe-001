namespace Cpu.Tui.Rendering;

/// <summary>
/// Terminal rectangle — position and size.
/// </summary>
public readonly record struct TermRect(int X, int Y, int W, int H)
{
    public int X2 => X + W;
    public int Y2 => Y + H;

    /// <summary>Center content of given size within this rectangle.</summary>
    public TermRect CenterFrame(int contentW, int contentH)
    {
        int fw = contentW + 2, fh = contentH + 2;
        int fx = Math.Max(1, (W - fw) / 2);
        int fy = Math.Max(1, (H - fh) / 2);
        return new TermRect(fx, fy, fw, fh);
    }

    /// <summary>Inner content rectangle (inside a frame).</summary>
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    public TermRect Inner => new(X + 1, Y + 1, W - 2, H - 2);

    public override string ToString() => $"({X},{Y},{W},{H})";
}
