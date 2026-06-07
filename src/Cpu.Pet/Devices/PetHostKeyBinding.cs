namespace Cpu.Pet.Devices;

/// <summary>
/// One PET special-key entry with optional host keyboard mapping.
/// Shared by TUI panels, input routing, and future machine adapters.
/// </summary>
public sealed record PetHostKeyBinding(
    string PetLabel,
    byte PetCode,
    string HostLabel,
    bool MapsFromHost = false,
    char? HostChar = null,
    ConsoleKey? HostKey = null);
