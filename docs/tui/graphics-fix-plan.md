# Plan naprawczy — grafika TUI

> Ostatnia aktualizacja: po implementacji P1 + P2 (G4–G10). Backlog architektoniczny: P16, P2e.

## Wykryte błędy i niespójności

| ID | Problem | Skutek | Status |
|----|---------|--------|--------|
| G1 | `DefaultGraphicsMode` z Setup nie trafiał do widoków po zapisie | Zmiana w F2 bez efektu w Image/Canvas | **Naprawione** — `ApplySettings` ustawia `Mode` |
| G2 | Cykl trybów zduplikowany w `ImageView` / `CanvasView` | Różna kolejność po dodaniu trybu | **Naprawione** — `TerminalGraphicsModes.Next()` |
| G3 | `FitImage`: `Grayscale` miał `prc=2`, renderer `1×1` | Złe proporcje obrazu w Grayscale | **Naprawione** — `TerminalGraphicsModes.PixelsPerCellRow` |
| G4 | Podwójne skalowanie: `FitImage` + `Resize*` w `Render` | Rozmycie / koszt CPU | **Naprawione** — `ScaleForCells`, `RenderScaled`, `PrepareCanvas` |
| G5 | `RenderColorShade()` niepodpięty do enum | Martwy kod; nazwa `Grayscale` myląca | **Naprawione** — tryb `ColorShade` |
| G6 | Duplikat palety 16 kolorów | Dryf wartości | **Naprawione** — `TerminalPalette16` |
| G7 | `BestGlyphRenderer` tworzony co klatkę | Alokacje w hot path | **Naprawione** — singleton `_bestGlyphRenderer` |
| G8 | Różne skróty trybu: Image ↑↓/F8, Canvas F10 | Brak jednej mapy klawiszy | **Naprawione** — **F8/F10** w Image i Canvas; ↑↓/WASD tylko demo 3D |
| G9 | Dokumentacja: F10 = FrameStyle w Screen | Sprzeczność z Setup | **Naprawione** |
| G10 | `JpegImageLoader` — pełny decode przed `FitImage` | RAM, brak ffmpeg | **Naprawione** — decode do `TargetPixelSize`, `IsFfmpegAvailable`, Setup panel |

## Zrealizowane zmiany API

| Element | Opis |
|---------|------|
| `TerminalGraphicsModes` | Cykl, gęstość pikseli, `TargetPixelSize` |
| `TerminalGraphicsRenderer.ScaleForCells` | Jedno skalowanie przed renderem |
| `TerminalGraphicsRenderer.RenderScaled` | Render bez ponownego `Resize*` |
| `PresentationSession.PrepareCanvas` | `FitImage` + `ScaleForCells` |
| `ColorShade` | Znaki `░▒▓` + paleta 16 kolorów |
| `TrueTone` | Truecolor RGB, spacja `fg=bg` |
| `TerminalPalette16` | Wspólna paleta dla quantizer + half-block |

### Kolejność cyklu trybów

```
HalfBlockColor → BrailleMono → Grayscale → ColorShade → TrueTone → BestGlyph → BestGlyphTrueColor → …
```

### Skróty klawiszy (ujednolicone)

| Moduł | Cykl trybu |
|-------|------------|
| Image (F7) | **F8** / **F10** |
| Canvas (F9) | **F8** / **F10** (↑↓/WASD tylko demo 3D) |
| Setup (F2) | ← → na „Default graphics” |

## Backlog (niski priorytet)

| ID | Opis | Priorytet |
|----|------|-----------|
| P16 | `PresentationContext` — widok tylko renderuje, moduł trzyma stan | Niski |
| P2e | `InputRouter` + `KeyBindings.cs` centralnie | Niski |
| P3a | HelpView — lista trybów + skróty | UX |
| P3c | README root — stare wzmianki F10 | Docs |

## Kryteria „done” G4 — spełnione

- [x] `RenderScaled` nie wywołuje `Resize*` gdy bufor ma wymiary z `TargetPixelSize`.
- [x] Testy `FitImageTests` — wymiary komórek vs pikseli per tryb.
- [x] `PrepareCanvas` — pojedyncza ścieżka skalowania.

## Powiązane pliki

| Obszar | Ścieżka |
|--------|---------|
| Enum + helper | `src/Cpu.Tui.Abstractions/TerminalGraphicsMode.cs`, `TerminalGraphicsModes.cs` |
| Renderer | `src/Cpu.Tui.Media/TerminalGraphicsRenderer.cs`, `TerminalPalette16.cs` |
| JPG | `src/Cpu.Tui.Media/JpegImageLoader.cs` |
| Fit | `src/Cpu.Tui/Rendering/PresentationSession.cs` |
| Widoki | `Cpu.Image/Rendering/Views/ImageView.cs`, `Cpu.Canvas/Rendering/Views/CanvasView.cs` |
| Docs | [graphics.md](graphics.md), [key-map.md](key-map.md), [roadmap.md](roadmap.md) |
