using Cpu.Tui.Rendering;

namespace Cpu.Module;

public interface IAppModule
{
    string Name { get; }
    ConsoleKey? ActivateKey { get; }
    string ActivateLabel { get; }
    bool IsTransient { get; }
    bool ShowInBar { get; }
    bool IsActive { get; }

    void OnActivate();
    void OnDeactivate();
    bool OnKey(ConsoleKeyInfo key);
    bool OnTick();
    void OnRender(ITerminalRenderer renderer, int termWidth, int termHeight);
}
