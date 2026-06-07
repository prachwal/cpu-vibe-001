# AGENTS.md — CPU-VIBE-001

> **IMPORTANT**: This file MUST be updated after every architecture change (new modules, refactored module structure, changed key dispatch, updated panel layout, modified I/O model). Also update `docs/tui/key-map.md` and `docs/README.md` index when adding docs. For TUI key/module changes use skill `.opencode/skills/cpu-tui-app.md`.

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
- Referencje: [docs/cpu/6502-architecture.md](docs/cpu/6502-architecture.md), `FLOW.md`, Obelisk 6502.

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
- Diagnostyka ZEXALL/ZEXDOC jest w [docs/z80/zexall-dd-prefix-bug.md](docs/z80/zexall-dd-prefix-bug.md).

## Flow

- 6502: `dotnet test Mos6502.slnx`, przed commitem `bash scripts/progress.sh`.
- Z80: `dotnet test Z80.slnx`; dla DD/FD zaczynaj od filtrów, np. `--filter FullyQualifiedName~DdFdTests`.
- Testy diagnostyczne z celowym `Assert.Fail` traktuj jako narzędzia debugowania, nie jako zieloną regresję.

## Terminal UI (Cpu.Tui)

Indeks dokumentacji: [docs/README.md](docs/README.md)

| Temat | Plik |
|-------|------|
| Architektura modułów | [docs/tui/overview.md](docs/tui/overview.md) |
| Mapa klawiszy | [docs/tui/key-map.md](docs/tui/key-map.md) |
| Checklist widoków | [docs/tui/views-checklist.md](docs/tui/views-checklist.md) |
| Tryby graficzne | [docs/tui/graphics.md](docs/tui/graphics.md) |
| Roadmap | [docs/tui/roadmap.md](docs/tui/roadmap.md) |

**Skille (czytaj przed edycją):**

- Logika aplikacji, klawisze, moduły: `.opencode/skills/cpu-tui-app.md`
- Rendering pikseli, ANSI: `.opencode/skills/cpu-tui-graphics.md`

> Przed zmianą routingu klawiszy lub hierarchii modułów — skill **cpu-tui-app** + `docs/tui/key-map.md`.

### Projekty

| Projekt | Zależności | Opis |
|---------|-----------|------|
| `Cpu.Module.Abstractions` | Abstractions | Kontrakt `IAppModule` dla trybów F-key, `MouseEvent` |
| `Cpu.Tui.Abstractions` | (none) | `ITuiAppConfiguration`, `TuiAppSettings`, motyw, panel |
| `Cpu.Tui.Media` | Abstractions | Pixel, Buffer, Canvas, Glyph, JPG |
| `Cpu.Board` | Core + Mos6502 | Generic `MachineBoard` z JSON profilu + `BusBackedMemory` |
| `Cpu.Tui` | Abstractions + Media + Board + Module | Aplikacja, **ModuleBase**, konfiguracja, skan modułów DLL |
| `Cpu.Setup` | Tui + Module | Konfiguracja aplikacji (F2) |
| `Cpu.Apple1` | Core + Board + Tui + Module | Apple 1 |
| `Cpu.Screen` | Tui + Module | Tryby ekranu |
| `Cpu.Help` | Tui + Module | Pomoc |
| `Cpu.Image` | Tui + Media + Module | Przeglądarka JPG |
| `Cpu.Canvas` | Tui + Media + Module | Dema graficzne |
| `Cpu.DemoMenu` | Tui + Module | Dema PIA |

### Architektura hierarchiczna (chain)

Moduły tworzą drzewo: `MainMenuModule` (root) → moduły podrzędne.

| Moduł | Projekt | Opis |
|-------|---------|------|
| `MainMenuModule` | `Cpu.Tui.Modules` | Root — menu główne (highlight, nawigacja) |
| `SetupModule` | `Cpu.Setup.Modules` | Motyw, ramki, strona panelu, zapis JSON |
| `ScreenModule` | `Cpu.Screen.Modules` | Tryby ekranu, echo |
| `HelpModule` | `Cpu.Help.Modules` | Pomoc |
| `DemoMenuModule` | `Cpu.DemoMenu.Modules` | Dema PIA |
| `ImageModule` | `Cpu.Image.Modules` | Przeglądarka JPG |
| `CanvasModule` | `Cpu.Canvas.Modules` | Dema graficzne |
| `Apple1Module` | `Cpu.Apple1.Modules` | Emulacja Apple 1 |

**Zasady chain:**
- `MainMenuModule` pokazuje listę modułów, highlight wybranego (`>` + odwrócone kolory)
- Po otwarciu modułu (`Enter` lub F-key), child renderuje treść; dolny wiersz: `"Esc back"`
- `Esc` zamyka direct child w `MainMenuModule`; leaf zwraca `false` dla Esc → parent zamyka
- **Dispatch:** `child.OnKey()` → jeśli `false`, MainMenu: Esc + F1/F2/F4/F7/F8/F9
- **DemoMenuModule:** child first, potem Esc zamyka player, potem `false` bubble (F-keys)
- Leaf **nie może** zwracać `true` bez akcji; echo nie połyka F-keys (ScreenModule)
- Wzorzec passthrough: `Apple1Module`, F-keys w `DemoPlayerModule`
- Moduły: skan DLL w `AppServices` — kolejność przez `ModuleOrderByType` (Setup pierwszy)
- **Konfiguracja:** `ITuiAppConfiguration` wstrzykiwana do `ModuleBase`; widoki przez `ITuiSettingsConsumer.ApplySettings`. Plik: `tui-settings.json` obok exe. Setup **S** → `Apply()` → zapis + reload + `Changed`. Panel lewo/prawo z `Config.Current.PanelSide`.
- **Mysz:** domyślnie wyłączona (`MouseEnabled` w Setup). Gdy włączona: bajty stdin + raw mode; inaczej `Console.ReadKey`.

**Kontrakt (`IAppModule`):**
- `Parent` / `Child` / `SetChild` — hierarchia
- `OnActivate/OnDeactivate` — lifecycle
- `OnKey` — obsługa klawiszy (zwraca true = skonsumowano)
- `OnMouse` — obsługa myszy
- `OnTick` — wywoływane co klatkę
- `OnRender` — renderowanie

### ModuleBase (klasa bazowa modułów)

Każdy moduł dziedziczy `ModuleBase` (`Cpu.Tui.Modules`), który zapewnia:

- **Standardowe lifecycle** — `OnActivate`/`OnDeactivate` → metody `OnActivateCore`/`OnDeactivateCore`
- **Standardowy panel funkcyjny** — 30 znaków; strona z `Config.Current.PanelSide` (lewo domyślnie). `RenderContent` dostaje przycięte współrzędne.
- **Moduły z childem (parent modules)** powinny nadpisywać `OnRender` i przekazywać pełne wymiary do childa — child sam zarządza swoim panelem
- **ErrorCollector** — wbudowany `Errors?.Add()` do centralnego logowania błędów
- **Key dispatch** — przez `OnKeyCore()`

Struktura modułu:
```csharp
public sealed class MyModule : ModuleBase
{
    public override string Name => "MyModule";
    public MyModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }
    protected override bool OnKeyCore(ConsoleKeyInfo key) { ... }
    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h) { ... }
    protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y) { ... }
    protected override void RenderPanelControls(ITerminalRenderer r, int w, ref int y) { ... }
}
```

**ModuleBase.OnRender** dzieli ekran:
```
┌──────┬────────────────────────┐
│panel │ renderContent(x=32,    │
│30 zn │ y=0, w=w-32, h=h-1)    │
│      │                        │
│ Info │                        │
│ ...  │                        │
│      │                        │
│Ctrl. │                        │
│ ...  │                        │
└──────┴────────────────────────┘
```

### Komponenty renderingu

- `ITerminalRenderer` — niskopoziomowy output (SetCell, Flush)
- `ITermView` / `BaseTermView` — lifecycle widoku (Activate→Seed→Render, Deactivate→Clear)
- `PresentationSession` — **jedyny** API dla widoków (Clear, Write, DrawFrame, RenderCanvas, RenderScreen)
- `TermViewManager` — przełączanie widoków z auto-clear na resize

### Stan po P1–P7 (skrót)

Echo, ScreenMode, FrameStyle (F10), F7 passthrough, DemoPlayer/DemoMenu navigation — naprawione. Backlog: [docs/tui/roadmap.md](docs/tui/roadmap.md).

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

Szczegółowa dokumentacja: [docs/machines/apple1.md](docs/machines/apple1.md).
