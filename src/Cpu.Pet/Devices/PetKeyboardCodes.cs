namespace Cpu.Pet.Devices;

/// <summary>
/// PET 2001 keyboard buffer codes (PETSCII key codes, not screen codes).
/// Derived from editor ROM ($E2xx input loop, key table at $E6F7).
/// </summary>
public static class PetKeyboardCodes
{
    public const byte Return = 0x0D;
    public const byte Space = 0x20;

    /// <summary>Left-arrow / delete previous character (editor CMP #$14 handlers).</summary>
    public const byte DeleteLeft = 0x14;

    /// <summary>Direct-command macro slot — editor injects "LOAD" from $E760 table, not backspace.</summary>
    public const byte LoadDirectCommand = 0x83;
}
