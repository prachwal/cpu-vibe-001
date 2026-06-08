# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Karpathy's 4 Rules for AI Coding

1. **Think Before Coding**: Never guess or make assumptions. If anything is ambiguous, pause, list the trade-offs, and ask for clarification before writing any code.
2. **Simplicity First**: Write the absolute minimum amount of code necessary to solve the problem. Avoid premature abstractions, unnecessary configuration, or over-engineering.
3. **Surgical Changes**: Make precise edits. Modify only what is strictly required for the task. Do not touch or refactor neighboring code, and strictly follow the existing codebase style.
4. **Goal-Driven Execution**: Work toward measurable success criteria. Run tests (including edge cases) frequently and iterate dynamically until all checks pass perfectly.

## Quick reference

```bash
# Build
dotnet build cpu-vibe.slnx

# Run the TUI app
dotnet run --project src/Cpu.Tui
# or
bash run.sh

# Run specific test suites
dotnet test tests/Mos6502.Tests/Mos6502.Tests.csproj
dotnet test tests/Z80.Tests/Z80.Tests.csproj
dotnet test tests/Cpu.Tui.Tests/Cpu.Tui.Tests.csproj
dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj
dotnet test tests/Cpu.Pet.Tests/Cpu.Pet.Tests.csproj
dotnet test tests/Cpu.Vic20.Tests/Cpu.Vic20.Tests.csproj
dotnet test tests/Cpu.C16.Tests/Cpu.C16.Tests.csproj
dotnet test tests/Cpu.Chips.Tests/Cpu.Chips.Tests.csproj

# Run a single test by filter
dotnet test tests/Mos6502.Tests/ --filter "FullyQualifiedName~AdcTests"
dotnet test tests/Z80.Tests/ --filter "FullyQualifiedName~DdFdTests"
dotnet test tests/Cpu.Pet.Tests/ --filter "PetIeeeBus"

# Check 6502 implementation progress
bash scripts/progress.sh
```

## Architecture overview

### Stack

.NET 8, C# 12. No reflection in hot paths. No `using static`. `byte` for 8-bit values, `ushort` for 16-bit, `sbyte` for signed offsets.

### Core abstractions (`src/Cpu.Core`)

`IMemory` / `ICpu` — shared interfaces for all emulators. `IBus` / `Bus` — address bus that dispatches reads/writes to attached `IDevice` instances by address. Unmapped reads return the last bus value (floating bus). Devices are scanned last-to-first on read/write so later-attached devices take priority.

### CPU emulators

Two independent emulators; rules for one do not apply to the other.

**MOS 6502** (`src/Mos6502/`):
```
Core/         — Cpu, Registers, Flags, Memory, Throttle, CpuVariant (6502/65C02/2A03)
Instructions/ — InstructionTable.cs (dispatch table), InstructionHandler.cs
Instructions/{Mnemonic}/{Mnemonic}{AddressingMode}.cs  — one file per opcode
```
Adding an instruction: create `src/Mos6502/Instructions/{Mnemonic}/{Mnemonic}{Mode}.cs`, register in `InstructionTable.cs`. Every `Execute` must add cycles (`cpu.Cycles += N`) at the end.

**Z80** (`src/Z80/`):
```
Core/         — Cpu, Registers, Flags, Memory
Instructions/ — InstructionTable.cs, InstructionHandler.cs, AluHelper.cs, RegisterHelper.cs
```
Prefixes `CB`, `ED`, `DD`, `FD`, `DD CB`, `FD CB` must consume exactly the bytes a real Z80 consumes. `DD`/`FD` substitute `HL/H/L` with `IX/IY` only where the real Z80 does. ROM references: `tests/roms/zexdoc.com`, `tests/roms/zexall.com`.

### Machine board (`src/Cpu.Board`)

`MachineBoard` — generic builder driven by JSON machine profiles (in `profiles/`). `BusBackedMemory` bridges CPU↔bus with I/O routing. `MachineProfile` is the JSON model: `CpuProfile` (entryPoint, resetVector), `MemoryRegion`, `PiaProfile`, `VicProfile`, `ViaProfile`, `DisplayProfile`.

### Chip emulators (`src/Cpu.Chips`)

VIA6522, CRTC6545, VIC6560, Color RAM — shared across machine projects.

### Machine modules

Each machine is a separate project with a `*Module`, `*Machine`, `*View`, `Devices/`, and profile JSON:

| Project | F-key | CPU | Key chips |
|---------|-------|-----|-----------|
| `Cpu.Apple1` | F8 | 6502 | PIA 6520 |
| `Cpu.Pet` | F11 | 6502 | PIA1/PIA2, VIA 6522, CRTC 6545 |
| `Cpu.Vic20` | F6 | 6502 | VIC6560, VIA 6522 ×2 |
| `Cpu.C16` | F10 | 7501/8501 | TED7360 |

### Terminal UI (`src/Cpu.Tui`)

**Application loop** (`App.cs`): resize → render → read input → dispatch key/mouse → tick.

**Module hierarchy** — tree rooted at `MainMenuModule`. Each module implements `IAppModule` (from `Cpu.Module.Abstractions`): `OnKey` returns `true` = consumed (stops parent fallback), `OnRender`, `OnTick`, `OnMouse`, `OnActivate`/`OnDeactivate`.

**`ModuleBase`** (base class for all modules) provides:
- Left panel (30 chars) when terminal width ≥ 116
- Optional right reference panel (`SecondaryPanelWidth` > 0)
- `RenderContent(x, y, w, h)` for the content area
- `OnKeyCore(key)` for module-local key handling
- `ErrorCollector` for centralized error logging

Module structure:
```csharp
public sealed class MyModule : ModuleBase
{
    public override string Name => "MyModule";
    public MyModule(ITuiAppConfiguration config, ErrorCollector? errors = null) : base(config, errors) { }
    protected override bool OnKeyCore(ConsoleKeyInfo key) { ... }
    protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h) { ... }
}
```

**Key dispatch rules**: `child.OnKey()` first; if `false`, `MainMenuModule` handles Esc + F-keys. A leaf must not return `true` without performing an action, and must not swallow F-keys (e.g. `ScreenModule`).

**Module registration**: `AppServices` scans `*.dll` for `IAppModule` implementations (transient). Order is controlled by `ModuleOrderByType`.

**Settings**: `ITuiAppConfiguration` injected into `ModuleBase`. Settings file: `tui-settings.json` beside the exe. `Setup` module saves + reloads + fires `Changed`.

### Rendering pipeline

```
ITerminalRenderer (SetCell, Flush) — AnsiTerminalRenderer with double-buffer diff
  ↓
PresentationSession — sole API for views (Clear, Write, DrawFrame, RenderCanvas, RenderScreen, FitImage)
  ↓
ITermView / BaseTermView — lifecycle (Activate → Seed → Render, Deactivate → Clear)
```

Rules:
- Always call `session.Clear()` before rendering to eliminate artifacts
- Always use `frame.Inner` when rendering into a frame, never `frame` directly
- Default clear color: `TerminalCell.Black` (Black/Black), never Gray/Black
- `session.FitImage()` is the single image-scaling implementation — do not duplicate

### Layering rules (critical)

- **No business logic in views** — key translation, data conversion, matrix→PETSCII mapping go in `Devices/` (hardware adapter), `System/` (machine logic), or `Chips/` (chip emulator). Views (`Rendering/Views/`) only call ready-made adapters.
- **No hardware workarounds** — never patch memory artificially, force screen state, or quick-fix emulation accuracy. Every change must emulate real hardware behavior. Workarounds require explicit user approval.
- Binary accuracy over implementation convenience. Every instruction must account for cycles.

## Key files to read before editing

| Area | Read first |
|------|-----------|
| TUI key routing or modules | `docs/tui/key-map.md`, `docs/tui/overview.md` |
| Any TUI change | `docs/tui/overview.md` |
| New machine chip | `docs/implementation-lessons.md` |
| 6502 instructions | `docs/cpu/6502-architecture.md`, `FLOW.md` |
| Z80 instructions | `docs/z80/zexall-dd-prefix-bug.md` |
| Apple 1 I/O | `docs/machines/apple1.md` |
| PET / IEEE-488 | `docs/machines/pet.md` |

After **any architecture change** (new module, key routing, panel layout, I/O model): update `AGENTS.md`, `docs/tui/key-map.md`, and `docs/README.md`.
