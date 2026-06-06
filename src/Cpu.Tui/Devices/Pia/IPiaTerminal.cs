namespace Cpu.Tui.Devices.Pia;

/// <summary>
/// Interface for terminal adapters connected to PIA.
/// Called on port writes and IRQ events.
/// </summary>
public interface IPiaTerminal
{
    /// <summary>
    /// Called when Port A is written.
    /// </summary>
    void OnPortAWrite(byte newValue, byte oldValue);

    /// <summary>
    /// Called when Port B is written.
    /// </summary>
    void OnPortBWrite(byte newValue, byte oldValue);

    /// <summary>
    /// Called when IRQ is triggered.
    /// </summary>
    void OnIrq(IrqSource source);
}
