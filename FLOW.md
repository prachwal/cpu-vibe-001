# FLOW.md — Flow pracy przed commit

## Przed każdym commitem

```
1. Uruchom skrypt postępu:
   bash scripts/progress.sh

2. Sprawdź wynik:
   - POSTEP: X%
   - PER MNEMONIC: które instrukcje zaimplementowane
   - TESTY: ile testów

3. Jeśli wszystko OK → commit
```

## Cykl implementacji nowej instrukcji

```
1. Utworzyć plik instrukcji:
   src/Mos6502/Instructions/{Mnemonic}/{Mnemonic}{Mode}.cs

2. Wzorzec (zawsze taki sam):
   namespace Mos6502.Instructions.{Mnemonic};
   public static class {Mnemonic}{Mode}
   {
       public static void Execute(Cpu cpu)
       {
           // logika
           cpu.Cycles += {N};    ← MUSI być na końcu
       }
   }

3. Dodać wpis w InstructionTable.cs:
   Handlers[0x{hex}] = {Mnemonic}{Mode}.Execute;

4. Napisać testy:
   tests/Mos6502.Tests/{Mnemonic}Tests.cs
   - Testuj: wartość, flagi (N,Z,V,C), cykle, page crossing
   - Porównuj z https://www.nesdev.org/obelisk-6502-guide/registers

5. Uruchomić testy:
   dotnet test tests/Mos6502.Tests/

6. Uruchomić progress:
   bash scripts/progress.sh

7. Commit
```

## Flagi — kiedy aktualizować

| Instrukcja | N | Z | V | C |
|------------|---|---|---|---|
| ADC | ✓ | ✓ | ✓ | ✓ |
| SBC | ✓ | ✓ | ✓ | ✓ |
| AND | ✓ | ✓ | | |
| ORA | ✓ | ✓ | | |
| EOR | ✓ | ✓ | | |
| CMP | ✓ | ✓ | | ✓ |
| ASL | ✓ | ✓ | | ✓ |
| LSR | ✓ | ✓ | | ✓ |
| INC | ✓ | ✓ | | |
| DEC | ✓ | ✓ | | |
| LDA/LDX/LDY | ✓ | ✓ | | |
| TAX/TAY/TXA/TYA | ✓ | ✓ | | |
| INX/INY/DEX/DEY | ✓ | ✓ | | |
| STA/STX/STY | | | | |
| JMP/JSR/RTS | | | | |
| Branch | | | | |
| CLC/SEC/CLI/SEI/CLD/SED/CLV | tylko ta flaga | | | |

## Struktura testów

```csharp
[Fact]
public void {Mnemonic}{Mode}_{CoTestuje}()
{
    // Arrange
    _cpu.Regs.PC = 0x0200;
    _cpu.Memory.Write(0x0200, 0x{opcode});
    // ...

    // Act
    _cpu.Step();

    // Assert
    _cpu.Regs.A.Should().Be(expected);
    _cpu.Cycles.Should().Be({N});
}
```

## Cykl dla Cpu.Board / Apple 1

```
1. Zmieniasz profil, PIA, adapter wyświetlacza lub I/O mapę?

2. Uruchom testy Board:
   dotnet test tests/Cpu.Board.Tests/Cpu.Board.Tests.csproj

3. Jeśli zmieniasz Apple1View / Apple1Module / PIA:
   dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj

4. Sprawdź czy łączny wynik OK:
   dotnet test tests/Cpu.Board.Tests/Cpu.Board.Tests.csproj && \
   dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj
```

## Progress script output

```
============================================
  CPU-VIBE-001 — Postęp implementacji
============================================

INSTRUKCJE
  Zaimplementowane:  30 / 151
  Stub (puste):      121 / 151

  POSTEP:            19%

PER MNEMONIC
  MNEMONIC  IMPL   ALL  DONE    PCT
  -------- ----- ----- ----- ------
  ADC           0     8     8   100%
  AND           8     8     0     0%
  ...
```
