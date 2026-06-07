# CPU Variants — Plan implementacji

## Warianty

### 1. Ricoh 2A03 (NES) — ZAIMPLEMENTOWANY ✅
- **Koszt:** 0 (zerowy)
- **Różnice vs 6502:** BCD wyłączony (ADC/SBC ignorują flagę D)
- **Status:** Nasz 6502 już to spełnia — ADC/SBC nie sprawdzają `CpuFlags.Decimal`
- **Akcja:** CpuVariant enum + przekazanie do Cpu ✅

### 2. WDC 65C02 — ZAIMPLEMENTOWANY ✅
- **Koszt:** niski (16 nowych opcode, prosta logika)
- **Nowe instrukcje:**
  - `BRA` (0x80) — Branch Always ✅
  - `PHX` (0xDA) — Push X ✅
  - `PLX` (0xFA) — Pull X ✅
  - `PHY` (0x5A) — Push Y ✅
  - `PLY` (0x7A) — Pull Y ✅
  - `STZ` (0x64/0x74/0x9C/0x9E) — Store Zero ✅
  - `TRB` (0x14/0x1C) — Test and Reset Bits ✅
  - `TSB` (0x04/0x0C) — Test and Set Bits ✅
  - `BIT #imm` (0x89) — Immediate mode ✅
  - `BIT zp,X` (0x34) — Zero Page,X ✅
  - `BIT abs,X` (0x3C) — Absolute,X ✅
  - `JMP (abs,X)` (0x7C) — Indirect,X ✅
  - `INC A` (0x1A) — Accumulator ✅
  - `DEC A` (0x3A) — Accumulator ✅
- **Poprawki vs 6502:**
  - `JMP (abs)` — bug fix zaimplementowany w InstructionTable ✅

### 3. WDC 65C816 — NIEZAIMPLEMENTOWANY
- **Koszt:** wysoki (dużo nowych rejestów, trybów adresowania, 16-bit)
- **Różnice:**
  - 16-bitowy akumulator i rejestry indeksowe
  - 24-bitowe adresowanie (banki)
  - Nowe rejestry: DBR, PBR, Direct Page
  - Nowe instrukcje: MVP, MVN, PEA, PER, PHB, PHD, PHK, PLB, PLD, RTL, STP, WAI, XBA, XCE
  - Rozszerzone addressing modes: (dp), (dp,S), [dp], al, axl
  - Emulation mode (6502 compatible) vs Native mode

## Implementacja

```
src/Mos6502/
├── Core/
│   ├── CpuVariant.cs           ← enum: Mos6502, Wdc65C02, Ricoh2A03, Wdc65C816 ✅
│   ├── Cpu.cs                  ← konstruktor(CpuVariant), Table property ✅
├── Instructions/
│   ├── InstructionTable.cs     ← klasa, nie static; Create(CpuVariant) ✅
│   ├── Bra/BraRelative.cs      ← 65C02: BRA $offset ✅
│   ├── Phx/PhxImplied.cs       ← 65C02: PHX ✅
│   ├── Plx/PlxImplied.cs       ← 65C02: PLX ✅
│   ├── Phy/PhyImplied.cs       ← 65C02: PHY ✅
│   ├── Ply/PlyImplied.cs       ← 65C02: PLY ✅
│   ├── Stz/StzZeroPage.cs      ← 65C02: STZ zp ✅
│   ├── Stz/StzZeroPageX.cs     ← 65C02: STZ zp,X ✅
│   ├── Stz/StzAbsolute.cs      ← 65C02: STZ abs ✅
│   ├── Stz/StzAbsoluteX.cs     ← 65C02: STZ abs,X ✅
│   ├── Trb/TrbZeroPage.cs      ← 65C02: TRB zp ✅
│   ├── Trb/TrbAbsolute.cs      ← 65C02: TRB abs ✅
│   ├── Tsb/TsbZeroPage.cs      ← 65C02: TSB zp ✅
│   ├── Tsb/TsbAbsolute.cs      ← 65C02: TSB abs ✅
│   ├── Bit/BitImmediate.cs     ← 65C02: BIT #imm ✅
│   ├── Bit/BitZeroPageX.cs     ← 65C02: BIT zp,X ✅
│   ├── Bit/BitAbsoluteX.cs     ← 65C02: BIT abs,X ✅
│   ├── Inc/IncAccumulator.cs   ← 65C02: INC A ✅
│   ├── Dec/DecAccumulator.cs   ← 65C02: DEC A ✅
│   ├── Jmp/JmpIndirectX.cs     ← 65C02: JMP (abs,X) ✅
```

## Tabela opcode

| Opcode | Instrukcja | 6502 | 65C02 | 2A03 | Status |
|--------|-----------|------|-------|------|--------|
| $04    | TSB zp    | NOP  | TSB   | NOP  | ✅ |
| $0C    | TSB abs   | NOP  | TSB   | NOP  | ✅ |
| $14    | TRB zp    | NOP  | TRB   | NOP  | ✅ |
| $1A    | INC A     | NOP  | INC   | NOP  | ✅ |
| $1C    | TRB abs   | NOP  | TRB   | NOP  | ✅ |
| $34    | BIT zp,X  | NOP  | BIT   | NOP  | ✅ |
| $3A    | DEC A     | NOP  | DEC   | NOP  | ✅ |
| $3C    | BIT abs,X | NOP  | BIT   | NOP  | ✅ |
| $5A    | PHY       | NOP  | PHY   | NOP  | ✅ |
| $64    | STZ zp    | NOP  | STZ   | NOP  | ✅ |
| $74    | STZ zp,X  | NOP  | STZ   | NOP  | ✅ |
| $7A    | PLY       | NOP  | PLY   | NOP  | ✅ |
| $7C    | JMP (abs,X)| NOP | JMP   | NOP  | ✅ |
| $80    | BRA       | NOP  | BRA   | NOP  | ✅ |
| $89    | BIT #imm  | NOP  | BIT   | NOP  | ✅ |
| $9C    | STZ abs   | NOP  | STZ   | NOP  | ✅ |
| $9E    | STZ abs,X | NOP  | STZ   | NOP  | ✅ |
| $DA    | PHX       | NOP  | PHX   | NOP  | ✅ |
| $FA    | PLX       | NOP  | PLX   | NOP  | ✅ |
