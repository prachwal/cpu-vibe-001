# ZEXALL Z80 Test Suite — Problem Analysis

## Current Status

ZEXALL instruction exerciser partially passes. Two test groups complete successfully, third hangs.

```
<adc,sbc> hl,<bc,de,hl,sp>.... OK
add hl,<bc,de,hl,sp>.......... OK
add ix,<bc,de,ix,sp>.......... (hang — runs 500M+ iterations without completing)
```

## Bugs Fixed (3)

### 1. ED 7A Opcode Swap (EDPrefixHandler.cs:87)
- **Problem:** `EDTable[0x7A]` was mapped to `SbcHlRr.Execute` (line 87), overwriting the correct `AdcHlRr.Execute` (line 81).
- **Effect:** `ADC HL,SP` executed as `SBC HL,SP`.
- **Fix:** Removed duplicate line 87.

### 2. LDIR Read/Write Order (EdBlockOps.cs)
- **Problem:** LDIR read from source and write to destination were not atomic — concurrent modification of overlapping memory caused infinite loops.
- **Fix:** Changed to `byte val = Read(HL); Write(DE, val);` pattern.

### 3. LD A,(DE) / LD (DE),A Opcode Swap (InstructionTable.cs:58-61)
- **Problem:** Four opcodes had read/write handlers swapped:
  - `0x02` `LD (BC),A` → was `LdAFromBc` (read), should be `LdBcFromA` (write)
  - `0x12` `LD (DE),A` → was `LdAFromDe` (read), should be `LdDeFromA` (write)
  - `0x0A` `LD A,(BC)` → was `LdBcFromA` (write), should be `LdAFromBc` (read)
  - `0x1A` `LD A,(DE)` → was `LdDeFromA` (write), should be `LdAFromDe` (read)
- **Effect:** `LD A,(DE)` wrote A to memory instead of reading, corrupting the counter terminal at `0x1CEE` during the `count()` function. This made the counter immediately reach its terminal state, causing zexall to run only 1 test case instead of ~72,000 per instruction group.
- **Fix:** Swapped handler assignments.

## Current Open Bug: DD Prefix Missing Instructions

### Symptom
The `add ix,<bc,de,ix,sp>` test group hangs. The zexall binary runs 500M+ iterations without completing this group.

### Root Cause Analysis
The DD prefix handler (`DDFdPrefixHandler.cs`) maps 33 opcodes. Two documented and ~41 undocumented opcodes fall through to `NopDdFd` (no-op, 4 cycles).

#### Missing Documented Opcodes (Critical)
| Opcode | Instruction | Cycles | Impact |
|--------|------------|--------|--------|
| `DD 23` | INC IX | 10 | 16-bit increment of index register. Used by zexall test framework. |
| `DD 2B` | DEC IX | 10 | 16-bit decrement of index register. Used by zexall test framework. |

#### Missing Undocumented IXh/IXl Opcodes (Important)
On real Z80, DD prefix replaces H/L with IXh/IXl in all instructions referencing those registers.

**LD r,IXh/IXl (14 opcodes):**
`DD 44` LD B,IXh | `DD 45` LD B,IXl | `DD 4C` LD C,IXh | `DD 4D` LD C,IXl |
`DD 54` LD D,IXh | `DD 55` LD D,IXl | `DD 5C` LD E,IXh | `DD 5D` LD E,IXl |
`DD 7C` LD A,IXh | `DD 7D` LD A,IXl

**LD IXh/IXl,r (12 opcodes):**
`DD 60` LD IXh,B | `DD 61` LD IXh,C | `DD 62` LD IXh,D | `DD 63` LD IXh,E |
`DD 65` LD IXh,IXl | `DD 67` LD IXh,A |
`DD 68` LD IXl,B | `DD 69` LD IXl,C | `DD 6A` LD IXl,D | `DD 6B` LD IXl,E |
`DD 6C` LD IXl,IXh | `DD 6F` LD IXl,A

**ALU A,IXh/IXl (16 opcodes):**
`DD 84` ADD A,IXh | `DD 85` ADD A,IXl |
`DD 8C` ADC A,IXh | `DD 8D` ADC A,IXl |
`DD 94` SUB IXh | `DD 95` SUB IXl |
`DD 9C` SBC A,IXh | `DD 9D` SBC A,IXl |
`DD A4` AND IXh | `DD A5` AND IXl |
`DD AC` XOR IXh | `DD AD` XOR IXl |
`DD B4` OR IXh | `DD B5` OR IXl |
`DD BC` CP IXh | `DD BD` CP IXl

**NOP-equivalent (correctly NOP):**
`DD 64` LD IXh,IXh | `DD 6D` LD IXl,IXl | `DD 76` (undefined)

### Implementation Plan

**Krok 1:** Add INC IX (DD 23) and DEC IX (DD 2B) — CRITICAL
- INC IX: `IX++`, 10 cycles, **NO flags affected** (16-bit INC/DEC on Z80 don't affect flags)
- DEC IX: `IX--`, 10 cycles, **NO flags affected**
- Also for IY: FD 23, FD 2B

**Krok 2:** Add missing documented DD opcodes
- DD E9: JP (IX) — jump to address in IX, 8 cycles
- DD F9: LD SP,IX — load SP from IX, 10 cycles
- Also for IY: FD E9, FD F9

**Krok 3:** Add LD r,IXh/IXl + LD IXh/IXl,r (26 opcodes)
- Helper: `GetIndexHigh(isIX, cpu)` → returns high byte of IX/IY
- Helper: `GetIndexLow(isIX, cpu)` → returns low byte of IX/IY
- Helper: `SetIndexHigh(isIX, cpu, val)` → sets high byte
- Helper: `SetIndexLow(isIX, cpu, val)` → sets low byte
- Each opcode is a 1-liner in InitTable

**Krok 4:** Add ALU A,IXh/IXl (16 opcodes)
- Uses existing `AluHelper.AddA/SubA/AndA/XorA/OrA/CpA`
- Each opcode reads IXh or IXl and passes to ALU helper

**Krok 5:** Fix NopDdFd fallback (architectural fix)
- For DD-prefixed opcodes that don't involve H/L, execute the base opcode with correct prefix timing
- This prevents PC desync for immediates and silent state corruption
- Currently NopDdFd does nothing (4 cycles) — wrong for most opcodes

**Krok 6:** Test after each group, run zexall full suite

### Known Issue: NopDdFd Fallback
The current fallback `NopDdFd` (no-op, 4 cycles) is architecturally wrong. On real Z80:
- DD-prefixed opcodes that don't reference H/L execute the base instruction with +4 cycle penalty
- DD-prefixed opcodes with immediates (like DD 36) consume the immediate byte
- NopDdFd doesn't consume operand bytes → causes PC desync

This needs to be fixed for full zexall compatibility, but INC IX/DEC IX are the priority.

### Key Files
- `src/Z80/Instructions/DdFd/DDFdPrefixHandler.cs` — DD/FD prefix handler (main file)
- `src/Z80/Instructions/DdFd/NopDdFd.cs` — NOP handler for unmapped opcodes
- `tests/Z80.Tests/OpcodeTests/CounterCorruptionTest.cs` — diagnostic tests
- `tests/roms/zexall.com` — ZEXALL test binary
- `tests/roms/zexdoc.com` — ZEXDOC test binary (reference)

### Reference
- Z80 instruction set: https://www.nesdev.org/wiki/6502_instruction_set (6502, but same project)
- Z80 DD prefix: https://www.z80.info/z80undoc.htm
- ZEXALL source: https://github.com/superzazu/zex
