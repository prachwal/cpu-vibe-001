# Mapa klawiszy TUI

> **Źródło prawdy.** Po zmianie `OnKey` zaktualizuj ten plik, `HelpView` i `ModuleKeyTests`.

## Dispatch

```
App.ReadInput()
  └─ ModuleManager.OnKey()
       └─ MainMenuModule
            ├─ child.OnKey → true? return
            ├─ Esc → CloseChild
            └─ F1/F2/F4/F7/F8/F9 → SwitchTo(name)
```

Moduły pośrednie (`DemoMenuModule`): child first → Esc zamyka child → `false` bubble do MainMenu.

## Mysz

| Zdarzenie | App | Moduły |
|-----------|-----|--------|
| Ruch | Kursor (biały blok), brak `OnMouse` | — |
| Klik LPM | `ModuleManager.OnMouse` | MainMenu / Setup (gdy Mouse **On** w Setup) |
| Domyślnie | Mysz **wyłączona** — tylko klawiatura (`Console.ReadKey`) | — |
| Apple 1 | Moduł `WantsMouse=false` | — |

Sekwencje ANSI myszy **nie mogą** przechodzić przez `Console.ReadKey` (reszta sekwencji wycieka jako znaki). Przy włączonej myszy: odczyt bajtów + raw mode (Linux).

## Globalne (MainMenu fallback)

| Klawisz | Moduł |
|---------|-------|
| F1 | Help |
| F2 | Setup |
| F4 | Demo Menu |
| F7 | Image |
| F8 | Apple 1 |
| F9 | Canvas |
| Esc | Zamknij child / quit (root bez child) |
| F12 | Panel błędów (tylko menu) |

## Esc

| Kontekst | Zachowanie |
|----------|------------|
| MainMenu, brak child | Quit |
| MainMenu, child | Close child |
| DemoMenu + player | Player konsumuje Esc podczas play; ended → parent zamyka player |
| Leaf (Help, Screen, …) | `false` → parent zamyka |

## Per moduł

### MainMenu

↑↓ wybór, Enter otwórz, klik LPM otwórz, F-keys jak wyżej.

### Setup

| Klawisz | Akcja |
|---------|-------|
| ↑↓ | Wybór wiersza (theme, frame, panel, graphics) |
| ←→ | Zmiana wartości |
| S / Enter (Save) | Zapis `tui-settings.json`, reload, natychmiastowe zastosowanie |
| Esc | Powrót |

Ustawienia globalne (motyw, `FrameStyle` ASCII/Unicode, `PanelSide` lewo/prawo, domyślny tryb graficzny) — wstrzykiwane przez `ITuiAppConfiguration` do modułów i widoków (`ITuiSettingsConsumer`).

### Screen

| Klawisz | Akcja |
|---------|-------|
| F5 | Cykl rozmiaru: 25×80 → 24×40 → 25×40 |
| F6 | Toggle echo (`EchoTerminal.ProcessKey`) |
| F1,F2,F4,F7,F8,F9,Esc | Passthrough (także w echo) |
| Printable | Echo gdy F6 ON |

Ramka ASCII/Unicode — globalnie w Setup (nie F10 w Screen).

### Help

Brak lokalnych klawiszy.

### Demo Menu

↑↓ wybór, Enter → player, Esc → lista / menu.

### DemoPlayer

| Klawisz | Playing | Ended |
|---------|---------|-------|
| P, +, - | pause, speed | — |
| F1,F4,F7,F8,F9 | passthrough | passthrough |
| Esc | consumed (no exit) | passthrough → zamknięcie |
| Inne | consumed | consumed |

### Image

← prev, →/F7 next, ↑↓/F8 cycle graphics mode (kolejność: `TerminalGraphicsModes.Next`).

### Canvas

←→ demo, F8/F10 cycle graphics mode, WASD/↑↓ (demo 3D — zoom).

### Apple 1

F-keys + Esc passthrough. ↑↓/F10 profil. Printable → CPU.

## Antywzorce (nie powtarzać)

1. **`return true` bez akcji** — blokuje globalne F-keys (historyczny bug F7 w Screen).
2. **Echo połyka F-keys** — w echo tylko printable/navigation; F1–F9/Esc → `false`.
3. **Stan modułu bez propagacji do widoku** — F5/F10 zmienia flagę, widok hardcode ASCII.
4. **DemoPlayer `return true` na końcu** dla wszystkich klawiszy — tylko nieobsłużone non-navigation.
5. **Esc przed child.OnKey w parent** — gracz demo zamykał się przed ignore-during-play.

## Testy regresji

```bash
dotnet test tests/Cpu.Tui.Tests --filter "ModuleKeyTests|ScreenModuleTests|DemoMenuModuleTests|DemoPlayerModuleTests"
```

## Wzorzec passthrough (Apple1, DemoPlayer F-keys)

```csharp
case ConsoleKey.F1:
case ConsoleKey.F4:
case ConsoleKey.F7:
case ConsoleKey.F8:
case ConsoleKey.F9:
    return false;
```
