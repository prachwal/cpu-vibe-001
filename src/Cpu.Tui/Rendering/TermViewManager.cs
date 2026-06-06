namespace Cpu.Tui.Rendering;

/// <summary>
/// Manages view switching with automatic area clearing on deactivation.
/// Ensures no artifacts when switching between views.
/// </summary>
public sealed class TermViewManager
{
    private readonly ITerminalRenderer _renderer;
    private ITermView? _active;
    private TermRect _lastArea;

    public ITermView? Active => _active;

    public TermViewManager(ITerminalRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Switch to a new view. Deactivates old, activates new.
    /// </summary>
    public void SwitchTo(ITermView newView, TermRect terminalArea)
    {
        if (_active != null)
            _active.Deactivate(_renderer, _lastArea);

        _active = newView;
        _lastArea = terminalArea;
        _active.Activate(_renderer, terminalArea);
    }

    /// <summary>
    /// Render the active view. Updates stored area for proper deactivation on resize.
    /// </summary>
    public void Render(TermRect terminalArea)
    {
        if (_active == null) return;

        // Handle terminal resize: deactivate old area, re-activate with new area
        if (_lastArea != terminalArea)
        {
            _active.Deactivate(_renderer, _lastArea);
            _active.Activate(_renderer, terminalArea);
        }

        _lastArea = terminalArea;
        _active.Render(_renderer, terminalArea);
        _renderer.Flush();
    }
}
