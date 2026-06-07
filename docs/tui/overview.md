# TUI — przegląd architektury

Aplikacja terminalowa: `dotnet run --project src/Cpu.Tui`

## Projekty

```
Cpu.Module.Abstractions   IAppModule, MouseEvent
Cpu.Tui.Abstractions      ITerminalRenderer, TerminalCell, TermRect, FrameStyle
Cpu.Tui.Media             PixelBuffer, Canvas, TerminalGraphicsRenderer
Cpu.Tui                   App, ModuleManager, MainMenuModule, ModuleBase
Cpu.Board                 MachineBoard, profile JSON
Cpu.{Screen,Help,DemoMenu,Image,Canvas,Apple1}   moduł + widok per feature
```

## Pętla aplikacji

```
App.Run()
  resize? → AnsiTerminalRenderer.Resize
  dirty?  → ModuleManager.Render(root) + mouse cursor overlay
  ReadInput → bajty stdin (mysz) / Console.ReadKey (Apple 1)
  ModuleManager.OnKey / OnMouse
  ModuleManager.Tick (active chain)
```

Pliki: `src/Cpu.Tui/App.cs`, `Modules/ModuleManager.cs`, `AppServices.cs`

## Chain modułów

```
MainMenuModule (root)
  ├── ScreenModule
  ├── HelpModule
  ├── DemoMenuModule
  │     └── DemoPlayerModule (child)
  ├── ImageModule
  ├── Apple1Module
  └── CanvasModule
```

Rejestracja: skan `*.dll` w `AppServices` → `IAppModule` transient → `MainMenuModule` dostaje kolekcję.

**Kolejność menu nie jest stabilna** (backlog: jawna lista w `AppServices`).

## Kontrakt IAppModule

| Członek | Rola |
|---------|------|
| `Name` | Identyfikator (`SwitchTo` w MainMenu) |
| `Parent` / `Child` / `SetChild` | Hierarchia |
| `OnKey` | `true` = skonsumowano (blokuje parent fallback) |
| `OnRender` | Rysowanie (root woła `_root.OnRender`) |
| `OnTick` | Co klatkę, od active leaf w górę |
| `WantsMouse` | Moduł obsługuje mysz gdy włączona w Setup |
| `OnMouse` | Kliknięcia — ruch aktualizuje tylko kursor w `App` |

## ModuleBase

- Panel lewy 30 znaków gdy `w >= 116` (`80 + PanelWidth + 6`)
- `RenderContent(x, y, w, h)` — obszar treści
- `OnKeyCore` — klawisze lokalne modułu
- Parent z childem (`DemoMenuModule`) **nadpisuje** `OnKey` / `OnRender`

## Widoki (BaseTermView)

Moduł = presenter (stan, klawisze). Widok = passive render.

**Reguła:** stan modułu (`ScreenMode`, `FrameStyle`, `EchoEnabled`, …) ustawiany przed `view.Render`, nie trzymany tylko w polach modułu bez propagacji.

```csharp
_screenView.ScreenMode = _screenMode;
_screenView.FrameStyle = _frameStyle;
_screenView.EchoEnabled = _echoMode;
_screenView.Render(r, area, session);
```

Lifecycle: `Activate` → lazy `Seed` → `Render`; `Deactivate` → clear (`TerminalCell.Black`).

## Rendering

```
PresentationSession
  Clear / Write / Centered
  DrawFrame(style) → użyj .Inner
  RenderScreen / RenderCanvas / FitImage

AnsiTerminalRenderer — double buffer, Flush diff
```

Domyślny clear: `TerminalCell.Black`. Nigdy renderować do outer `frame` bez `.Inner`.

## Mysz

**Problem (root cause):** `Console.ReadKey` nie nadaje się do współbieżności z raportowaniem myszy ANSI. Sekwencja `\x1b[<35;x;yM` jest rozbijana — po krótkim buforze `Esc` reszta (`[`, `<`, cyfry, `M`) wraca do kolejki jako **pojedyncze klawisze** (fałszywe strzałki, litery, nawigacja menu).

**Rozwiązanie:** Mysz domyślnie **wyłączona** (`TuiAppSettings.MouseEnabled`, Setup → Mouse input). Gdy włączona:

- Przy `WantsMouse=true`: **tylko odczyt bajtów** ze stdin + `AnsiInputParser` (nigdy `ReadKey` na tej samej sekcji strumienia).
- Linux/WSL: **raw mode** (`termios`, wyłączone ICANON/ECHO) przez `UnixTerminalRawMode`.
- Bufor persystentny; `IsIncomplete()` czeka na resztę sekwencji; osierocone fragmenty `[`/`<` odrzucane.
- Przy `WantsMouse=false` (Apple 1): `Console.ReadKey` — brak raportowania myszy z terminala.

Tryby terminala: `1000` (klik), `1003` (ruch/kursor), `1006` (SGR). Ruch → `DrawMouseCursor`; moduły dostają tylko LPM press.
- Klik: `MainMenuModule` otwiera pozycję menu; `SetupModule` wybiera wiersz / zapis.

## Powiązane

- Klawisze: [key-map.md](key-map.md)
- Checklist widoków: [views-checklist.md](views-checklist.md)
- Tryby graficzne: [graphics.md](graphics.md)
- Backlog: [roadmap.md](roadmap.md)
