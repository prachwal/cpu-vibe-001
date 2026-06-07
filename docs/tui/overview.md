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
  ReadInput → AnsiInputParser (mouse) / Console.ReadKey (no mouse)
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
| `WantsMouse` | `App` włącza ANSI mouse (1000/1003/1006) |
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

- Sekwencje `\x1b[<…` / `\x1b[M` parsowane w `AnsiInputParser` — **nie** trafiają do `OnKey`.
- Włączone tryby: `1000` (klik), `1003` (ruch — kursor), `1006` (SGR).
- Ruch: biały blok kursora w `App.DrawMouseCursor`; moduły dostają tylko **press** lewego (button 0).
- `Apple1Module`: `WantsMouse => false` — wtedy `Console.ReadKey` bez raportowania myszy.
- Klik: `MainMenuModule` otwiera pozycję menu; `SetupModule` wybiera wiersz / zapis.

## Powiązane

- Klawisze: [key-map.md](key-map.md)
- Checklist widoków: [views-checklist.md](views-checklist.md)
- Tryby graficzne: [graphics.md](graphics.md)
- Backlog: [roadmap.md](roadmap.md)
