namespace Cpu.Tui.Rendering;

public interface ITermView
{
    string Name { get; }
    void Activate(ITerminalRenderer renderer, TermRect terminalArea);
    void Deactivate(ITerminalRenderer renderer, TermRect terminalArea);
    void Render(ITerminalRenderer renderer, TermRect terminalArea)
    {
        Render(renderer, terminalArea, new PresentationSession(renderer, terminalArea));
    }
    void Render(ITerminalRenderer renderer, TermRect terminalArea, PresentationSession session);
}
