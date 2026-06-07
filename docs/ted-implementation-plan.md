# TED7360 Implementation Plan

## Overview

The MOS Technology 7360/8360 TExt Display (TED) is a combined video + sound + I/O chip used in Commodore 16, 116, and Plus/4.

**Key specs:**
- 34 registers at $FF00-$FF1F, $FF3E-$FF3F
- 5 video modes (text/graphics, HiRes/Multi, ECM)
- 121 colors (16 chrominances × 8 luminances + black)
- 3 timers (1 interval, 2 one-shot)
- 2 sound channels (square + noise)
- Keyboard matrix input (8×8)
- Raster interrupt
- Hardware cursor
- NTSC/PAL support
- DRAM refresh
- Twice clock / single clock

## Architecture layers

```
src/Cpu.Chips/TED7360/
├── TED7360Chip.cs          — rejestr file + logika układu
├── TED7360Constants.cs     — rejestry, maski bitów, wartości default
├── TED7360Device.cs        — wrapper IDevice dla Bus
├── TED7360Video.cs         — rendering pixeli (wszystkie tryby)
├── TED7360Palette.cs       — 121 kolorów → RGB
├── TED7360Audio.cs         — 2 kanały square/noise
├── TED7360Timer.cs         — 3 timery

src/Cpu.C16/
├── System/C16Machine.cs    — MachineBoard + TED + RAM + ROM
├── Devices/C16HostKeyMap.cs— host → C16 matrix
├── Rendering/Views/C16View.cs — widok TUI (tylko Render)
├── Modules/C16Module.cs    — moduł TUI (F-key, panel)
├── profiles/c16.json       — profil 16KB/32KB
├── profiles/c16-pal.json   — PAL
├── profiles/c116.json      — C116 (16KB)
├── profiles/plus4.json     — Plus/4 (64KB)
└── roms/commodore-c16/     — ROM-y z VICE
```

## Phase 1: TED chip base (minimal boot)

Goal: CPU boots from ROM, TED in default text mode, screen shows something.

### 1.1 Constants + Register File

**File:** `TED7360Constants.cs`

- All register addresses: `REG_FF00_TIMER1LO` through `REG_FF1F_CHRAST`
- Shadow registers: `REG_FF3E_SWITCHROM`, `REG_FF3F_SWITCHRAM`
- Register default values (from datasheet):
  - `$FF06` = 0x1B (DEN=1, 25 rows)
  - `$FF07` = 0x08 (PAL) / 0x48 (NTSC)
  - `$FF0A` = 0xA2
  - `$FF12` = 0xC4
  - `$FF13` = 0xD1
  - `$FF14` = 0x0F
- Bit masks for each register
- Screen dimensions: 40×25 (default), 38×24 (CSEL/RSEL)
- Video mode constants: `MODE_HIRES_CHAR`, `MODE_MULTI_CHAR`, `MODE_ECM`, `MODE_HIRES_BITMAP`, `MODE_MULTI_BITMAP`
- Default palette (first 8 colors like VIC-II)

### 1.2 Chip class

**File:** `TED7360Chip.cs`

```csharp
public sealed class TED7360Chip
{
    private readonly byte[] _reg = new byte[64];    // $FF00-$FF3F
    private int _rasterCounter;                      // current scanline
    private bool _palMode;

    // Timers
    private ushort _timer1, _timer1Latch;            // interval, reloads
    private ushort _timer2, _timer3;                  // one-shot, wraps to $FFFF
    private bool _t1Running, _t2Running, _t3Running;

    // Audio oscillator state
    private double[] _phase = new double[2];

    // Keyboard matrix
    private byte _keyboardRow;                       // last written row select

    // Pixel buffer (320×200)
    private readonly byte[] _pixels = new byte[320 * 200];
}
```

Properties:
- `this[int reg]` — raw register read (for diagnostics)
- `ReadByte(ushort addr)` / `WriteByte(ushort addr, byte val)` — register access with:
  - Timer start/stop behavior ($FF00-$FF05)
  - Read-only bits (unused bits = 1, ROM/RAM bit in $FF13 RO)
  - $FF1C/$FF1D raster counter read
  - $FF1E horizontal position read
  - $FF09 IRQ flags (clear on write)
  - $FF3E/$FF3F shadow register paging
- `Reset()` — set defaults
- `SetPalMode(bool pal)` — switch video timing
- `HasInterrupt` / `AcknowledgeInterrupt()`

**Lessons applied:**
- `ToCpuAddress`-like bug: TED registers are flat at $FF00-$FF3F. No address translation needed. ✓
- Register R/W behavior: check datasheet for each register. Unused bits = 1 ✓
- IRQ: TED HAS raster IRQ (unlike VIC-I). Check datasheet before implementing ✓

### 1.3 Video: text mode rendering

**File:** `TED7360Video.cs`

Static class or separate object that renders pixel buffer from chip state.

Registers needed:
- `$FF06` (CR1): DEN, BMM, ECM, RSEL, YSCROLL
- `$FF07` (CR2): RVSDIS, MCM, CSEL, XSCROLL
- `$FF12` (bit 2): BMPROM
- `$FF13`: chargen address, SINGLECLK
- `$FF14`: screen memory address
- `$FF15-$FF19`: color registers
- `$FF0C/$FF0D`: cursor position

**Text mode (default):**
```
ECM=0 BMM=0 MCM=0 RVSDIS=0 → HiRes char, 128 chars
```

- Read screen memory from CPU address space: 2KB (1KB color + 1KB chars)
  - Address from `$FF14` bits 7-3 (2KB boundaries)
  - First 1KB = color/attribute RAM
  - Second 1KB = character codes
- Read chargen data from ROM or RAM
  - Address from `$FF13` bits 7-2 (1KB boundaries for 128-char mode, 2KB for 256-char)
- For each character cell (40×25):
  - Get char code from char RAM
  - Get color/attribute from color RAM
  - Bit 7 of char code = inverse (in 128-char mode)
  - Bit 7 of color byte = flash enable
  - For each of 8 raster lines: read chargen byte → 8 pixels
  - Apply color: pixel=0 → background ($FF15), pixel=1 → foreground (color byte)
- Hardware cursor: when cursor position matches current cell, render cursor
- Border: use $FF19 color around active area
- Flash: toggle flash state every 16 frames using $FF1F bits 6-3

### 1.4 Full video mode table

| Mode | ECM | BMM | MCM | RVSDIS | Resolution | Colors/cell | Char set | Special |
|------|-----|-----|-----|--------|------------|-------------|----------|---------|
| 0 | 0 | 0 | 0 | 0 | 40×25 | 2 (bg + fg) | 128 chars | Inverse via bit7 |
| 1 | 0 | 0 | 0 | 1 | 40×25 | 2 (bg + fg) | 256 chars | |
| 2 | 1 | 0 | 0 | X | 40×25 | 4 (bg0-3 + fg) | 64 chars | ECM bg from $FF16-18 |
| 3 | 0 | 0 | 1 | 0 | 40×25 | 4 (bg0-2 + fg) | 128 chars | Multi-color pixels |
| 4 | 0 | 0 | 1 | 1 | 40×25 | 4 (bg0-2 + fg) | 256 chars | Multi-color pixels |
| 5 | 0 | 1 | 0 | X | 320×200 | 2/cell | — | Bitmap |
| 6 | 0 | 1 | 1 | X | 160×200 | 4/cell | — | Bitmap multi |

**Test:** render each mode with known pattern, verify pixel output.

**Lessons applied:**
- Border ($FF19): like VIC-20 Phase 1 fix ✓
- Display enable ($FF06 bit 4): check before rendering ✓
- Tests for each mode before integration ✓

### 1.5 Palette

**File:** `TED7360Palette.cs`

```csharp
public static class TED7360Palette
{
    public static (byte R, byte G, byte B) ToRgb(byte chromaLuma);
    public static byte Pack(int luma, int chroma);
}
```

- Chrominance 0-15 (0 = black, 1-15 = colors)
- Luminance 0-7 (brightness levels)
- 15 × 8 + 1 (black) = 121 colors
- Chrominance 0 = black regardless of luminance
- First 8 colors match VIC-II at appropriate luminance

## Phase 2: Timers + IRQ

### 2.1 Timers

- Timer 1: interval mode. Counts down from written value. On underflow: reload, set IRQ flag.
- Timer 2: one-shot. Counts down from $FFFF (free running after reset). On underflow: wrap to $FFFF, set IRQ flag.
- Timer 3: like timer 2, but different register.
- Write order: low byte first (stops timer), high byte starts timer.
- All timers run at single clock frequency.

**Timer test:**
- Write value, wait N cycles, check IRQ flag
- Timer 2/3 free running from $FFFF after reset

### 2.2 Raster IRQ

- Compare `_rasterCounter` with $FF0B value (9-bit with $FF0A bit 0 as MSB)
- On match: set IRST bit in $FF09
- If enabled via $FF0A bit 1: assert IRQ

**Lessons applied:**
- VIC-I had no raster IRQ → TED HAS it ✓
- Check datasheet: reading $FF09 clears no flags, writing $FF09 with bit=1 clears that flag ✓

### 2.3 Interrupt handling

- `$FF09` (IRQST): flags for timer1/2/3, raster, lightpen(unused)
- `$FF0A` (IRQEN): enable bits for each source + raster compare bit 9
- `HasInterrupt` = any enabled flag active
- Clear: write $FF09 with 1 bits at positions to clear

## Phase 3: Audio

### 3.1 Sound channels

**File:** `TED7360Audio.cs`

- Channel 1: frequency from $FF0E (low) + $FF12 bits 1-0 (high) = 10-bit value
- Channel 2: frequency from $FF0F (low) + $FF10 bits 1-0 (high) = 10-bit value
- Waveform: square (bit 4/5 of $FF11) and/or noise (bit 6 of $FF11)
- Volume: $FF11 bits 3-0 (max 8)
- D/A mode: $FF11 bit 7

Frequency formula:
```
NTSC: reg = 1023 - (111860.78125 / freq)
PAL:  reg = 1023 - (110840.46875 / freq)
```

**Integration:** `AdvanceOscillators(double dt)` + `GetAudioSample()` similar to VIC-20 audio pipeline.

## Phase 4: Keyboard

### 4.1 Matrix via $FF08

- Write to $FF08: sets row select (strobe)
- Read from $FF08: returns column data for selected row
- 8×8 matrix

**File:** `C16HostKeyMap.cs` — map host key → (row, col). Layout differs from VIC-20.
At minimum: `TryGetPetscii(row, col)` for known keys, plus special keys map.

**Lessons applied:**
- ROM keyboard scan: find in ROM before implementing ✓
- Buffer injection: TED writes to same KERNAL buffer ($0277) pattern? ✓
- If scan is dead code → implement in `C16Machine.FillKeyboardBuffer` ✓
- Translate matrix → PETSCII in `Devices/`, not in view ✓

## Phase 5: Machine + Module

### 5.1 C16Machine

**File:** `C16Machine.cs`

```csharp
public sealed class C16Machine : IDisposable
{
    private readonly MachineBoard _board;
    private readonly TED7360Device _ted;
    // RAM, ROMs

    // TED access
    public TED7360Chip Ted => _ted.Chip;

    // Keyboard
    public void PressKey(int row, int col);
    public void FillKeyboardBuffer(int row, int col);
    public void ReleaseAllKeys();

    // Video
    public void RenderVideo();   // → TED pixel buffer
    public byte GetPixel(int x, int y);

    // Step
    public void Step(long cpuCycles);
    public void Run(long cpuCycles);
}
```

### 5.2 C16View

**File:** `C16View.cs`

- Reads pixel buffer from `_machine.RenderVideo()`
- Uses `PresentationSession.RenderCanvas()` for graphics modes
- For text modes: reads screen RAM + TED registers, renders to terminal
- No keyboard logic, no file I/O, no translation

**Lessons applied:**
- View ONLY renders. No `_heldKeys`, no `FillKeyboardBuffer`, no `ProcessPendingKeys` ✓
- All keyboard logic in `C16Machine` ✓
- No `$CE/$CF` workaround ✓

### 5.3 C16Module

**File:** `C16Module.cs`

- F6 open machine
- F5 model selector (C16 NTSC/PAL, C116, Plus/4)
- Panel: PC, registers, video mode, timer values
- Controls: F10 text/graphics toggle

### 5.4 Profiles

```
c16.json       — NTSC, 16KB RAM
c16-pal.json   — PAL,  16KB RAM
c116.json      — C116 (16KB, mniej rozszerzeń)
plus4.json     — Plus/4 (64KB, pełna)
```

## Phase 6: Tests

### Unit tests (per subsystem)

| Test | What | File |
|------|------|------|
| Registers R/W | All 34 registers with correct default + read-only/unused bits | `TED7360RegisterTests` |
| Timer 1 | Interval mode, reload, IRQ flag | `TED7360TimerTests` |
| Timer 2/3 | Free running, one-shot, wrap | |
| IRQ | Raster match, timer, enable/disable, clear | `TED7360IrqTests` |
| Palette | 121 colors, black+luminance, VIC-II compat | `TED7360PaletteTests` |
| Video text mode | 40×25, 128/256 chars, inverse, flash, cursor | `TED7360VideoTests` |
| Video ECM | 64 chars, 4 background colors | |
| Video multi-color | 4 colors/cell, mixed HiRes/Multi | |
| Video bitmap | 320×200, 2 colors/cell | |
| Video multi bitmap | 160×200, 4 colors/cell | |
| Audio | Square, noise, frequency, volume, D/A | `TED7360AudioTests` |
| Keyboard matrix | Row write, column read, mapping | `C16KeyboardTests` |
| Boot | C16 profile boots to BASIC | `C16BootTests` |

### Integration tests

| Test | What |
|------|------|
| `C16Machine_TED_WiredCorrectly` | TED attached to bus, registers readable |
| `C16Machine_Boot_ShowsBanner` | Boot 6M cycles, screen has text |
| `C16Machine_TypePrint1` | Keyboard input produces output |

## Implementation order

```
Phase 1: Constants + Register file + Text-mode video + Palette
  → boot C16 ROM, screen shows BASIC banner
  → commit: "TED7360: register file + text video + palette"

Phase 2: Timers + IRQ
  → 3 timers, raster IRQ
  → commit: "TED7360: timers + raster IRQ"

Phase 3: Remaining video modes
  → ECM, multi-color char, bitmap modes
  → commit: "TED7360: ECM + multi-color + bitmap video"

Phase 4: Audio
  → 2 square/noise channels, volume, D/A mode
  → commit: "TED7360: audio"

Phase 5: Keyboard + C16Machine + C16View + C16Module
  → Full machine with all I/O
  → commit: "C16: machine + TUI module"

Phase 6: Additional profiles (C116, Plus/4)
  → commit: "C16: C116 + Plus/4 profiles"
```

## Lessons from VIC-20/PET applied

| Lesson | Application |
|--------|-------------|
| `ToCpuAddress` 32-bit NOT bug | TED registers are flat $FF00-$FF3F, no address translation |
| Profile name → ROM directory | All profiles use `"name": "Commodore 16"` → `commodore-c16/` |
| ROM keyboard scan dead? | Find scan code in C16 ROM first. If dead → `C16Machine.FillKeyboardBuffer` |
| `"0x0FFF"` vs `"0x1000"` bug | Validate sizes: `size = end - start + 1` |
| PETSCII vs screen code | C16 uses different encoding. Check TED char ROM format |
| Raster IRQ on VIC-I (doesn't exist) | TED HAS raster IRQ. Implement correctly from datasheet |
| Timer per-cycle vs per-instruction | `Step(cpuCycles)` iterates by CPU cycle delta |
| Business logic in view | Zero keyboard logic in `C16View`. All in `C16Machine` + `Devices/` |
| Cursor blink `raw == 0xA0` | TED has hardware cursor. Check $FF0C/$FF0D register |
| Test reads wrong address | Test `_ted.ScreenAddr` before asserting screen content |
