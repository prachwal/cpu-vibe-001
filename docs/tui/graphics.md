# Tryby graficzne terminala

## Tryby (`TerminalGraphicsMode`)

| Tryb | Piksele/komórkę | Kolory | Użycie |
|------|-----------------|--------|--------|
| `HalfBlockColor` | 1×2 | 16 (`ConsoleColor`) | Kolor, szybki |
| `BrailleMono` | 2×4 | mono | Wysoka rozdzielczość |
| `Grayscale` | 1×2 | odcienie | Podgląd jasności |
| `BestGlyph` | 2×4 | 2 (quantized) | Dithering glyph |
| `BestGlyphTrueColor` | 2×4 | 24-bit | Najwyższa jakość |

Cycle: **Canvas** F10, **Image** ↑↓/F8.

## API

```csharp
session.RenderCanvas(buffer, mode, frame.Inner);
session.FitImage(img, maxCols, maxRows, mode);
TerminalGraphicsRenderer.Render(renderer, buffer, mode, x, y, w, h);
```

Skalowanie: wyłącznie `PresentationSession.FitImage()` — nie duplikować.

## Half-block

Znak `▀`, fg = górny piksel, bg = dolny. Rozdzielczość pikseli: `cols × rows×2`.

## Braille

Znaki `U+2800..U+28FF`, tile 2×4, próg jasności.

## BestGlyph

Per-cell wybór glyph z atlasu 2×4 (`GlyphAtlas`, `BestGlyphRenderer`). Szczegóły implementacji: `src/Cpu.Tui.Media/BestGlyphRenderer.cs`, testy `BestGlyphRendererTests`.

## Image module

- JPG z `samples/` via `ffmpeg` pipe (`JpegImageLoader`)
- Brak `ffmpeg` → komunikat błędu w widoku
- Skalowanie z zachowaniem proporcji do obszaru roboczego

## Ramki vs tryby graficzne

To osobne pojęcia:

- **FrameStyle** (`Ascii` / `Unicode`) — box drawing wokół panelu (`┌─┐│` vs `+-|`)
- **TerminalGraphicsMode** — jak piksele mapują na znaki wewnątrz ramki

Screen: F10 toggle FrameStyle. Canvas/Image: F10/↑↓ toggle graphics mode.

## Ograniczenia

- `ConsoleColor` = 16 kolorów (poza BestGlyphTrueColor)
- Unicode wymaga fontu/terminala UTF-8
- Braille mono per komórka, nie per dot

## Powiązane

- Renderer ANSI: [ansi-renderer.md](ansi-renderer.md)
- Architektura: [overview.md](overview.md)
