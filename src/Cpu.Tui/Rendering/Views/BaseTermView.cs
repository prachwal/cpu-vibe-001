namespace Cpu.Tui.Rendering.Views;

public abstract class BaseTermView : ITermView
{
    public abstract string Name { get; }
    protected bool _seeded;

    public virtual void Activate(ITerminalRenderer r, TermRect area)
    {
        if (!_seeded) { Seed(); _seeded = true; }
        Render(r, area);
    }

    public virtual void Deactivate(ITerminalRenderer r, TermRect area)
    {
        TermArea.Clear(r, area, TerminalCell.Black, "BaseTermView.Deactivate");
    }

    public abstract void Render(ITerminalRenderer r, TermRect area, PresentationSession session);

    public void Render(ITerminalRenderer r, TermRect area)
    {
        Render(r, area, new PresentationSession(r, area));
    }

    protected abstract void Seed();
}
