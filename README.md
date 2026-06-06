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

Każdy tryb F-key to osobny moduł implementujący `IAppModule`, rejestrowany w DI
i zarządzany przez `ModuleManager`. Pasek funkcyjny budowany automatycznie.

## Uruchomienie

```bash
dotnet build cpu-vibe.slnx
dotnet run --project src/Cpu.Tui
```

## Klawisze

| Klawisz | Moduł | Tryb |
|---------|-------|------|
| `F1` | `HelpModule` | Podpowiedź klawiszy |
| `F2` | `ScreenModule` | Zmiana trybu ekranu (40/80 kolumn) |
| `F3` | `ScreenModule` | Echo mode (pisanie na ekranie) |
| `F4` | `DemoMenuModule` | Menu demonstracji PIA |
| `F5` | `ScreenModule` | Odświeżenie |
| `F6` | `ScreenModule` | Przełącz ramki ASCII/Unicode |
| `F7` | `ImageModule` | Przeglądarka JPG |
| **`F8`** | **`Apple1Module`** | **Emulacja Apple 1 (Woz Monitor)** |
| `F9` | `CanvasModule` | Canvas — dema graficzne |
| `F10` | `Apple1Module` | Przełącz profil (Woz Monitor ↔ BASIC) |
| `Esc` | — | Wyjście / powrót do domyślnego modułu |

## Apple 1

Uruchamiany klawiszem **F8**. Dwa profile:

| Profil | ROM | Klawisz |
|--------|-----|---------|
| `apple-1.json` | Woz Monitor ($FF00) | `F8` |
| `apple-1-basic.json` | Integer BASIC ($E000) + Woz Monitor ($FF00) | `F8` → `F10` |

CPU 6502 @ ~1 MHz, RAM 4KB, PIA 6520, wyświetlacz 40×24 zielony na czarnym.
Profile maszyn w `src/Cpu.Board/profiles/`.

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
dotnet test tests/Cpu.Board.Tests/Cpu.Board.Tests.csproj     # 9 testów
dotnet test tests/Mos6502.Tests/Mos6502.Tests.csproj         # 281 testów
dotnet test tests/Z80.Tests/Z80.Tests.csproj                 # 252 testów
```

## Skill dla agenta

Dokumentacja frameworka graficznego: `.opencode/skills/cpu-tui-graphics.md` — automatycznie ładowana przy zadaniach z Cpu.Tui.
