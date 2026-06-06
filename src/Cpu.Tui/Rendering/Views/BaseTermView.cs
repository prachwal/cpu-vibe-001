namespace Cpu.Tui.Rendering.Views;

/// <summary>
/// Base class for terminal views. Automates Activate/Deactivate lifecycle.
/// Subclasses implement Seed() and Render().
/// </summary>
public abstract class BaseTermView : ITermView
{
    public abstract string Name { get; }
    protected bool _seeded;

    /// <summary>
    /// Activate: seed + render. Idempotent after first seed.
    /// </summary>
    public virtual void Activate(ITerminalRenderer r, TermRect area)
    {
        if (!_seeded) { Seed(); _seeded = true; }
        Render(r, area);
    }

    /// <summary>
    /// Deactivate: clear the entire area to prevent artifacts.
    /// </summary>
    public virtual void Deactivate(ITerminalRenderer r, TermRect area)
    {
        TermArea.Clear(r, area);
    }

    /// <summary>
    /// Render current state into the terminal area.
    /// </summary>
    public abstract void Render(ITerminalRenderer r, TermRect area);

    /// <summary>
    /// Seed content once (called lazily on first Activate or when _seeded=false).
    /// </summary>
    protected abstract void Seed();
}
