---
name: cpu-tui-graphics
description: "Use when working on Cpu.Tui pixel rendering — TerminalGraphicsRenderer, PixelBuffer, Canvas, BestGlyph, AnsiTerminalRenderer internals, PresentationSession drawing APIs. For module/key logic use cpu-tui-app skill instead."
---

# Cpu.Tui — rendering graficzny

> **Moduły, klawisze, nawigacja:** użyj [cpu-tui-app.md](cpu-tui-app.md), nie tego skillu.

## Dokumentacja

| Temat | Plik |
|-------|------|
| Indeks | [docs/README.md](../../docs/README.md) |
| Tryby graficzne | [docs/tui/graphics.md](../../docs/tui/graphics.md) |
| ANSI renderer (design) | [docs/tui/ansi-renderer.md](../../docs/tui/ansi-renderer.md) |
| Architektura TUI | [docs/tui/overview.md](../../docs/tui/overview.md) |

## Warstwy

```
Cpu.Tui.Abstractions   ITerminalRenderer, TerminalCell, TermRect, FrameStyle
Cpu.Tui.Media          PixelBuffer, TerminalGraphicsRenderer, BestGlyphRenderer
Cpu.Tui                AnsiTerminalRenderer, PresentationSession, BaseTermView
```

## PresentationSession (widoki)

```csharp
session.Clear();
var inner = session.DrawFrame(frame, frameStyle, title).Inner; // zawsze .Inner
session.RenderCanvas(buffer, mode, inner);
session.RenderScreen(screenBuffer, rows, cols, inner);
var (cols, rows) = session.FitImage(img, maxCols, maxRows, mode);
```

Domyślny clear: `TerminalCell.Black`.

## TerminalGraphicsMode

| Mode | Pix/cell |
|------|----------|
| HalfBlockColor | 1×2 |
| BrailleMono | 2×4 |
| Grayscale | 1×1 |
| TrueTone | 1×1 RGB (space, fg=bg) |
| BestGlyph | 2×4 |
| BestGlyphTrueColor | 2×4 RGB |

Cykl: `TerminalGraphicsModes.Next()`. Domyślny tryb: Setup → `DefaultGraphicsMode` → `ApplySettings` w widokach.

Implementacja: `src/Cpu.Tui.Media/TerminalGraphicsRenderer.cs`, helper: `src/Cpu.Tui.Abstractions/TerminalGraphicsModes.cs`

Plan błędów: [graphics-fix-plan.md](../../docs/tui/graphics-fix-plan.md)

## FrameStyle (ramki tekstowe)

`Ascii` (`+-|`) vs `Unicode` (`┌─┐│`). Osobne od graphics mode.

Globalnie w **Setup (F2)** — propagacja przez `ITuiSettingsConsumer.ApplySettings`.

## AnsiTerminalRenderer

Double-buffer, `Flush()` diff, `_lastFg`/`lastBg` tracking.

## Reguły kodu

- Bez `using static`
- Nie duplikuj `FitImage`
- Widoki: `BaseTermView`, clear on Deactivate
- Testy: `dotnet test tests/Cpu.Tui.Tests`

## JPG

`JpegImageLoader` — `ffmpeg` pipe, obrazy w `samples/`.
