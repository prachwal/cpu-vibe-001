namespace Cpu.Apple1.Devices;

public sealed record Apple1HostKeyBinding(
    string MachineLabel,
    byte MachineCode,
    string HostLabel,
    bool MapsFromHost = false,
    char? HostChar = null,
    ConsoleKey? HostKey = null,
    bool WozMonitorOnly = false);
