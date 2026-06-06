# AGENTS.md — CPU-VIBE-001

## Zakres repo

Repo zawiera dwa emulatory CPU:

- `src/Mos6502` / `tests/Mos6502.Tests` — MOS 6502 i warianty 65C02/2A03.
- `src/Z80` / `tests/Z80.Tests` — Z80, testowany m.in. ROM-ami `zexdoc.com` i `zexall.com`.

Nie zakładaj, że reguły 6502 dotyczą Z80. Przed zmianą sprawdź ścieżkę projektu.

## Reguły wspólne

- .NET 8, C# 12.
- Zgodność binarna z prawdziwym CPU jest ważniejsza niż wygoda implementacji.
- Nie dodawaj instrukcji ani zachowań spoza danego CPU.
- Każda instrukcja musi doliczać cykle.
- Hot path: unikaj refleksji, dynamic dispatch, zbędnych abstrakcji i alokacji.
- Bez `using static`.
- Bez komentarzy w kodzie poza XML docs, chyba że krótki komentarz wyjaśnia nieoczywistą zgodność sprzętową.
- Używaj `byte` dla 8-bit, `ushort` dla 16-bit, `sbyte` dla signed offset.

## MOS 6502

Punkt odniesienia: https://www.nesdev.org/wiki/6502_instruction_set

Struktura:

```
src/Mos6502/Instructions/
├── InstructionHandler.cs
├── InstructionTable.cs
├── {Mnemonic}/
│   └── {Mnemonic}{AddressingMode}.cs
```

Wzorzec:

```csharp
namespace Mos6502.Instructions.{Mnemonic};

/// <summary>
/// {MNEMONIC} {syntax} — {opis}
/// Opcode: ${hex} | Size: {N} bytes | Cycles: {N}
/// { Flags: ... }
/// </summary>
public static class {Class}
{
    public static void Execute(Cpu cpu)
    {
        cpu.Cycles += {N};
    }
}
```

Testy:

- Jeden test per instrukcja/adresowanie, gdy to praktyczne.
- Testuj wynik, flagi `N/Z/V/C`, cykle i page crossing.
- Referencje: `docs/architecture.md`, `FLOW.md`, Obelisk 6502.

## Z80

Punkty odniesienia:

- `tests/roms/zexdoc.com`
- `tests/roms/zexall.com`
- https://www.z80.info/z80undoc.htm

Zasady:

- Prefiksy `CB`, `ED`, `DD`, `FD`, `DD CB`, `FD CB` muszą konsumować dokładnie te bajty, które konsumuje prawdziwy Z80.
- `DD`/`FD` zastępują `HL/H/L` przez `IX/IY/IXh/IXl/IYh/IYl` tylko tam, gdzie robi to Z80.
- Prefiksowane opcodes nie mogą cicho zamieniać się w zwykły 4-cyklowy NOP, jeśli bazowa instrukcja powinna się wykonać albo ma operandy.
- Testuj osobno: rejestry, flagi `S/Z/H/PV/N/C`, cykle, `PC`, `SP`, odczyt/zapis pamięci i warianty undocumented.
- Diagnostyka ZEXALL/ZEXDOC jest w `docs/zexall-dd-prefix-bug.md`.

## Flow

- 6502: `dotnet test Mos6502.slnx`, przed commitem `bash scripts/progress.sh`.
- Z80: `dotnet test Z80.slnx`; dla DD/FD zaczynaj od filtrów, np. `--filter FullyQualifiedName~DdFdTests`.
- Testy diagnostyczne z celowym `Assert.Fail` traktuj jako narzędzia debugowania, nie jako zieloną regresję.
