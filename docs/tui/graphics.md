# Tryby graficzne terminala

## Tryby (`TerminalGraphicsMode`)

| Tryb | Piksele/komórkę | Kolory | Glyph | Użycie |
|------|-----------------|--------|-------|--------|
| `HalfBlockColor` | 1×2 | 16 (`ConsoleColor`) | `▀` | Kolor, szybki |
| `BrailleMono` | 2×4 | mono | Braille | Wysoka rozdzielczość |
| `Grayscale` | 1×1 | truecolor luma | `█` | Podgląd jasności |
| `ColorShade` | 1×1 | 16 (`ConsoleColor`) | `░▒▓█` | Kolor + cieniowanie glyph |
| `TrueTone` | 1×1 | 24-bit RGB | spacja (`fg=bg`) | Pełny kolor bez kształtu glyph |
| `BestGlyph` | 2×4 | 2 (quantized) | atlas 2×4 | Dithering glyph |
| `BestGlyphTrueColor` | 2×4 | 24-bit | atlas 2×4 | Najwyższa jakość |

Kolejność cyklu (wspólna): `TerminalGraphicsModes.Next()` — patrz [graphics-fix-plan.md](graphics-fix-plan.md).

### Skróty klawiszy

| Moduł | Cykl trybu |
|-------|------------|
| **Image** (F7) | **F8** / **F10** |
| **Canvas** (F9) | **F8** / **F10** (↑↓/WASD tylko demo 3D) |
| **Setup** (F2) | ← → na wierszu „Default graphics mode” |

Domyślny tryb startowy: **Setup → Default graphics mode** → `ImageView` / `CanvasView` przy aktywacji i po zapisie ustawień.

## API

```csharp
session.RenderCanvas(buffer, mode, frame.Inner);
var (cols, rows) = PresentationSession.FitImage(img, maxCols, maxRows, mode);
TerminalGraphicsRenderer.Render(renderer, buffer, mode, x, y, w, h);
TerminalGraphicsModes.Next(mode);
TerminalGraphicsModes.PixelsPerCellColumn(mode);
TerminalGraphicsModes.PixelsPerCellRow(mode);
```

```csharp
var (scaled, cols, rows) = PresentationSession.PrepareCanvas(source, maxCols, maxRows, mode);
session.RenderCanvas(scaled, mode, frame.Inner);
TerminalGraphicsRenderer.ScaleForCells(buffer, cols, rows, mode);
TerminalGraphicsRenderer.RenderScaled(renderer, scaled, mode, x, y, cols, rows);
```

Skalowanie: **`FitImage`** (layout komórek) + **`ScaleForCells`** (jeden resize pikseli) — patrz [graphics-fix-plan.md](graphics-fix-plan.md).

## Half-block

Znak `▀`, fg = górny piksel, bg = dolny. Rozdzielczość pikseli: `cols × rows×2`.

## Braille

Znaki `U+2800..U+28FF`, tile 2×4, próg jasności.

## Grayscale, ColorShade, TrueTone

- **Grayscale** — blok `█`, kolor z jasności (truecolor gray).
- **ColorShade** — znaki `░▒▓█`, kolor z palety 16 (`ConsoleColor`).
- **TrueTone** — jeden piksel na komórkę, RGB ze źródła; komórka to spacja z `fg=bg`.

## BestGlyph

Per-cell wybór glyph z atlasu 2×4 (`GlyphAtlas`, `BestGlyphRenderer`). Szczegóły: `src/Cpu.Tui.Media/BestGlyphRenderer.cs`.

## Image module

- JPG z `samples/` via `ffmpeg` (`JpegImageLoader.Load(path, targetW, targetH)`)
- Brak `ffmpeg` → komunikat w widoku + status w Setup (F2)
- Skalowanie z zachowaniem proporcji do obszaru roboczego

## Ramki vs tryby graficzne

Osobne pojęcia:

- **FrameStyle** (`Ascii` / `Unicode`) — box drawing wokół panelu (`┌─┐│` vs `+-|`). Ustawiane globalnie w **Setup (F2)**, propagowane przez `ITuiSettingsConsumer`.
- **TerminalGraphicsMode** — mapowanie pikseli wewnątrz ramki.

Screen **nie** przełącza już ramek klawiszem lokalnym — tylko Setup.

## Ograniczenia

- `ConsoleColor` = 16 kolorów (HalfBlock, Braille, część quantize)
- Unicode wymaga terminala UTF-8
- Braille mono per komórka, nie per dot
- `RenderColorShade` — tryb enum **`ColorShade`**

## Powiązane

- Plan naprawczy: [graphics-fix-plan.md](graphics-fix-plan.md)
- Renderer ANSI: [ansi-renderer.md](ansi-renderer.md)
- Architektura: [overview.md](overview.md)
- Mapa klawiszy: [key-map.md](key-map.md)
