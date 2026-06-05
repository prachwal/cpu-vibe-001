# AGENTS.md — Zasady kodowania CPU-VIBE-001

## Nadrzędna reguła

**Binarna kompatybilność 1:1 z oryginalnym MOS 6502.** Kod wygenerowany przez nasz asembler MUSI działać na prawdziwym 6502. Kod z prawdziwego 6502 MUSI działać na naszej implementacji. Punkt odniesienia: https://www.nesdev.org/wiki/6502_instruction_set

## Zakazane

- Nowe instrukcje których nie ma w 6502
- Zmiana zachowania istniejących instrukcji
- Interfejsy, abstrakcje, refleksja, dynamic dispatch w hot path
- Komentarze w kodzie (oprócz XML docs)
- instrukcje bez `cpu.Cycles += N` (każda instrukcja liczy cykle)

## Struktura plików

```
src/Mos6502/Instructions/
├── InstructionHandler.cs     ← delegate void InstructionHandler(Cpu cpu)
├── InstructionTable.cs       ← InstructionHandler[256], O(1) decode
├── {Mnemonic}/
│   └── {Mnemonic}{AddressingMode}.cs   ← jeden plik = jedna instrukcja
```

Nazwa pliku: `{Mnemonic}{AddressingMode}.cs` np. `LdaImmediate.cs`, `AdcZeroPage.cs`.

## Wzorzec instrukcji

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
        // ... logika
        cpu.Cycles += {N};
    }
}
```

## Cykle

Każda instrukcja MUSI mieć `cpu.Cycles += N` na końcu Execute.

| Typ | Cykle |
|-----|-------|
| Implied / Accumulator | 2 |
| Immediate | 2 |
| Zero Page | 3 |
| Zero Page,X/Y | 4 |
| Absolute | 4 |
| Absolute,X/Y | 4 (+1 page crossing) |
| Indirect,X | 6 |
| Indirect,Y | 5 (+1 page crossing) |
| Relative (nie skoczył) | 2 |
| Relative (skoczył) | 3 (+2 page crossing) |

## InstructionTable.cs

- 256 wpisów
- Default: `Nop.Execute` dla undefined opcodes
- Wpisy pogrupowane po instrukcji (ADC, AND, ASL...)
- Pełna lista w `docs/architecture.md` sekcja 6

## Kodowanie

- .NET 8, C# 12
- Namespace: `Mos6502.Instructions.{Mnemonic}`
- Brak `using static` w instrukcjach
- Brak `var` — jawne typy
- `byte` dla 8-bit, `ushort` dla 16-bit, `sbyte` dla signed offset
- Cykle: `byte` (maks 255 na instrukcję)

## Testy

- Jeden test per instrukcja
- Testuj: wartość wyniku, flagi (N,Z,V,C), cykle, page crossing
- Porównuj z referencją: https://www.nesdev.org/obelisk-6502-guide/registers

## Pliki referencyjne

- `docs/architecture.md` — pełna specyfikacja (opcodes, tryby, memory map)
- `FLOW.md` — flow pracy przed commit (cykl implementacji, struktura testów)
- `AGENTS.md` — ten plik

## Flow

Przed każdym commit → `bash scripts/progress.sh`. Pełny opis w `FLOW.md`.
