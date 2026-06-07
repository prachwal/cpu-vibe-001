# Checklist widoków TUI

## Lifecycle (BaseTermView)

### Activate

- [ ] `TermArea.Clear` jeśli pełny redraw
- [ ] `Seed()` jednorazowo
- [ ] `Render` do `frame.Inner`

### Deactivate

- [ ] Clear obszaru (`TerminalCell.Black`)
- [ ] Parent: `_fullRedraw = true` przy resize/switch

## ITermView

| Metoda | Obowiązek |
|--------|-----------|
| `Activate` | Seed + Render |
| `Render` | Aktualny stan |
| `Deactivate` | Clear |

## App.Render

- [x] `renderer.Clear` przy full redraw
- [x] `_modules.Render`
- [x] `renderer.Flush`

## Audyt komponentów

| Komponent | Module | FrameStyle | Context z modułu | Keys |
|-----------|--------|------------|------------------|------|
| ScreenView | Screen | ✅ Unicode default | ✅ mode/echo/style | ✅ |
| CanvasView | Canvas | ⚠️ Ascii | ✅ mode API | ✅ |
| ImageView | Image | ⚠️ Ascii | ✅ mode API | ✅ |
| HelpView | Help | — | N/A | ✅ |
| DemoMenuView | Demo Menu | ✅ | N/A | ✅ |
| DemoPlayer | inline | ✅ | N/A | ✅ |
| Apple1View | Apple 1 | ⚠️ Ascii | N/A | ✅ passthrough |

## Backlog

- [ ] `PresentationContext` record moduł → widok
- [ ] `InputRouter` / centralny KeyBindingTable
- [ ] Unicode frames w Canvas/Image/Apple1
- [ ] Stabilna kolejność modułów w menu

Szczegóły: [roadmap.md](roadmap.md)
