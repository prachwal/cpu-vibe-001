namespace Cpu.Tui.Rendering;

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

    public void SwitchTo(ITermView newView, TermRect terminalArea)
    {
        if (_active != null)
            _active.Deactivate(_renderer, _lastArea);

        _active = newView;
        _lastArea = terminalArea;
        _active.Activate(_renderer, terminalArea);
    }

    public void Render(TermRect terminalArea)
    {
        if (_active == null) return;

        if (_lastArea != terminalArea)
        {
            _active.Deactivate(_renderer, _lastArea);
            _active.Activate(_renderer, terminalArea);
        }

        _lastArea = terminalArea;
        var session = new PresentationSession(_renderer, terminalArea);
        _active.Render(_renderer, terminalArea, session);
        _renderer.Flush();
    }
}
