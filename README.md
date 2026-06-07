# CPU-VIBE-001

Emulatory CPU MOS 6502 i Z80 w .NET 8 z terminalowym UI oraz emulacją Apple 1.

## Projekty

| Projekt | Opis |
|---------|------|
| `Cpu.Core` | Interfejsy bazowe: `IMemory`, `IDevice`, `IBus`, `Memory`, `RamDevice`, `RomDevice` |
| `Mos6502` | Emulator MOS 6502 / 65C02 / 2A03 |
| `Z80` | Emulator Z80 — zgodność ZEXDOC/ZEXALL |
| `Cpu.Module.Abstractions` | Kontrakt `IAppModule` dla trybów F-key |
| `Cpu.Board` | Generic machine builder z JSON profilu + `BusBackedMemory` |
| `Cpu.Tui.Abstractions` | Interfejsy renderingu (`ITerminalRenderer`, `TerminalCell`, `TermRect`) |
| `Cpu.Tui.Media` | Grafika terminalowa: `PixelBuffer`, `PixelCanvas`, tryby renderowania |
| `Cpu.Tui` | Aplikacja terminalowa — host modułów F-key |

## Architektura modułowa

Każdy tryb to osobny projekt z modułem `ModuleBase` + widokiem `BaseTermView`.
Dokumentacja: [docs/README.md](docs/README.md) · klawisze: [docs/tui/key-map.md](docs/tui/key-map.md)

## Uruchomienie

```bash
dotnet build cpu-vibe.slnx
dotnet run --project src/Cpu.Tui
```

## Klawisze

### Globalne (z otwartego modułu, gdy moduł nie konsumuje klawisza)

| Klawisz | Moduł | Akcja |
|---------|-------|-------|
| `F1` | Help | Pomoc |
| `F4` | Demo Menu | Dema PIA |
| `F7` | Image | Przeglądarka JPG |
| `F8` | Apple 1 | Emulacja Apple 1 |
| `F9` | Canvas | Dema graficzne |
| `Esc` | — | Zamknij moduł / wyjście z menu |

### Screen (moduł otwarty z menu)

| Klawisz | Akcja | Uwagi |
|---------|-------|-------|
| `F5` | Cykl rozmiaru ekranu | ⚠️ w trakcie naprawy |
| `F6` | Echo — pisanie na ekranie | ⚠️ w trakcie naprawy |
| `F10` | Ramki ASCII/Unicode | proponowane (F8 = konflikt z Apple 1) |

Pełna mapa: [docs/tui/key-map.md](docs/tui/key-map.md).

## Apple 1

Uruchamiany klawiszem **F8**. Dwa profile:

| Profil | ROM | Klawisz |
|--------|-----|---------|
| `apple-1.json` | Woz Monitor ($FF00) | `F8` |
| `apple-1-basic.json` | Integer BASIC ($E000) + Woz Monitor ($FF00) | `F8` → `F10` |

CPU 6502 @ ~1 MHz, RAM 4KB, PIA 6520, wyświetlacz 40×24 zielony na czarnym.
Profile maszyn w `src/Cpu.Board/profiles/`.
Szczegółowa dokumentacja: [docs/machines/apple1.md](docs/machines/apple1.md).

## Tryby graficzne (F9 → F10)

| Tryb | Piksele/komórkę | Kolory |
|------|----------------|--------|
| `HalfBlockColor` | 1×2 | 16 |
| `BrailleMono` | 2×4 | 2 |
| `Grayscale` | 1×2 | 24 |
| `BestGlyph` | 2×4 | 2 |
| `BestGlyphTrueColor` | 2×4 | 16.7M |

## Testy

```bash
dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj       # 227 testów
dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj   # 13 testów
dotnet test tests/Mos6502.Tests/Mos6502.Tests.csproj         # 281 testów
dotnet test tests/Z80.Tests/Z80.Tests.csproj                 # 252 testów
```

## Skill dla agenta

Dokumentacja frameworka: [docs/README.md](docs/README.md) — skille `cpu-tui-app` (logika) i `cpu-tui-graphics` (pixele).
