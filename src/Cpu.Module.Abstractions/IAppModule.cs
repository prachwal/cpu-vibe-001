namespace Cpu.Module;

public readonly record struct MouseEvent(int X, int Y, int Button, bool IsRelease, bool IsMotion = false);

public interface IAppModule
{
    string Name { get; }
    IAppModule? Parent { get; set; }
    IAppModule? Child { get; }
    void SetChild(IAppModule? child);
    bool IsActive { get; }

    bool WantsMouse => true;
    void OnActivate();
    void OnDeactivate();
    bool OnKey(ConsoleKeyInfo key);
    bool OnMouse(MouseEvent e);
    bool OnTick();
    void OnRender(Cpu.Tui.Rendering.ITerminalRenderer renderer, int termWidth, int termHeight);
}
