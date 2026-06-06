# BestGlyph renderer design

## Cel

`BestGlyph` ma renderować obraz w terminalu przez automatyczny wybór najlepszego znaku dla lokalnego fragmentu pikseli. Renderer nie wybiera z góry trybu `Braille`, `HalfBlock` albo `Shade`; dla każdej komórki terminala wybiera glyph, foreground i background minimalizujące błąd względem oryginalnego obrazu.

## Problem

Obecne tryby są ręcznie dobrane:

- `HalfBlockColor` — dobre kolory, niska rozdzielczość pionowa.
- `BrailleMono` — dobra rozdzielczość, brak koloru per punkt.
- `Grayscale` — dobra jasność, brak koloru.
- `ColorShade` — kolor + jasność, ale słaby kształt lokalny.

`BestGlyph` ma łączyć te zalety przez lokalne dopasowanie.

## Model komórki

Jedna komórka terminala reprezentuje tile pikseli, np.:

```text
2x4 pixels -> 1 terminal cell
```

To pasuje do braille i dobrze kompensuje proporcje znaków terminala.

Każdy kandydat glyph ma maskę jasności w tym samym rozmiarze:

```csharp
public readonly record struct GlyphPattern(
    char Glyph,
    byte[] Alpha); // length = TileWidth * TileHeight, 0..255
```

`Alpha=0` oznacza background, `Alpha=255` foreground, wartości pośrednie oznaczają blend.

## Atlas glyphów

Minimalny atlas:

- ` ` — pusto;
- `█` — pełne;
- `░`, `▒`, `▓` — półtony;
- `▀`, `▄` — half-block;
- braille `U+2800..U+28FF`.

Docelowy atlas:

- block elements: `▀▄█▌▐`;
- shade elements: `░▒▓`;
- braille;
- opcjonalnie geometric/box glyphs, jeśli terminal dobrze obsługuje UTF.

## Dobór kolorów

Dla danego tile i glyphu:

1. Piksele z wysokim `Alpha` traktujemy jako foreground.
2. Piksele z niskim `Alpha` traktujemy jako background.
3. Liczymy średnie RGB:

```csharp
fg = weighted average(pixel, weight = alpha)
bg = weighted average(pixel, weight = 255 - alpha)
```

4. Mapujemy `fg/bg` do `ConsoleColor` albo w przyszłości do ANSI truecolor.

Wersja 16-color:

```csharp
ConsoleColor fg = Palette.NearestConsoleColor(fgRgb);
ConsoleColor bg = Palette.NearestConsoleColor(bgRgb);
```

Wersja truecolor:

```text
ESC[38;2;R;G;Bm
ESC[48;2;R;G;Bm
```

## Funkcja błędu

Dla każdego piksela tile:

```csharp
expected = Lerp(bg, fg, alpha / 255.0)
error += DistanceSquared(original, expected)
```

Podstawowy score:

```csharp
score = rgbError
```

Lepszy score:

```csharp
score = rgbError
      + lumaWeight * lumaError
      + edgeWeight * edgeError
      + colorChangePenalty
```

`colorChangePenalty` może ograniczać migotanie i nadmierne zmiany kolorów między sąsiednimi komórkami.

## Algorytm

```csharp
for each terminal cell:
    tile = sample image fragment

    bestScore = infinity
    bestGlyph = ' '
    bestFg = Gray
    bestBg = Black

    for each glyph in atlas:
        fg = EstimateForeground(tile, glyph.Alpha)
        bg = EstimateBackground(tile, glyph.Alpha)

        fg = Quantize(fg)
        bg = Quantize(bg)

        score = ComputeError(tile, glyph.Alpha, fg, bg)

        if score < bestScore:
            save candidate

    renderer.SetCell(x, y, bestGlyph, bestFg, bestBg)
```

## API

```csharp
public enum TerminalGraphicsMode
{
    HalfBlockColor,
    BrailleMono,
    Grayscale,
    ColorShade,
    BestGlyph
}
```

```csharp
public sealed class BestGlyphRenderer
{
    public BestGlyphRenderer(GlyphAtlas atlas, ColorQuantizer quantizer);

    public void Render(
        ITerminalRenderer renderer,
        PixelBuffer image,
        int terminalX,
        int terminalY,
        int cols,
        int rows);
}
```

## Pliki docelowe

```text
src/Cpu.Tui/Graphics/
├── BestGlyphRenderer.cs
├── GlyphAtlas.cs
├── GlyphPattern.cs
├── ColorQuantizer.cs
└── PixelMath.cs
```

Testy:

```text
tests/Cpu.Tui.Tests/
├── BestGlyphRendererTests.cs
├── GlyphAtlasTests.cs
└── ColorQuantizerTests.cs
```

## Etapy implementacji

### Etap 1: atlas 2x4

- Dodać `GlyphPattern`.
- Dodać `GlyphAtlas.CreateDefault2x4()`.
- Wygenerować:
  - `space`;
  - `full block`;
  - shades;
  - half-block;
  - braille.

### Etap 2: quantizer 16-color

- Dodać `ColorQuantizer`.
- Użyć istniejącej palety `ConsoleColor`.
- Testować najbliższe kolory dla red/green/blue/black/white.

### Etap 3: renderer

- Dodać `BestGlyphRenderer`.
- Renderować obraz przeskalowany do `cols*2 x rows*4`.
- Dla każdej komórki liczyć najlepszy glyph.
- Podpiąć do `TerminalGraphicsMode.BestGlyph`.

### Etap 4: testy jakościowe

Minimalne testy:

- pusty tile wybiera `space`;
- pełny biały tile wybiera `█`;
- górna połowa biała wybiera `▀`;
- pojedyncze punkty wybierają odpowiedni braille;
- czerwony tile wybiera czerwony foreground;
- mieszany tile ustawia różne `fg/bg`.

### Etap 5: optymalizacja

- Precompute dla glyphów:
  - `alpha`;
  - `foreground weights`;
  - `background weights`.
- Unikać alokacji per tile.
- Opcjonalnie cache dla podobnych tile hash.
- Opcjonalnie równoległe liczenie dla dużych obrazów.

## Truecolor jako następny krok

`ConsoleColor` ogranicza jakość do 16 kolorów. `BestGlyph` będzie działał, ale pełny potencjał ma z truecolor:

```csharp
renderer.SetCellTrueColor(x, y, glyph, fgRgb, bgRgb);
```

Wymaga rozszerzenia `TerminalCell`:

```csharp
public readonly record struct TerminalColor(byte R, byte G, byte B);
```

I ANSI:

```text
ESC[38;2;R;G;Bm
ESC[48;2;R;G;Bm
```

## Ryzyka

- `BestGlyph` jest droższy CPU niż proste tryby.
- Jakość zależy od fontu terminala.
- Braille/Unicode może być źle renderowany w części terminali.
- `ConsoleColor` może powodować banding kolorów.

## Domyślne ustawienie

Nie robić `BestGlyph` domyślnym trybem na start.

Proponowana kolejność `F8`:

```text
HalfBlockColor -> ColorShade -> BrailleMono -> Grayscale -> BestGlyph
```

`BestGlyph` jako tryb eksperymentalny po prostszych trybach.
