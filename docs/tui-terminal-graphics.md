# Terminal graphics in Cpu.Tui

## Dostępne tryby

### Half-block color

Najlepszy tryb dla kolorowej grafiki. Jedna komórka terminala reprezentuje 2 piksele pionowo:

- znak: `▀`;
- foreground = górny piksel;
- background = dolny piksel;
- rozdzielczość terminalowa: `cols x rows`;
- rozdzielczość pikselowa: `cols x rows*2`.

```csharp
PixelBuffer pixels = new(80, 50);
pixels.SetPixel(0, 0, new Pixel(255, 0, 0));
pixels.SetPixel(0, 1, new Pixel(0, 0, 255));

TerminalGraphicsRenderer.RenderHalfBlock(renderer, pixels, 0, 0, 80, 50);
renderer.Flush();
```

### Braille mono

Najlepszy tryb dla wysokiej rozdzielczości czarno-białej. Jedna komórka terminala reprezentuje siatkę `2x4` piksele:

- zakres znaków: `U+2800..U+28FF`;
- rozdzielczość terminalowa: `cols x rows`;
- rozdzielczość pikselowa: `cols*2 x rows*4`;
- działa przez próg jasności.

```csharp
PixelBuffer pixels = new(160, 100);
pixels.SetPixel(0, 0, Pixel.White);

TerminalGraphicsRenderer.RenderBrailleMono(
    renderer,
    pixels,
    terminalX: 0,
    terminalY: 0,
    width: 160,
    height: 100,
    threshold: 128,
    fg: ConsoleColor.Gray,
    bg: ConsoleColor.Black);

renderer.Flush();
```

### Grayscale shades

Najprostszy tryb do podglądu jasności:

- znaki: ` `, `░`, `▒`, `▓`, `█`;
- jedna komórka = jeden piksel logiczny;
- kolor opcjonalny, domyślnie szary na czarnym.

```csharp
PixelBuffer pixels = new(80, 25);
pixels.SetPixel(0, 0, Pixel.Black);
pixels.SetPixel(1, 0, Pixel.White);

TerminalGraphicsRenderer.RenderGrayscale(renderer, pixels, 0, 0, 80, 25);
renderer.Flush();
```

## Dobór trybu

- Kolorowe obrazy, gry, proste demo video: `RenderHalfBlock`.
- Monochromatyczne wysokie DPI, wykresy, kontury, fonty: `RenderBrailleMono`.
- Debug jasności, proste preview, heatmapy: `RenderGrayscale`.

## Ograniczenia

- Kolor jest mapowany do 16 kolorów `ConsoleColor`.
- Braille jest mono; kolor dotyczy całej komórki, nie pojedynczej kropki.
- Half-block ma tylko dwa kolory na komórkę: górny i dolny piksel.
- Unicode musi być poprawnie obsługiwany przez terminal/font.

## Pliki

- `src/Cpu.Tui/Graphics/Pixel.cs`
- `src/Cpu.Tui/Graphics/PixelBuffer.cs`
- `src/Cpu.Tui/Graphics/TerminalGraphicsRenderer.cs`
- `tests/Cpu.Tui.Tests/TerminalGraphicsRendererTests.cs`
