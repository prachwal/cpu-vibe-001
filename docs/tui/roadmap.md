# TUI — roadmap uspójnienia

## Ukończone (P1–P7)

| ID | Opis | Commit area |
|----|------|-------------|
| P1 | Echo: F6 + `EchoTerminal.ProcessKey` | ScreenModule |
| P2 | `ScreenMode` → ScreenView via `TerminalLayout.Fit` | ScreenModule, ScreenView |
| P3 | `FrameStyle` propagowany; domyślnie Unicode; toggle w **Setup (F2)** | SetupModule, ITuiSettingsConsumer |
| P4 | F7 nie konsumowany | ScreenModule |
| P5 | DemoPlayer passthrough F-keys | DemoPlayerModule |
| P6 | DemoMenu bubble po child | DemoMenuModule |
| P7 | HelpView zsynchronizowany | HelpView |

## Backlog

| ID | Opis | Priorytet |
|----|------|-----------|
| P8 | Unicode frames w Canvas/Image/Apple1/DemoPlayer | Średni |
| P9 | Jawna kolejność modułów w `AppServices` | Średni |
| P10 | Screen bez echo: treść PIA / demo startowa | Średni |
| P11 | `ModuleBase.OnKey` — child + passthrough (ujednolicić DemoMenu) | Średni |
| P12 | Ujednolicenie trybów graficznych | **Ukończone** — [graphics-fix-plan.md](graphics-fix-plan.md) |
| P13 | Jeden resize (FitImage vs Render) | **Ukończone** |
| P14 | ColorShade vs Grayscale | **Ukończone** (`ColorShade` enum) |
| P15 | Key map F8/F10 cykl trybu | **Ukończone** |
| P16 | `PresentationContext` + `InputRouter` | Niski — backlog |

## Faza architektoniczna (plan)

### InputRouter

Centralny `KeyBindings.cs` ↔ [key-map.md](key-map.md). Fazy: capture (global) → target (moduł) → bubble (parent).

### PresentationContext

```csharp
record PresentationContext(ScreenSize, FrameStyle, TerminalGraphicsMode, EchoMode);
```

Moduł ustawia; widok tylko renderuje.

### IUiTheme

UTF domyślnie, fallback ASCII po detekcji terminala.

## Wzorce referencyjne

Ratatui/Bubble Tea (focus stack), WPF routing (capture/target/bubble), Textual MVP (passive view).

Nie wracać do monolitu `App.Render.cs`.

## Testy do dodania

- `ModuleMenu_OrderIsStable` po P9
- Frame Unicode w Canvas po P8
