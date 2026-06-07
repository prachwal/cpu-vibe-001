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

## Terminal UI (Cpu.Tui)

Struktura: [docs/project-structure.md](docs/project-structure.md)
Checklista komponentów: [docs/component-checklist.md](docs/component-checklist.md)

Pełna dokumentacja frameworka graficznego i aplikacji terminalowej znajduje się
w skille `.opencode/skills/cpu-tui-graphics.md` (załadowany automatycznie przy
zadaniach z `Cpu.Tui`).

### Projekty

| Projekt | Zależności | Opis |
|---------|-----------|------|
| `Cpu.Module.Abstractions` | Abstractions | Kontrakt `IAppModule` dla trybów F-key |
| `Cpu.Tui.Abstractions` | (none) | Interfejsy + typy bazowe |
| `Cpu.Tui.Media` | Abstractions | Pixel, Buffer, Canvas, Glyph, JPG |
| `Cpu.Board` | Core + Mos6502 | Generic `MachineBoard` z JSON profilu + `BusBackedMemory` |
| `Cpu.Apple1` | Core + Board + Tui + Module | Apple 1 (view, module, adapters, profiles, ROMs) |
| `Cpu.Tui` | Abstractions + Media + Board + Module | Aplikacja terminalowa (moduły ładowane przez assembly scan) |

### Architektura modułowa (F-keys)

Każdy tryb F-key to osobny moduł implementujący `Cpu.Module.IAppModule`:

| Moduł | Plik | Klawisz |
|-------|------|---------|
| `ScreenModule` | `Modules/ScreenModule.cs` | domyślny (F2,F3,F6) |
| `HelpModule` | `Modules/HelpModule.cs` | F1 |
| `DemoMenuModule` | `Modules/DemoMenuModule.cs` | F4 |
| `ImageModule` | `Modules/ImageModule.cs` | F7 |
| `Apple1Module` | `Modules/Apple1Module.cs` | F8 |
| `CanvasModule` | `Modules/CanvasModule.cs` | F9 |

Moduły rejestrowane w DI (`AppServices.cs`) jako `IAppModule`, zarządzane przez `ModuleManager`. Pasek funkcyjny budowany automatycznie z `ActivateLabel` aktywnych modułów.

**Kontrakt (`IAppModule`):**
- `ActivateKey` — klawisz F aktywacji (null = domyślny)
- `ShowInBar` — czy pokazywać w pasku funkcyjnym
- `OnActivate/OnDeactivate` — lifecycle
- `OnKey` — obsługa klawiszy (zwraca true = skonsumowano)
- `OnTick` — wywoływane co klatkę
- `OnRender` — renderowanie

### Komponenty renderingu

- `ITerminalRenderer` — niskopoziomowy output (SetCell, Flush)
- `ITermView` / `BaseTermView` — lifecycle widoku (Activate→Seed→Render, Deactivate→Clear)
- `PresentationSession` — **jedyny** API dla widoków (Clear, Write, DrawFrame, RenderCanvas, RenderScreen)
- `TermViewManager` — przełączanie widoków z auto-clear na resize

### Zasady

- Każdy widok dziedziczy `BaseTermView` (automatyczny clear na Deactivate)
- `PresentationSession.Clear()` przed renderem → eliminacja artefaktów
- `fullRedraw` = `_renderer.Clear` + flush całego back-buffera
- Renderowanie: zawsze `session.RenderCanvas(buffer, mode, frame.Inner)` / `session.RenderScreen(screen, rows, cols, frame.Inner)` — nigdy `frame` bez `.Inner`
- Domyślny clear: `TerminalCell.Black` (Black/Black) — nigdy Gray/Black
- `PresentationSession.FitImage()` — jedna implementacja skalowania, nie duplikuj
- Testy: `dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj`
- Testy board: `dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj`
- Build całego rozwiązania: `dotnet build cpu-vibe.slnx` (pomija benchmarki z błędami)
- Wszystkie testy: `    dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj && dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj`

## Apple 1 (Cpu.Board)

- **F8** — uruchamia emulację Apple 1 (Woz Monitor)
- **F10** — przełącz profil na BASIC (Integer BASIC + Woz Monitor)
- `Esc` / `F8` — wyjście
- CPU 6502 + PIA 6520 ($D010) + RAM 4KB + ROM-y
- Wyświetlacz 40×24 zielony na czarnym
- Klawiatura: typowanie wysyła ASCII do PIA

### Profile

Dwa pliki w `src/Cpu.Board/profiles/`:

| Profil | ROM-y | Autostart |
|--------|-------|-----------|
| `apple-1.json` | Woz Monitor ($FF00) | resetVector ($FFFC) |
| `apple-1-basic.json` | BASIC ($E000) + Woz Monitor ($FF00) | `entryPoint: "0xE000"` |

### I/O map

| Adres | Urządzenie | Opis |
|-------|-----------|------|
| `$D010-$D015` | PIA 6520 | Klawiatura (port A, CA1) + wyświetlacz (port B) |
| `$D0F2` | BasicDspDevice | **Alternatywny port wyjścia BASIC ROM** — patrz niżej |
| `$E000-$EFFF` | BASIC ROM | Integer BASIC (4 KB) |
| `$FF00-$FFFF` | Woz Monitor | 256 B |

### entryPoint

Opcjonalne pole JSON w `cpu`. Jeśli ustawione, `MachineBoard.Reset()` nadpisuje PC po resecie, startując CPU bezpośrednio z podanego adresu zamiast przez wektor `$FFFC`/`$FFFD`.

### BASIC ROM — znany quirk

BASIC (basic.bin) **nie używa `$D012`** (PIA port B) do wyjścia na wyświetlacz. Zamiast tego pisze do **`$D0F2`**:

```asm
E3D5: BIT $D0F2    ; czekaj na gotowość
E3D8: BMI E3D5
E3DA: STA $D0F2    ; wyślij znak
```

Obsługę dodaje `BasicDspDevice` (`src/Cpu.Board/Adapters/BasicDspDevice.cs`), automatycznie podpinany w `Apple1View`.

### Maszyna

- `MachineBoard` — generic builder z JSON profilu
- `BusBackedMemory` — CPU ↔ magistrala z routowaniem I/O
- `MachineProfile` — model JSON: CpuProfile (`entryPoint`, `resetVector`), MemoryRegion, PiaProfile, DisplayProfile

### Testy

```bash
dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj
```

Szczegółowa dokumentacja: [docs/apple1.md](docs/apple1.md).
