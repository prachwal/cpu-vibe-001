# Ultra-fast TUI ANSI renderer

> **Referencja projektowa.** Implementacja: `src/Cpu.Tui/Rendering/AnsiTerminalRenderer.cs`. Aktualny przegląd: [overview.md](overview.md).

## Cel

Zastąpić renderowanie oparte o `System.Console.SetCursorPosition` i `Console.Write` rendererem, który:

- śledzi dirty cells;
- grupuje zmiany w ciągłe runy;
- generuje ANSI escape codes w buforze pamięci;
- robi jeden zapis do stdout na klatkę;
- nie czyści ekranu poza resize/full redraw;
- nadaje się do przyszłych emulatorów i szybkiego trybu echo/video.

## Główny problem obecnego podejścia

Obecny renderer jest już lepszy logicznie, bo ma dirty cells, ale nadal wykonuje kosztowne operacje per znak:

```csharp
Console.SetCursorPosition(col, row);
Console.ForegroundColor = fg;
Console.BackgroundColor = bg;
Console.Write(ch);
```

To jest wolne, bo każda komórka przechodzi przez API `System.Console`, które robi własną synchronizację, translację kolorów i operacje terminalowe.

Docelowo jedna ramka powinna wyglądać tak:

```csharp
buffer.Append("\x1b[12;20H");
buffer.Append("\x1b[37;40m");
buffer.Append("HELLO WORLD");
stdout.Write(buffer);
```

## Komponent docelowy

### `TerminalCell`

```csharp
public readonly record struct TerminalCell(
    char Ch,
    ConsoleColor Fg,
    ConsoleColor Bg);
```

### `ITerminalRenderer`

```csharp
public interface ITerminalRenderer : IDisposable
{
    int Width { get; }
    int Height { get; }
    void Resize(int width, int height);
    void SetCell(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg);
    void SetText(int x, int y, ReadOnlySpan<char> text, ConsoleColor fg, ConsoleColor bg);
    void Clear(ConsoleColor fg, ConsoleColor bg);
    void Flush();
}
```

### `AnsiTerminalRenderer`

Odpowiedzialność:

- przechowuje `frontBuffer`: ostatni stan znany terminalowi;
- przechowuje `backBuffer`: stan docelowy;
- `SetCell` modyfikuje tylko `backBuffer`;
- `Flush` porównuje `backBuffer` z `frontBuffer`;
- różnice grupuje w poziome runy;
- generuje ANSI do jednego bufora;
- zapisuje bufor jednym `Stream.Write`.

Szkic:

```csharp
public sealed class AnsiTerminalRenderer : ITerminalRenderer
{
    private readonly Stream _stdout;
    private TerminalCell[] _front = [];
    private TerminalCell[] _back = [];
    private byte[] _output = new byte[64 * 1024];
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    public AnsiTerminalRenderer(Stream stdout)
    {
        _stdout = stdout;
        WriteRaw("\x1b[?25l");
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _front = new TerminalCell[width * height];
        _back = new TerminalCell[width * height];
        Array.Fill(_front, TerminalCell.Unknown);
        Array.Fill(_back, new TerminalCell(' ', ConsoleColor.Gray, ConsoleColor.Black));
        WriteRaw("\x1b[2J\x1b[H");
    }

    public void SetCell(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg)
    {
        if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) return;
        _back[y * _width + x] = new TerminalCell(ch, fg, bg);
    }

    public void Flush()
    {
        AnsiBuilder builder = new AnsiBuilder(_output);
        ConsoleColor fg = ConsoleColor.Gray;
        ConsoleColor bg = ConsoleColor.Black;

        for (int y = 0; y < _height; y++)
        {
            int x = 0;
            while (x < _width)
            {
                int index = y * _width + x;
                if (_front[index] == _back[index])
                {
                    x++;
                    continue;
                }

                TerminalCell cell = _back[index];
                builder.MoveTo(x, y);
                builder.SetColor(cell.Fg, cell.Bg, ref fg, ref bg);

                while (x < _width)
                {
                    index = y * _width + x;
                    TerminalCell next = _back[index];
                    if (_front[index] == next) break;
                    if (next.Fg != fg || next.Bg != bg) break;

                    builder.WriteChar(next.Ch);
                    _front[index] = next;
                    x++;
                }
            }
        }

        _stdout.Write(_output, 0, builder.Length);
        _stdout.Flush();
    }

    public void Dispose()
    {
        WriteRaw("\x1b[0m\x1b[?25h");
    }
}
```

### `AnsiBuilder`

Odpowiedzialność:

- pisze ANSI do `byte[]`;
- unika alokacji stringów w hot path;
- mapuje `ConsoleColor` do ANSI SGR;
- wspiera `MoveTo`, `SetColor`, `WriteChar`, `WriteAscii`.

Przykładowe API:

```csharp
public ref struct AnsiBuilder
{
    private Span<byte> _buffer;
    private int _length;

    public int Length => _length;

    public void MoveTo(int x, int y);
    public void SetColor(ConsoleColor fg, ConsoleColor bg, ref ConsoleColor currentFg, ref ConsoleColor currentBg);
    public void WriteChar(char ch);
}
```

## Zasady wydajności

- Zero `Console.SetCursorPosition` w hot path.
- Zero `Console.ForegroundColor` / `Console.BackgroundColor` w hot path.
- Zero `Console.Write` per znak.
- Jeden `Stream.Write` na flush.
- Jeden `Flush` po obsłużeniu wszystkich zmian.
- Kolory zmieniane tylko gdy run wymaga innego `fg/bg`.
- Full clear tylko przy resize albo zmianie trybu renderera.
- Cursor emulatora jako zwykła dirty cell z odwróconymi kolorami.

## Integracja z `Cpu.Tui`

### Obecnie

`App` zna szczegóły terminala:

- rozmiar terminala;
- pozycje;
- kolory;
- `Console.SetCursorPosition`;
- dirty cache.

### Docelowo

`App` powinien znać tylko:

- `TerminalLayout`;
- `ITerminalRenderer`;
- `ScreenBuffer`;
- tryb ekranu.

Render ekranu:

```csharp
private void RenderScreen(TerminalLayout layout)
{
    ScreenSize actual = layout.Fit(GetRequestedScreenSize());
    Rect screen = layout.Center(actual);

    for (int row = 0; row < actual.Rows; row++)
    {
        for (int col = 0; col < actual.Cols; col++)
        {
            _renderer.SetCell(
                screen.X + col,
                screen.Y + row,
                _screen.GetChar(col, row),
                _screen.GetForeground(col, row),
                _screen.GetBackground(col, row));
        }
    }

    _renderer.Flush();
}
```

## Plan migracji

### Etap 1: Izolacja renderera

1. Dodać `src/Cpu.Tui/Rendering/TerminalCell.cs`.
2. Dodać `src/Cpu.Tui/Rendering/ITerminalRenderer.cs`.
3. Przenieść obecny cache komórek z `App` do `ConsoleCellRenderer`.
4. `App` ma wywoływać tylko `SetCell`, `SetText`, `Clear`, `Flush`.
5. Build musi przejść bez zmiany zachowania.

Efekt:

- logika layoutu oddzielona od sposobu pisania do terminala;
- można wymienić backend bez ruszania `App`.

### Etap 2: ANSI backend

1. Dodać `AnsiTerminalRenderer`.
2. Używać `Console.OpenStandardOutput()` jako `Stream`.
3. Na starcie pisać:
   - hide cursor: `\x1b[?25l`;
   - reset style: `\x1b[0m`.
4. Na dispose pisać:
   - reset style: `\x1b[0m`;
   - show cursor: `\x1b[?25h`.
5. `Flush` generuje bufor ANSI i jeden `Stream.Write`.

Efekt:

- brak per-cell `System.Console` calls;
- pierwsza duża poprawa wydajności.

### Etap 3: Run grouping

1. Porównywać `front/back` wierszami.
2. Dirty komórki grupować w poziome runy.
3. Przerywać run tylko gdy:
   - trafia clean cell;
   - zmienia się kolor;
   - kończy się wiersz.
4. Jeden cursor move na run, nie na znak.

Efekt:

- przy pisaniu tekstu jedno przesunięcie kursora i jeden run zamiast N pozycji.

### Etap 4: Raw input

1. Usunąć zależność od `Console.KeyAvailable` jako głównego mechanizmu.
2. Dodać `ITerminalInput`.
3. Na Unix użyć raw mode przez `termios`.
4. Na Windows użyć `Console.ReadKey` tymczasowo albo Win32 input później.

Efekt:

- mniejsze opóźnienia inputu;
- brak wpływu trybu konsoli na renderer.

### Etap 5: Frame scheduler

1. `App` nie renderuje w pętli.
2. Zdarzenia ustawiają dirty:
   - input;
   - zmiana `ScreenBuffer`;
   - blink kursora;
   - resize.
3. Opcjonalny limit FPS dla emulatorów video, np. 50/60 Hz.
4. Jeśli nie ma dirty, pętla śpi.

Efekt:

- brak mrugania;
- brak zbędnej pracy CPU.

### Etap 6: Integracja ze screen memory

1. `ScreenBuffer.Write` oznacza dirty cell dla adresów znaków/atrybutów.
2. Renderer pyta tylko o dirty zakresy, nie skanuje całego ekranu.
3. Dla trybu 25x80 maksymalny koszt pełnej ramki to 2000 komórek.
4. Dla typowego inputu koszt to 1-2 komórki.

Efekt:

- echo mode i edycja tekstu są praktycznie natychmiastowe;
- emulator video może aktualizować tylko zmienione znaki.

## Docelowe pliki

```
src/Cpu.Tui/
├── App.cs
├── ScreenBuffer.cs
├── Rendering/
│   ├── ITerminalRenderer.cs
│   ├── TerminalCell.cs
│   ├── ConsoleCellRenderer.cs
│   ├── AnsiTerminalRenderer.cs
│   └── AnsiBuilder.cs
└── Input/
    ├── ITerminalInput.cs
    └── ConsoleTerminalInput.cs
```

## Kryteria sukcesu

- W echo mode wpisanie znaku zmienia tylko:
  - poprzedni cursor cell;
  - wpisany znak;
  - nowy cursor cell.
- Ramka nie jest dotykana przy wpisywaniu.
- Przy blinku kursora zmienia się tylko jedna komórka.
- Przy resize jest jeden full redraw.
- `dotnet build src/Cpu.Tui/Cpu.Tui.csproj` przechodzi.
- Smoke run startuje bez wyjątku.

## Najważniejsza decyzja

Nie optymalizować dalej `System.Console`. Trzeba utrzymać `System.Console` tylko jako fallback/debug backend, a docelowy szybki backend oprzeć o batched ANSI + `Stream.Write`.

## Przełączalny styl ramek

### Cel

Renderer ma obsługiwać Unicode, ale UI nie może zakładać, że każdy terminal/font poprawnie pokaże box drawing. Dlatego ramki powinny mieć przełączalny styl:

- `Ascii` — domyślny, zawsze czytelny: `+`, `-`, `|`;
- `Unicode` — ładniejszy, dla terminali z poprawnym UTF-8 i fontem: `┌`, `┐`, `└`, `┘`, `─`, `│`.

### Model

```csharp
public enum FrameStyle
{
    Ascii,
    Unicode
}

public readonly record struct FrameGlyphs(
    char TopLeft,
    char TopRight,
    char BottomLeft,
    char BottomRight,
    char Horizontal,
    char Vertical)
{
    public static readonly FrameGlyphs Ascii = new('+', '+', '+', '+', '-', '|');
    public static readonly FrameGlyphs Unicode = new('┌', '┐', '└', '┘', '─', '│');
}
```

### Integracja z `App`

`App` trzyma aktualny styl:

```csharp
private FrameStyle _frameStyle = FrameStyle.Ascii;
```

Przełącznik klawiszem, np. `F6`:

```csharp
case ConsoleKey.F6:
    _frameStyle = _frameStyle == FrameStyle.Ascii
        ? FrameStyle.Unicode
        : FrameStyle.Ascii;
    _statusText = _frameStyle.ToString();
    _dirty = true;
    _fullRedraw = true;
    break;
```

Rysowanie ramki nie używa literałów:

```csharp
private void DrawFrame(int left, int top, int w, int h)
{
    FrameGlyphs g = _frameStyle == FrameStyle.Unicode
        ? FrameGlyphs.Unicode
        : FrameGlyphs.Ascii;

    _renderer.SetCell(left, top, g.TopLeft, frameFg, frameBg);
    _renderer.SetCell(left + w - 1, top, g.TopRight, frameFg, frameBg);
    _renderer.SetCell(left, top + h - 1, g.BottomLeft, frameFg, frameBg);
    _renderer.SetCell(left + w - 1, top + h - 1, g.BottomRight, frameFg, frameBg);

    for (int c = left + 1; c < left + w - 1; c++)
    {
        _renderer.SetCell(c, top, g.Horizontal, frameFg, frameBg);
        _renderer.SetCell(c, top + h - 1, g.Horizontal, frameFg, frameBg);
    }

    for (int r = top + 1; r < top + h - 1; r++)
    {
        _renderer.SetCell(left, r, g.Vertical, frameFg, frameBg);
        _renderer.SetCell(left + w - 1, r, g.Vertical, frameFg, frameBg);
    }
}
```

### Pasek funkcyjny

Pasek powinien pokazywać aktywny styl:

```text
F1 Help  F2 25x80  F3 Echo  F4 Demo  F5 Refresh  F6 Frame:ASCII  Esc Quit
```

Po przełączeniu:

```text
F6 Frame:UTF
```

### Testy

Minimalne testy:

- `FrameGlyphs.Ascii` ma `+ - |`;
- `FrameGlyphs.Unicode` ma `┌ ┐ └ ┘ ─ │`;
- `DrawFrame` w trybie ASCII wysyła ASCII;
- `DrawFrame` w trybie Unicode wysyła UTF-8;
- przełączenie stylu ustawia full redraw;
- pasek pokazuje aktualny styl.

### Zasada domyślna

Domyślnie `Ascii`. Unicode ma być funkcją opt-in, bo terminal użytkownika może mieć:

- niepoprawne kodowanie;
- font bez box drawing;
- terminal emulujący CP437/legacy behavior;
- konfigurację, która pokazuje box drawing jako bloki.
