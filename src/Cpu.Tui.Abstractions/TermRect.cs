namespace Cpu.Tui.Rendering;

/// <summary>
/// Terminal rectangle — position and size.
/// </summary>
public readonly record struct TermRect(int X, int Y, int W, int H)
{
    public int X2 => X + W;
    public int Y2 => Y + H;

    /// <summary>Center content of given size within this rectangle.</summary>
    /// Clamped to fit inside area — never out of bounds. May return zero-size if impossible.
    public TermRect CenterFrame(int contentW, int contentH)
    {
        int fw = Math.Min(contentW + 2, W);
        int fh = Math.Min(contentH + 2, H);
        int fx = (W - fw) / 2;
        int fy = (H - fh) / 2;
        return new TermRect(X + fx, Y + fy, fw, fh);
    }

    /// <summary>Inner content rectangle (inside a frame).</summary>
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    public TermRect Inner => new(X + 1, Y + 1, W - 2, H - 2);

    public override string ToString() => $"({X},{Y},{W},{H})";
}
