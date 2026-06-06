namespace Cpu.Tui.Rendering;

/// <summary>
/// A full-screen terminal view with lifecycle management.
/// On deactivation, the view clears its area to prevent artifacts.
/// On activation, the view re-renders its content.
/// </summary>
public interface ITermView
{
    string Name { get; }
    void Activate(ITerminalRenderer renderer, TermRect terminalArea);
    void Deactivate(ITerminalRenderer renderer, TermRect terminalArea);
    void Render(ITerminalRenderer renderer, TermRect terminalArea);
}
