---
name: cpu-tui-graphics
description: "Use when working on Cpu.Tui terminal graphics framework — rendering, views, pixel buffers, graphics modes, ANSI output, or the terminal application itself. Covers architecture, lifecycle, code patterns, and rules."
---

# Cpu.Tui — Terminal Graphics Framework & Application

## Architecture Overview

Three-layer project stack:

| Layer | Project | Dependencies | Purpose |
|-------|---------|-------------|---------|
| Abstractions | `Cpu.Tui.Abstractions` | (none) | Interfaces + value types |
| Media | `Cpu.Tui.Media` | Abstractions | Pixel buffers, canvas, glyph renderers |
| App | `Cpu.Tui` | Abstractions + Media + Core | Terminal application, views, ANSI output |

## Project: Cpu.Tui.Abstractions (`src/Cpu.Tui.Abstractions/`)

### `ITerminalRenderer` — `Cpu.Tui.Rendering.ITerminalRenderer`
Low-level output interface. One instance, wraps stdout.

```
Width/Height → Resize(w,h) → SetCell(x,y,ch,fg,bg) → Clear(fg,bg) → Flush()
```

### `TerminalCell` — `readonly record struct`
Canonical clear cell: `TerminalCell.Black` (space, Black/Black). `TerminalCell.Unknown` (NUL, Black/Black) used as sentinel in front buffer.

### `TerminalColor` — `readonly record struct`
Dual-mode color: `ConsoleColor` (16-color) or 24-bit RGB via `TerminalColor.FromRgb()`.

### `TermRect` — `readonly record struct`
Position + size: `X`, `Y`, `W`, `H`, `X2`, `Y2`. Key methods:
- `CenterFrame(int contentW, int contentH)` — clamped to area, never overflows
- `Inner` → `(X+1, Y+1, W-2, H-2)` — content area inside a frame border

### `FrameStyle` / `FrameGlyphs` — `Cpu.Tui.Rendering`
`FrameStyle.Ascii` ( `+` / `-` / `|` ) or `FrameStyle.Unicode` ( `┌┐└┘─│` ).

### `TerminalGraphicsMode` — `Cpu.Tui`
Enum: `HalfBlockColor`, `BrailleMono`, `Grayscale`, `BestGlyph`, `BestGlyphTrueColor`.

### `RenderLog` — `Cpu.Tui.Diagnostics`
Env `CPU_TUI_RENDER_LOG=1` enables file logging. `[Conditional("DEBUG")]` — works only in Debug builds.

## Project: Cpu.Tui.Media (`src/Cpu.Tui.Media/`)

### `Pixel` — `readonly record struct(byte R, byte G, byte B)`
24-bit RGB. `Luma` = weighted luminance. Static presets: `Black`, `White`, `Red`, `Blue`, `Green`.

### `PixelBuffer` — `sealed class`
2D pixel array. `ResizeNearest()`, `ResizeBilinear()`. Bounds-safe `GetPixel`/`SetPixel`.

### `PixelCanvas` — `sealed class`
320×200 drawing surface. Primitives: `SetPixel`, `GetPixel`, `Clear`, `FillRect`, `DrawLine` (Bresenham), `DrawRect`.

### `PixelMath` — `static class`
Color math: `Lerp`, `WeightedAverage`, `DistanceSquared`, foreground/background estimation, tile error computation (quantized and luma-weighted).

### `TerminalGraphicsRenderer` — `static class`
Dispatches pixel buffer rendering by mode. Static methods: `Render`, `RenderHalfBlock`, `RenderBrailleMono`, `RenderGrayscale`, `RenderBestGlyph`, `RenderBestGlyphTrueColor`.

### `BestGlyphRenderer` — `sealed class`
Per-cell glyph selection from 2×4 atlas. Two entry points: `Render(...)` → 16-color quantized, `RenderTrueColor(...)` → 24-bit RGB.

### `GlyphAtlas` / `GlyphPattern`
2×4 tile atlas. `GlyphPattern`: `char Glyph` + `byte[8] Alpha` mask. `FromBinary()` and `Full()` / `Empty()` helpers.

### `ColorQuantizer` — `static class`
`NearestConsoleColor(byte r, byte g, byte b)` → closest of 16 `ConsoleColor` values. `ConsoleColorToRgb(ConsoleColor)`.

### `AttributeCanvas` — `sealed class`
ZX Spectrum-style: 256×192 bitmap + 32×24 attribute grid (8×8 cells, INK/PAPER). `RenderTo(PixelBuffer)`.

### `JpegImageLoader` — `static class`
`Load(string path)` → `PixelBuffer`. Uses `ffmpeg` pipe for JPEG decoding.

### `Demo3D` — `sealed class`
Wireframe 3D shapes (cube, pyramid, sphere). Rotation (WASD), zoom (arrows). Renders to `PixelCanvas`.

## Project: Cpu.Tui (`src/Cpu.Tui/`)

### Rendering Files (`src/Cpu.Tui/Rendering/`)

#### `AnsiTerminalRenderer : ITerminalRenderer`
Double-buffered ANSI output to stdout. Tracks `_lastFg`/`_lastBg` across flushes to avoid inherited colors.
- `Resize()` — allocates front/back buffers, emits reset sequence
- `Flush()` — compares front vs back, emits only changed cells as ANSI escape sequences
- `InvalidateArea(x,y,w,h)` — marks region as changed for next Flush

#### `AnsiBuilder` — `ref struct`
Zero-alloc ANSI sequence builder. `MoveTo(col,row)`, `SetColor(fg,bg)`, `WriteChar(ch)`.

#### `PresentationSession` — `sealed class`
Single-owner session bundling renderer + area. Preferred API for view rendering.
- `Clear()` — canonical Black/Black
- `Write(x,y,text,fg,bg)` — with auto-truncation
- `Centered(lines,fg,bg)`
- `DrawFrame(rect,style,title?)` → returns inner rect
- `CenterFrame(contentW,contentH)` → centered (clamped)
- `RenderCanvas(pixels,mode,inner)` → pixel graphics
- `RenderScreen(screen,rows,cols,inner)` → text screen
- `FitImage(img,maxCols,maxRows,mode)` → `(cols,rows)` — unified scaling

#### `ITermView`
View lifecycle interface: `Activate`, `Deactivate`, `Render` (with `PresentationSession` overload).

#### `BaseTermView : ITermView`
Abstract base. `Seed()` called lazily on first Activate. `Deactivate` clears area with `TerminalCell.Black`.

#### `TermViewManager`
Manages active view. `SwitchTo(newView, area)` → deactivates old, activates new. `Render(area)` handles resize.

#### `TermArea` — `static class`
Stateless helpers: `Clear`, `Write`, `Centered`, `Canvas`, `Screen`. Default clear: Black/Black.

#### `TermFrame` — `static class`
`Draw(renderer, rect, style, title?)`.

#### `TermWidget`
Managed widget: frame + inner area + position tracking with auto-clear on move.

### Views (`src/Cpu.Tui/Rendering/Views.cs`)

| View | Key | Description |
|------|-----|-------------|
| `ScreenView` | default | 80×25 / 40×24 text screen with cursor |
| `CanvasView` | F9 | Pixel canvas with 3 demos, 5 graphics modes |
| `HelpView` | F1 | Keyboard shortcut reference |
| `DemoMenuView` | F4 | PIA demo selector |
| `ImageView` | F7 | JPEG viewer from `samples/` |

### App partials (`src/Cpu.Tui/App.*.cs`)

| File | Responsibility |
|------|---------------|
| `App.cs` | Fields, constructor, `Run()` main loop |
| `App.Input.cs` | Key dispatch, mode-specific handlers, `EnterCanvasMode()` |
| `App.Render.cs` | Render dispatch, screen/canvas/image rendering, function bar, info panel |
| `App.Modes.cs` | Screen mode cycling, PIA demo playback, image mode startup |

### Layout (`src/Cpu.Tui/Layout/TerminalLayout.cs`)

- `TerminalLayout(Width, Height)` — terminal dimensions. `ContentHeight = Height - 1` (reserves row for function bar). `CanRender ⇒ Width ≥ 40 && Height ≥ 25`.
- `ScreenMode` — `Rows24Cols40`, `Rows25Cols40`, `Rows25Cols80`.
- `ScreenSize(Rows, Cols)`.
- `TerminalLayout.Fit(requested)` — fits screen size to available space.

### Devices (`src/Cpu.Tui/Devices/Pia/`)

- `PiaDevice : IDevice` — PIA 6520 emulation (6 registers, IRQ, edge detection)
- `PiaTerminalAdapter : IPiaTerminal` — VT52-like terminal adapter, fills ScreenBuffer from PIA
- `EchoTerminal` — simple keyboard typing mode, writes to ScreenBuffer
- `PiaDemos` — 5 demo sequences (Lorem Ipsum, Box Drawing, Color Bars, Cursor Movement, Scrolling)

### App Services (`AppServices.cs`)

DI wiring. Singletons: `Stream` → `AnsiTerminalRenderer` → `ScreenBuffer` → `EchoTerminal` → `PiaDevice` → `PiaTerminalAdapter` → `App`.

## Rendering Pipeline

```
App.Run()
  → layout changed? → renderer.Resize()
  → dirty? → App.Render()
    → Render(layout, fullRedraw)
      → mode dispatch: RenderScreen / RenderCanvas / RenderImage / RenderHelp / RenderDemoMenu
      → RenderFunctionBar()
      → renderer.Flush()
```

### Canvas Rendering Detail
```
RenderCanvas(layout)
  → split area: canvas (left) + info panel (right, if width ≥ 66)
  → CanvasView.Render(renderer, canvasTerm, session)
    → FitImage(pixels, maxCols, maxRows, mode) → (cols, rows)
    → CenterFrame(cols, rows) → frame rect
    → Clear orphaned (if frame moved)
    → DrawFrame(frame, title)
    → TerminalGraphicsRenderer.Render(pixels, mode, inner)
  → RenderCanvasInfoPanel(canvasTerm)
    → Draw frame + write text: canvas res, cell res, mode, colors, terminal size
```

## Graphics Mode Details

| Mode | Pixels per cell | Colors | Resize | Use case |
|------|----------------|--------|--------|----------|
| `HalfBlockColor` | 1×2 | 16 | Nearest | Fast, colorful, blocky |
| `BrailleMono` | 2×4 | 2 | Nearest | Detailed mono, 8 dots/cell |
| `Grayscale` | 1×2 | 24 | Nearest | Smooth grayscale |
| `BestGlyph` | 2×4 | 2 | Nearest | Best mono detail, pattern dithering |
| `BestGlyphTrueColor` | 2×4 | 16.7M | Bilinear | Best quality, 24-bit color |

## Code Rules

- Every view: `BaseTermView` → auto-clear on `Deactivate`
- `PresentationSession.Clear()` before `Write`/`Centered` to avoid artifact ghosts
- Frame content: `session.DrawFrame(rect, style, title)` → use returned `.Inner` for content
- `PresentationSession.FitImage()` is the single scaling implementation — do not duplicate
- Canvas rendering: always call `session.RenderCanvas(buffer, mode, frame.Inner)` (not `frame`)
- Screen rendering: always call `session.RenderScreen(screen, rows, cols, frame.Inner)` (not `frame`)
- Default clear: `TerminalCell.Black` (Black/Black) everywhere
- `TerminalCell.Empty` was removed — do not reintroduce
- Flush color tracking: `AnsiTerminalRenderer` tracks `_lastFg`/`_lastBg` — Flush starts from terminal's actual state, not resetting to Black/Black
- Info panel: 26 columns wide, appears when terminal ≥ 66 columns, renders in canvas mode only

## Testing

```
dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj
```

Test patterns:
- `CanvasViewTests` — Activate seeds content, Deactivate clears, CycleMode/NextPrevDemo cycle, TermViewManager switch preserves clear
- `FakeTerminalRenderer` — test double: `GetCell(x,y)` → `TerminalCell`
