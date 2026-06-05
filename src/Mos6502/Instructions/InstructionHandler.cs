namespace Mos6502.Instructions;

/// <summary>
/// Typ delegate dla wszystkich instrukcji 6502.
/// Każda instrukcja to statyczna metoda o tym sygnaturze.
/// </summary>
public delegate void InstructionHandler(Cpu cpu);
