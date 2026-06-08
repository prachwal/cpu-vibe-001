# AGENTS.md — CPU-VIBE-001

> **IMPORTANT**: This file MUST be updated after every architecture change (new modules, refactored module structure, changed key dispatch, updated panel layout, modified I/O model). Also update `docs/tui/key-map.md` and `docs/README.md` index when adding docs. For TUI key/module changes use skill `.opencode/skills/cpu-tui-app.md`.
>
> **Przed implementacją nowego układu** — czytaj [docs/implementation-lessons.md](docs/implementation-lessons.md) — lista błędów z VIC-20/PET i jak ich unikać.
>
> **Plan implementacji TED7360 (C16/Plus4):** [docs/ted-implementation-plan.md](docs/ted-implementation-plan.md)

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
- **ZAKAZ dodawania workaroundów** — nigdy nie omijaj poprawnej emulacji sprzętu przez sztuczne wpisywanie danych do pamięci, wymuszanie stanów ekranu, ani inne "szybkie fixy". Każda zmiana musi emulować rzeczywiste zachowanie układu. Workaround wymaga wyraźnego pozwolenia użytkownika i tymczasowego uzasadnienia w AGENTS.md.
- **ZAKAZ logiki biznesowej w warstwie widoku** — translacja kodów klawiszy, konwersja danych, zapis do plików, i wszelka logika domenowa (np. mapowanie matryca→PETSCII) musi być w odpowiedniej warstwie: `Devices/` (adapter sprzętu), `System/` (logika maszyny), `Chips/` (układ). Warstwa widoku (`Rendering/Views/`) może TYLKO wywoływać gotowe adaptery, nigdy implementować translacji samodzielnie.

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
| Plan naprawczy grafiki | [docs/tui/graphics-fix-plan.md](docs/tui/graphics-fix-plan.md) |
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
| `Cpu.Chips` | Core | VIA6522, CRTC6545, VIC6560, Color RAM |
| `Cpu.Tui` | Abstractions + Media + Board + Module | Aplikacja, **ModuleBase**, konfiguracja, skan modułów DLL |
| `Cpu.Setup` | Tui + Module | Konfiguracja aplikacji (F2) |
| `Cpu.Apple1` | Core + Board + Tui + Module | Apple 1 |
| `Cpu.Pet` | Core + Board + Chips + Tui + Module | Commodore PET 2001 |
| `Cpu.Vic20` | Core + Board + Chips + Tui + Module | Commodore VIC-20 |
| `Cpu.C16` | Core + Board + Chips + Tui + Module | Commodore 16 + TED7360 |
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
| `PetModule` | `Cpu.Pet.Modules` | Commodore PET 2001 |
| `Vic20Module` | `Cpu.Vic20.Modules` | Commodore VIC-20 |

**Zasady chain:**
- `MainMenuModule` pokazuje listę modułów, highlight wybranego (`>` + odwrócone kolory)
- Po otwarciu modułu (`Enter` lub F-key), child renderuje treść; dolny wiersz: `"Esc back"`
- `Esc` zamyka direct child w `MainMenuModule`; leaf zwraca `false` dla Esc → parent zamyka
- **Dispatch:** `child.OnKey()` → jeśli `false`, MainMenu: Esc + F1/F2/F4/F6/F7/F8/F9/F10/F11
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
- **Opcjonalny panel referencyjny (prawy)** — `SecondaryPanelWidth` > 0 włącza drugi panel przy prawej krawędzi; nadpisz `RenderSecondaryPanel` i używaj `SecondaryPanelLine`. Layout i próg szerokości (`SecondaryPanelMinWidth`) obsługuje `ModuleBase.OnRender`. Wzorzec: `PetModule` + `PetHostKeyMap`.
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

**ModuleBase.OnRender** dzieli ekran (z opcjonalnym panelem referencyjnym po prawej):
```
┌──────┬──────────────────┬──────┐
│panel │ renderContent    │ ref  │
│30 zn │ (reszta)         │30 zn │
│ Info │                  │ keys │
│ Ctrl │                  │ ...  │
└──────┴──────────────────┴──────┘
```
Panel referencyjny pojawia się gdy `w >= SecondaryPanelMinWidth` i moduł ustawi `SecondaryPanelWidth`.

### Komponenty renderingu

- `ITerminalRenderer` — niskopoziomowy output (SetCell, Flush)
- `ITermView` / `BaseTermView` — lifecycle widoku (Activate→Seed→Render, Deactivate→Clear)
- `PresentationSession` — **jedyny** API dla widoków (Clear, Write, DrawFrame, RenderCanvas, RenderScreen)
- `TermViewManager` — przełączanie widoków z auto-clear na resize

### Stan emulacji VIC-20 (po 7 fazach)

| Faza | Co zrobiono | Status |
|------|-------------|--------|
| 1 | VIC-I: $03/$04 RO, DisplayEnable, LightPen, border | done |
| 2 | Audio pipeline (oscylatory podłączone), floating bus | done |
| 3 | PAL profil + profile config (VicProfile/ViaProfile w JSON) | done |
| 4 | Memory: expansion blocks zamiast blanket RAM | done |
| 5 | VIA #2 ($9120) + joystick stub, IRQ OR | done |
| 6 | VIA timer per-cycle (Update co cykl CPU) | done |
| — | VIC-I nie ma raster IRQ (to feature VIC-II z C64) | N/A |

Tryby graficzne: `TerminalGraphicsModes`, `TrueTone`, propagacja z Setup — patrz [docs/tui/graphics-fix-plan.md](docs/tui/graphics-fix-plan.md). Backlog: [docs/tui/roadmap.md](docs/tui/roadmap.md).

### Zasady

- Każdy widok dziedziczy `BaseTermView` (automatyczny clear na Deactivate)
- `PresentationSession.Clear()` przed renderem → eliminacja artefaktów
- `fullRedraw` = `_renderer.Clear` + flush całego back-buffera
- Renderowanie: zawsze `session.RenderCanvas(buffer, mode, frame.Inner)` / `session.RenderScreen(screen, rows, cols, frame.Inner)` — nigdy `frame` bez `.Inner`
- Domyślny clear: `TerminalCell.Black` (Black/Black) — nigdy Gray/Black
- `PresentationSession.FitImage()` — jedna implementacja skalowania, nie duplikuj; wspólna siatka komórek (`FrameLayoutPixelsPerCell*`: 1×2) dla wszystkich trybów; `ScaleForCells` skaluje źródło do tej samej ramki referencyjnej, tryb wpływa tylko na mapowanie piksel→komórka
- Testy: `dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj`
- Testy board: `dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj`
- Testy PET: `dotnet test tests/Cpu.Pet.Tests/Cpu.Pet.Tests.csproj`
- Testy chipów: `dotnet test tests/Cpu.Chips.Tests/Cpu.Chips.Tests.csproj`
- Build całego rozwiązania: `dotnet build cpu-vibe.slnx` (pomija benchmarki z błędami)
- Wszystkie testy: `    dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj && dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj && dotnet test tests/Cpu.Chips.Tests/Cpu.Chips.Tests.csproj && dotnet test tests/Cpu.Vic20.Tests/Cpu.Vic20.Tests.csproj`

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
- `MachineProfile` — model JSON: CpuProfile (`entryPoint`, `resetVector`), MemoryRegion, PiaProfile, VicProfile, ViaProfile, DisplayProfile

### Testy

```bash
dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj
```

- Klawiatura hosta: `Apple1HostKeyMap` (`Devices/Apple1HostKeyMap.cs`) — mapowanie i prawy panel TUI; referencja w `roms/apple-1/Host_Keyboard_Reference.txt`

Szczegółowa dokumentacja: [docs/machines/apple1.md](docs/machines/apple1.md).

## Commodore PET (Cpu.Pet)

- **F11** — uruchamia emulację PET 2001-32
- `Esc` / **F11** — wyjście z modułu
- CPU 6502 + PIA1 ($E810) + PIA2 IEEE ($E820) + VIA6522 ($E840) + CRTC6545 ($E880)
- Video RAM $8000 (40×25), tekstowy kursor (ZP $C6, $C4/$C5)
- Wyświetlanie: `PetScii.ToDisplayChar()` — mapowanie PETSCII→ASCII terminala (bez surowego cast)
- Klawiatura hosta: `PetHostKeyMap` (`Devices/PetHostKeyMap.cs`) — jedna lista `SpecialKeys` / `PanelRows` dla mapowania i prawego panelu TUI
- Prawy panel (≥110 kolumn): Host → PET → kod hex; lewy panel = CPU/stats
- **IEEE-488 / stacja dysków**: wirtualna magistrala IEEE-488 + stacja dysków #8
  - PIA1 ($E810-$E813) = klawiatura/kaseta/EOI
  - PIA2 ($E820-$E823) = IEEE DIO + NDAC/DAV handshake
  - VIA PB2 = ATN, PB1 = NRFD, PB0/6/7 = NDAC/NRFD/DAV readback
  - Bus state machine: IDLE → Command → DataOut/DataIn
  - Commodore DOS: LOAD, SAVE, INIT, katalog (LOAD"$"), error channel (SA 15)
  - Obsługa D64: BAM, katalog, sektory, alokacja/zapis
  - **F12** — montuj obraz D64 (dialog wyboru pliku)
  - Obsługa testów: `dotnet test tests/Cpu.Pet.Tests --filter "PetIeeeBus|PetIeeeDiskDrive|D64Image|CbmDos|PetMachineIeee"`
  - Szczegóły: [docs/machines/pet/index.md](docs/machines/pet/index.md)
  - Protokół + D64: [docs/machines/pet/ieee-488.md](docs/machines/pet/ieee-488.md)
  - Skill: `.opencode/skills/pet-ieee488.md`

### Profile

Pliki w `src/Cpu.Pet/profiles/pet-*.json`, ROM-y w `src/Cpu.Pet/roms/commodore-pet/`.

### I/O map

| Adres | Urządzenie | Opis |
|-------|-----------|------|
| `$8000-$83FF` | Video RAM | 40×25 znaków |
| `$E810-$E813` | PIA1 6821 | Klawiatura/kaseta/EOI |
| `$E820-$E823` | PIA2 6821 | IEEE-488 DIO read/write + NDAC/DAV |
| `$E840-$E84F` | VIA 6522 | CB1=VSync, PB5=DE, PB2=ATN, PB0/6/7=NDAC/NRFD/DAV |
| `$E880-$E88F` | CRTC 6545 | Kontroler wideo |
| `$C000-$FFFF` | ROM | BASIC + Editor + Kernal |

### Architektura IEEE-488

```
KERNAL → PIA2 $E822 (DIO out) → PetIeeePortBBinding → PetIeeeBus → PetIeeeDiskDrive → CbmDosEngine → D64Image
         PIA2 $E820 (DIO in)  ← PetIeeePortABinding  ←────────────┘
         PIA2 CA2/CB2 (NDAC/DAV) ───────────────────→ PetIeeeBus
         VIA $E840 (PB1/PB2=NRFD/ATN, PB0/6/7 readback) ───────────┘
```

| Sygnał | W PET | Opis |
|--------|-------|------|
| DIO1-8 | PIA2 Port A/B | Port A read, Port B write |
| ATN | VIA PB2 write, PIA2 CA1 read | tryb komenda/dane |
| DAV | VIA PB7 read | Data Valid (z urządzenia) |
| NRFD | VIA PB6 read, VIA PB1 write | Not Ready For Data |
| NDAC | VIA PB0 read, PIA2 CA2 write | Not Data Accepted |

### Pliki IEEE-488

| Plik | Opis |
|------|------|
| `Devices/IPortBinding.cs` | Interfejs bindingów PIA |
| `Devices/PetIeeePortABinding.cs` | Binding PIA2 Port A ← magistrala |
| `Devices/PetIeeePortBBinding.cs` | Binding PIA2 Port B → magistrala |
| `Devices/IIeeeDevice.cs` | Interfejs urządzenia IEEE-488 |
| `Devices/PetIeeeBus.cs` | State machine magistrali |
| `Devices/CbmDos/D64Image.cs` | Parsowanie D64 (BAM, katalog, sektory) |
| `Devices/CbmDos/CbmDosEngine.cs` | Silnik DOS (komendy, pliki, errory) |
| `Devices/CbmDos/PetIeeeDiskDrive.cs` | Adapter IIeeeDevice → CbmDosEngine |
| `Devices/CbmDos/DirEntry.cs` | Struktura wpisu katalogu |
| `Devices/CbmDos/FileType.cs` | Enum typów plików |

### Pliki D64

Obrazy testowe w `src/Cpu.Pet/roms/pet-test-disks/` (kopiowane do outputu przez build):
- `games-1.d64` — 37 gier, w tym HELLO, SPACE INVADERS, BATTLESHIP
- `utils.d64` — narzędzia: Supermon, kalkulator, disk utilities
- `test-hello.d64` — prosty program testowy BASIC

Montowanie: **F12** w module PET.

### Testy

```bash
dotnet test tests/Cpu.Pet.Tests/Cpu.Pet.Tests.csproj
dotnet test tests/Cpu.Chips.Tests/Cpu.Chips.Tests.csproj

# Filtry IEEE-488:
dotnet test tests/Cpu.Pet.Tests --filter "PetIeeeBus"
dotnet test tests/Cpu.Pet.Tests --filter "PetIeeeDiskDrive"
dotnet test tests/Cpu.Pet.Tests --filter "D64Image"
dotnet test tests/Cpu.Pet.Tests --filter "CbmDos"
dotnet test tests/Cpu.Pet.Tests --filter "CbmDosSave"
dotnet test tests/Cpu.Pet.Tests --filter "PetMachineIeee"
```

## Commodore VIC-20 (Cpu.Vic20)

- **F6** — uruchamia emulację VIC-20 (NTSC)
- `Esc` / **F6** — wyjście z modułu
- CPU 6502 + VIC6560 ($9000) + VIA6522 ($9110) + color RAM ($9400)
- Wyświetlanie domyślnie: **tryb tekstowy** — bezpośredni odczyt screen RAM (22×23) w ramce 40×25 jak PET; **F10** przełącza tekst ↔ grafika (HalfBlock → Braille → …)
- Klawiatura: matrix 8×8 przez VIA + `VicHostKeyMap`; prawy panel referencyjny
- **F12** — przełącza tryb **keyboard echo**: overlay na dole ekranu pokazuje czas, akcję (tap/press/release), row/col i etykietę naciśniętego klawisza. Ponowny F12 wyłącza overlay. Włączony overlay nie wpływa na działanie klawiatury.
- **Obsługa klawiatury:** KERNAL ROM ma martwy skaner matrycy (IRQ handler `$FF72` nie skanuje). Klawisze są obsługiwane przez `Vic20Machine.FillKeyboardBuffer(row, col)` → `VicHostKeyMap.TryGetPetscii()` konwertuje pozycję matrycy na PETSCII i zapisuje do bufora KERNAL-a ($0277) + head ($C6). Translacja matryca→PETSCII jest w warstwie `Devices/` (`VicHostKeyMap`), nie w widoku.
- ROM-y: `src/Cpu.Vic20/roms/commodore-vic-20/` (VICE 3.10: basic, kernal, chargen — patrz README)

### Profile

`src/Cpu.Vic20/profiles/vic20-ntsc.json` (NTSC, domyślny)
`src/Cpu.Vic20/profiles/vic20-pal.json` (PAL)

### I/O map

| Adres | Urządzenie | Opis |
|-------|-----------|------|
| `$8000-$8FFF` | Char ROM | vic20-chargen.bin |
| `$9000-$900F` | VIC6560 | 16 rejestrów (mirror co 16 B w stronie) |
| `$9110-$911F` | VIA 6522 | Klawiatura, joystick |
| `$9120-$912F` | VIA 6522 #2 | User port, joystick, cassette |
| `$9400-$97FF` | Color RAM | 4 bity na znak |
| `$C000-$DFFF` | BASIC ROM | basic.bin |
| `$E000-$FFFF` | KERNAL ROM | kernal.bin |

### Testy

```bash
dotnet test tests/Cpu.Vic20.Tests/Cpu.Vic20.Tests.csproj
dotnet test tests/Cpu.Chips.Tests/Cpu.Chips.Tests.csproj
```

- Testy VIA #2, expansion blocks, PAL, raster timing, audio, floating bus — zintegrowane z `Cpu.Chips.Tests` i `Cpu.Vic20.Tests`

## Commodore 16 (Cpu.C16)

- **F10** — uruchamia emulację Commodore 16
- `Esc` — wyjście z modułu; w module **F10** przełącza tekst/grafikę
- CPU 7501/8501 class (MOS 6502 core) + TED7360 ($FF00-$FF3F) + 16 KB RAM mirror
- `C16MemoryDevice` jest mapperem maszyny: RAM 16 KB mirrored, BASIC ROM $8000-$BFFF, KERNAL ROM $C000-$FFFF, TED I/O zawsze $FF00-$FF3F
- Bankowanie jak VICE/Plus4: `$FF3E` wybiera ROM, `$FF3F` RAM, `$FDD0-$FDDF` wybiera bank ROM; `$FC00-$FCFF` pozostaje KERNAL w trybie ROM dla procedur przełączania banków
- Klawiatura: `C16HostKeyMap` → `C16KeyboardMatrix`; PIO2 `$FD30-$FD3F` wybiera wiersze, TED `$FF08` zwraca kolumny; brak klawiszy = `$FF`
- TUI wpisuje znaki przez tap matrycy oraz bufor KERNAL (`$0527`, licznik `$00EF`) jak VICE `kbdbuf`

### Profile

`src/Cpu.C16/profiles/c16.json`

### Testy

```bash
dotnet test tests/Cpu.C16.Tests/Cpu.C16.Tests.csproj
dotnet test tests/Cpu.Chips.Tests/Cpu.Chips.Tests.csproj --filter TED7360
```
