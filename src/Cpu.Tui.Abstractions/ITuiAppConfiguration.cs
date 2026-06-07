namespace Cpu.Tui;

public interface ITuiAppConfiguration
{
    TuiAppSettings Current { get; }
    event Action? Changed;
    void Apply(TuiAppSettings settings);
    void Reload();
}
