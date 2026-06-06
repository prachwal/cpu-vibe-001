# Project Structure

## Solution: `cpu-vibe.slnx`

```
cpu-vibe.slnx
├── src/
│   ├── Cpu.Core/          — CPU interfaces (IMemory, IDevice, ICpu, Bus)
│   ├── Cpu.Tui.Abstractions/ — Terminal rendering interfaces + base types
│   ├── Cpu.Tui/           — Terminal application (App, ANSI renderer, views)
│   ├── Cpu.Tui.Media/     — Graphics/media processing (Pixel, Canvas, JPG)
│   ├── Mos6502/           — MOS 6502 CPU emulator
│   └── Z80/               — Z80 CPU emulator
├── tests/
│   ├── Cpu.Tui.Tests/     — Tests for all TUI components
│   ├── Mos6502.Tests/     — MOS 6502 instruction tests
│   └── Z80.Tests/         — Z80 instruction tests
├── benchmarks/
│   ├── Mos6502.Benchmarks/
│   └── Z80.Benchmarks/
└── samples/
    └── Mos6502.Demo/
```

## Cpu.Tui.Abstractions

Interfaces and value types with zero external dependencies.

| File | Type | Description |
|------|------|-------------|
| `ITerminalRenderer.cs` | Interface | Low-level terminal output (SetCell, SetText, Flush) |
| `ITermView.cs` | Interface | View lifecycle (Activate, Deactivate, Render) |
| `TerminalCell.cs` | Record struct | One cell: char + foreground + background |
| `TerminalColor.cs` | Record struct | ConsoleColor or 24-bit RGB |
| `TermRect.cs` | Record struct | Rectangle (X,Y,W,H) + CenterFrame |
| `FrameGlyphs.cs` | Enum + Record | FrameStyle (Ascii/Unicode) + glyph table |
| `TerminalGraphicsMode.cs` | Enum | Rendering mode (HalfBlock, Braille, Grayscale, BestGlyph) |

## Cpu.Tui.Media

Graphics and media processing. Depends on Cpu.Tui.Abstractions.

| File | Type | Description |
|------|------|-------------|
| `Pixel.cs` | Record struct | 24-bit RGB pixel with luma |
| `PixelBuffer.cs` | Class | Pixel array + ResizeNearest + ResizeBilinear |
| `PixelMath.cs` | Static | Lerp, weighted average, tile error |
| `PixelCanvas.cs` | Class | 320×200 drawing surface (SetPixel, Clear, FillRect, DrawLine) |
| `AttributeCanvas.cs` | Class | ZX Spectrum 256×192 + 32×24 attribute grid |
| `SpectrumPalette.cs` | Static | 8 standard + 8 bright ZX Spectrum colors |
| `ColorQuantizer.cs` | Static | RGB → ConsoleColor nearest match |
| `GlyphPattern.cs` | Record struct | 2×4 pixel alpha mask per glyph |
| `GlyphAtlas.cs` | Class | Collection of glyph patterns (braille, blocks, shades) |
| `BestGlyphRenderer.cs` | Class | Per-tile best glyph selection |
| `TerminalGraphicsRenderer.cs` | Static | Render PixelBuffer with given mode |
| `JpegImageLoader.cs` | Static | ffmpeg-based JPG decoder |
| `Demo3D.cs` | Class | 3D wireframe (Vector3, Matrix4, Cube/Pyramid/Sphere) |

## Cpu.Tui

Terminal application. Depends on Cpu.Tui.Abstractions + Cpu.Tui.Media + Cpu.Core.

### Rendering pipeline

```
ITerminalRenderer  ←  AnsiTerminalRenderer  ←  TermWidget / TermFrame / TermArea
                                             ←  App.Render.cs
                                             ←  Views (ScreenView, CanvasView, …)
```

| File | Type | Description |
|------|------|-------------|
| `Rendering/AnsiBuilder.cs` | Ref struct | Zero-alloc ANSI escape builder |
| `Rendering/AnsiTerminalRenderer.cs` | Class | Double-buffered ANSI renderer (Flush per frame) |
| `Rendering/TermWidget.cs` | Class | Legacy widget with frame + auto-clear |
| `Rendering/TermFrame.cs` | Static | Draw frame borders (ASCII/Unicode) |
| `Rendering/TermArea.cs` | Static | Area operations (Clear, Write, Centered, Canvas, Screen) |
| `Rendering/TermViewManager.cs` | Class | View lifecycle manager (SwitchTo + resize) |
| `Rendering/Views/BaseTermView.cs` | Abstract | Base class implementing ITermView |
| `Rendering/Views.cs` | Classes | ScreenView, CanvasView |

### App structure (partial class)

| File | Responsibility |
|------|---------------|
| `App.cs` | Fields, constructor, Run() main loop |
| `App.Input.cs` | ReadInput, key handlers |
| `App.Modes.cs` | Mode switching, StartImageMode, TickDemo |
| `App.Render.cs` | Render dispatch, RenderScreen, RenderImage, RenderHelp, RenderDemoMenu |
| `AppServices.cs` | DI container (Microsoft.Extensions.DependencyInjection) |

### ScreenBuffer

`ScreenBuffer.cs` implements `IMemory` (from Cpu.Core). Used for the PIA terminal
display (80×25 text mode with characters + color attributes at $0800).

### Devices

| File | Description |
|------|-------------|
| `Devices/Pia/PiaDevice.cs` | PIA 6520 emulation (6 registers, IRQ, edge detect) |
| `Devices/Pia/PiaTerminalAdapter.cs` | VT52-like terminal (CSI, SGR, scroll, colors) |
| `Devices/Pia/PiaDemos.cs` | Demo strings (Lorem Ipsum, Box Drawing, Color Bars, …) |
| `Devices/Pia/EchoTerminal.cs` | Typing mode (echo keys, cursor) |

### Layout

| File | Type | Description |
|------|------|-------------|
| `Layout/TerminalLayout.cs` | Enum + Records | ScreenMode, ScreenSize, TerminalLayout.Fit() |

## Lifecycle (BaseTermView)

```
Activate(r, area)
  ├── _seeded? → Seed()          – jednorazowe przygotowanie treści
  └── Render(r, area)            – rysowanie do terminala

Render(r, area)
  └── wypełnienie area treścią    – może być wołane wiele razy (np. 3D tick)

Deactivate(r, area)
  └── TermArea.Clear(r, area)    – czyszczenie obszaru (eliminacja artefaktów)
```

See [docs/component-checklist.md](docs/component-checklist.md) for full checklist.
