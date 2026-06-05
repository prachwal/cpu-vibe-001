# Architektura minimalnego procesora 8-bit

## ZASADA NADRZĘDNA — kompatybilność binarna 1:1 z MOS 6502

> **Wszystkie opcodes, tryby adresowania i zachowania muszą być identyczne
> z oryginalnym procesorem MOS 6502 (MOS Technology, 1975).**
>
> - Żadnych instrukcji, których nie ma w prawdziwym 6502
> - Żadnych modyfikacji zachowania istniejących instrukcji
> - Kod binarny wygenerowany przez nasz asembler MUSI działać na prawdziwym 6502
> - Kod binarny z prawdziwego 6502 MUSI działać na naszej implementacji
> - Punkt odniesienia: [https://www.nesdev.org/wiki/6502_instruction_set](https://www.nesdev.org/wiki/6502_instruction_set)

---

## Cel

Minimalna architektura procesora 8-bit w 100% kompatybilna z MOS 6502.
Implementacja w .NET 8 — symulacja, edukacja, pełna zgodność binarna.

---

## 1. Schemat blokowy

```
+----------------------------------------------------------------+
|                    CPU 6502 (kompatybilny)                     |
|                                                                |
|  +---------+                                                   |
|  |   PC    | Program Counter (16-bit)                          |
|  | (16-bit)|                                                   |
|  +---------+                                                   |
|       |                                                        |
|       v                                                        |
|  +----------+  +-----------+  +-----------+  +------------+    |
|  | Address  |  |  Memory   |  |  Control  |  | Instruction|    |
|  | Bus      |->| Bus (8b)  |<-|   Unit    |  | Register   |    |
|  | (16-bit) |  |           |  | (Decoder) |  | (IR)       |    |
|  +----------+  +-----------+  +-----------+  +------------+    |
|                         |                        |             |
|                         v                        v             |
|  +------+  +------+  +------+  +-------+  +---------+          |
|  |  A   |  |  X   |  |  Y   |  |  ALU  |  |   P     |          |
|  | (8b) |  | (8b) |  | (8b) |  | (8b)  |  | Flags   |          |
|  +------+  +------+  +------+  +-------+  | N,V,..,C|          |
|     ^                            |        +---------+          |
|     +---------- Data Bus <-------+                             |
|          |         |         |                                 |
|          v         v         v                                 |
|  +------------------------------------------+                  |
|  |              Memory (64 KB)              |                  |
|  +------------------------------------------+                  |
|  | 0x0000-0x00FF: Zero Page (szybki)        |                  |
|  | 0x0100-0x01FF: Stos (rośnie w dół)       |                  |
|  | 0x0200-0x7FFF: RAM                       |                  |
|  | 0x8000-0xFFFF: ROM / Program             |                  |
|  +------------------------------------------+                  |
+----------------------------------------------------------------+
```

---

## 2. Rejestry

Dokładnie jak oryginalny MOS 6502.

| Rejestr | Rozmiar | Opis |
|---------|---------|------|
| `A` | 8-bit | **Akumulator** — wyniki ALU, dane operacyjne |
| `X` | 8-bit | **Rejestr indeksowy X** — indeksy, pętle, adresowanie |
| `Y` | 8-bit | **Rejestr indeksowy Y** — indeksy, pętle |
| `PC` | 16-bit | **Licznik programu** — adres następnej instrukcji |
| `SP` | 8-bit | **Wskaźnik stosu** — only low byte, high = 0x01xx |
| `P` | 8-bit | **Rejestr statusu (flagi)** |

### Rejestr statusu `P` — pełna struktura 6502

```
Bit:  7   6   5   4   3   2   1   0
      N   V   1   B   D   I   Z   C
      |   |   |   |   |   |   |   |
      |   |   |   |   |   |   |   +-- Carry
      |   |   |   |   |   |   +------ Zero
      |   |   |   |   |   +---------- Interrupt Disable
      |   |   |   |   +-------------- Decimal Mode (BCD)
      |   |   |   +------------------ Break Command
      |   |   +---------------------- (zawsze 1)
      |   +-------------------------- Overflow
      +------------------------------ Negative (bit 7 wyniku)
```

Po resecie: `P = 0x24` (I=1, bit 5=1, reszta=0).

---

## 3. ALU — Jednostka Arytmetyczno-Logiczna

Operacje wykonywane na rejestrze A (dokładnie jak 6502):

| Operacja | Opis | Flagi |
|----------|------|-------|
| `ADC` | A = A + operand + C (BCD lub binarny) | N, V, Z, C |
| `SBC` | A = A - operand - !C | N, V, Z, C |
| `AND` | A = A & operand | N, Z |
| `ORA` | A = A \| operand | N, Z |
| `EOR` | A = A ^ operand | N, Z |
| `ASL` | A = A << 1 (shift left, bit 0 = 0) | N, Z, C |
| `LSR` | A = A >> 1 (shift right, bit 7 = 0) | N, Z, C |
| `ROL` | A = rotate left through carry | N, Z, C |
| `ROR` | A = rotate right through carry | N, Z, C |
| `BIT` | testuj bity: N=bit7, V=bit6, Z=A&[addr] | N, V, Z |
| `INC` | [addr] = [addr] + 1 (tylko w pamięci!) | N, Z |
| `DEC` | [addr] = [addr] - 1 (tylko w pamięci!) | N, Z |
| `INX` | X = X + 1 | N, Z |
| `INY` | Y = Y + 1 | N, Z |
| `DEX` | X = X - 1 | N, Z |
| `DEY` | Y = Y - 1 | N, Z |
| `CMP` | Porównaj A z operandem (A - operand, tylko flagi) | N, Z, C |
| `CPX` | Porównaj X z operandem | N, Z, C |
| `CPY` | Porównaj Y z operandem | N, Z, C |

> **WAŻNE:** INC/DEC działają TYLKO w pamięci (Zero Page lub Absolute).
> Na prawdziwym 6502 NIE MA inkrementacji/dekrementacji akumulatora
> (opcodes 0x1A, 0x3A to undefined NOP).

---

## 4. Tryby adresowania — pełne 6502

| Tryb | Składnia | Rozmiar | Opis |
|------|----------|---------|------|
| **Implied** | `NOP` | 1 B | Brak operandu |
| **Accumulator** | `ASL A` | 1 B | Operacja na rejestrze A |
| **Immediate** | `LDA #$42` | 2 B | Bajt = operand |
| **Zero Page** | `LDA $10` | 2 B | Adres 0x00-0xFF |
| **Zero Page,X** | `LDA $10,X` | 2 B | (addr + X) & 0xFF |
| **Zero Page,Y** | `LDX $10,Y` | 2 B | (addr + Y) & 0xFF |
| **Absolute** | `LDA $1000` | 3 B | Pełny adres 16-bit |
| **Absolute,X** | `LDA $1000,X` | 3 B | addr + X (z page crossing) |
| **Absolute,Y** | `LDA $1000,Y` | 3 B | addr + Y |
| **Indirect** | `JMP ($FFFC)` | 3 B | Wskaźnik (tylko JMP) |
| **Indexed Indirect** | `LDA ($10,X)` | 2 B | [(zp+X)], [(zp+X)+1] → addr |
| **Indirect Indexed** | `LDA ($10),Y` | 2 B | [(zp)], [(zp)+1] + Y → addr |
| **Relative** | `BEQ label` | 2 B | PC + offset (-128..+127) |

---

## 5. Kompletny zestaw instrukcji — 151 opcodes

> Wszystkie opcodes odpowiadają oryginalnemu MOS 6502.
> Brak = undefined (traktowane jako NOP z niezdefiniowanym zachowaniem).

### 5.1 Ładowanie i zapis (Load/Store)

| Instrukcja | Opcode | Tryb | Opis |
|------------|--------|------|------|
| `LDA #imm` | `0xA9` | Imm | A = imm |
| `LDA zp` | `0xA5` | ZeroPage | A = [zp] |
| `LDA zp,X` | `0xB5` | ZP,X | A = [(zp+X) & 0xFF] |
| `LDA abs` | `0xAD` | Abs | A = [addr] |
| `LDA abs,X` | `0xBD` | Abs,X | A = [addr + X] |
| `LDA abs,Y` | `0xB9` | Abs,Y | A = [addr + Y] |
| `LDA (zp,X)` | `0xA1` | Ind,X | A = [ptr + X] |
| `LDA (zp),Y` | `0xB1` | Ind,Y | A = [ptr] + Y |
| `LDX #imm` | `0xA2` | Imm | X = imm |
| `LDX zp` | `0xA6` | ZeroPage | X = [zp] |
| `LDX zp,Y` | `0xB6` | ZP,Y | X = [(zp+Y) & 0xFF] |
| `LDX abs` | `0xAE` | Abs | X = [addr] |
| `LDX abs,Y` | `0xBE` | Abs,Y | X = [addr + Y] |
| `LDY #imm` | `0xA0` | Imm | Y = imm |
| `LDY zp` | `0xA4` | ZeroPage | Y = [zp] |
| `LDY zp,X` | `0xB4` | ZP,X | Y = [(zp+X) & 0xFF] |
| `LDY abs` | `0xAC` | Abs | Y = [addr] |
| `LDY abs,X` | `0xBC` | Abs,X | Y = [addr + X] |
| `STA zp` | `0x85` | ZeroPage | [zp] = A |
| `STA zp,X` | `0x95` | ZP,X | [(zp+X) & 0xFF] = A |
| `STA abs` | `0x8D` | Abs | [addr] = A |
| `STA abs,X` | `0x9D` | Abs,X | [addr + X] = A |
| `STA abs,Y` | `0x99` | Abs,Y | [addr + Y] = A |
| `STA (zp,X)` | `0x81` | Ind,X | [ptr + X] = A |
| `STA (zp),Y` | `0x91` | Ind,Y | [ptr] + Y = A |
| `STX zp` | `0x86` | ZeroPage | [zp] = X |
| `STX zp,Y` | `0x96` | ZP,Y | [(zp+Y) & 0xFF] = X |
| `STX abs` | `0x8E` | Abs | [addr] = X |
| `STY zp` | `0x84` | ZeroPage | [zp] = Y |
| `STY zp,X` | `0x94` | ZP,X | [(zp+X) & 0xFF] = Y |
| `STY abs` | `0x8C` | Abs | [addr] = Y |

### 5.2 Transfer między rejestrami

| Instrukcja | Opcode | Opis |
|------------|--------|------|
| `TAX` | `0xAA` | X = A |
| `TAY` | `0xA8` | Y = A |
| `TXA` | `0x8A` | A = X |
| `TYA` | `0x98` | A = Y |
| `TSX` | `0xBA` | X = SP |
| `TXS` | `0x9A` | SP = X |

### 5.3 Arytmetyka

| Instrukcja | Opcode | Tryb | Opis |
|------------|--------|------|------|
| `ADC #imm` | `0x69` | Imm | A = A + imm + C |
| `ADC zp` | `0x65` | ZP | A = A + [zp] + C |
| `ADC zp,X` | `0x75` | ZP,X | A = A + [(zp+X)] + C |
| `ADC abs` | `0x6D` | Abs | A = A + [addr] + C |
| `ADC abs,X` | `0x7D` | Abs,X | A = A + [addr+X] + C |
| `ADC abs,Y` | `0x79` | Abs,Y | A = A + [addr+Y] + C |
| `ADC (zp,X)` | `0x61` | Ind,X | A = A + [ptr+X] + C |
| `ADC (zp),Y` | `0x71` | Ind,Y | A = A + [ptr]+Y + C |
| `SBC #imm` | `0xE9` | Imm | A = A - imm - !C |
| `SBC zp` | `0xE5` | ZP | A = A - [zp] - !C |
| `SBC zp,X` | `0xF5` | ZP,X | A = A - [(zp+X)] - !C |
| `SBC abs` | `0xED` | Abs | A = A - [addr] - !C |
| `SBC abs,X` | `0xFD` | Abs,X | A = A - [addr+X] - !C |
| `SBC abs,Y` | `0xF9` | Abs,Y | A = A - [addr+Y] - !C |
| `SBC (zp,X)` | `0xE1` | Ind,X | A = A - [ptr+X] - !C |
| `SBC (zp),Y` | `0xF1` | Ind,Y | A = A - [ptr]+Y - !C |

### 5.4 Inkrementacja / Dekrementacja

| Instrukcja | Opcode | Tryb | Opis |
|------------|--------|------|------|
| `INC zp` | `0xE6` | ZP | [zp] = [zp] + 1 |
| `INC zp,X` | `0xF6` | ZP,X | [(zp+X)] = [(zp+X)] + 1 |
| `INC abs` | `0xEE` | Abs | [addr] = [addr] + 1 |
| `INC abs,X` | `0xFE` | Abs,X | [addr+X] = [addr+X] + 1 |
| `DEC zp` | `0xC6` | ZP | [zp] = [zp] - 1 |
| `DEC zp,X` | `0xD6` | ZP,X | [(zp+X)] = [(zp+X)] - 1 |
| `DEC abs` | `0xCE` | Abs | [addr] = [addr] - 1 |
| `DEC abs,X` | `0xDE` | Abs,X | [addr+X] = [addr+X] - 1 |
| `INX` | `0xE8` | Impl | X = X + 1 |
| `INY` | `0xC8` | Impl | Y = Y + 1 |
| `DEX` | `0xCA` | Impl | X = X - 1 |
| `DEY` | `0x88` | Impl | Y = Y - 1 |

### 5.5 Operacje logiczne

| Instrukcja | Opcode | Tryb | Opis |
|------------|--------|------|------|
| `AND #imm` | `0x29` | Imm | A = A & imm |
| `AND zp` | `0x25` | ZP | A = A & [zp] |
| `AND zp,X` | `0x35` | ZP,X | A = A & [(zp+X)] |
| `AND abs` | `0x2D` | Abs | A = A & [addr] |
| `AND abs,X` | `0x3D` | Abs,X | A = A & [addr+X] |
| `AND abs,Y` | `0x39` | Abs,Y | A = A & [addr+Y] |
| `AND (zp,X)` | `0x21` | Ind,X | A = A & [ptr+X] |
| `AND (zp),Y` | `0x31` | Ind,Y | A = A & [ptr]+Y |
| `ORA #imm` | `0x09` | Imm | A = A \| imm |
| `ORA zp` | `0x05` | ZP | A = A \| [zp] |
| `ORA zp,X` | `0x15` | ZP,X | A = A \| [(zp+X)] |
| `ORA abs` | `0x0D` | Abs | A = A \| [addr] |
| `ORA abs,X` | `0x1D` | Abs,X | A = A \| [addr+X] |
| `ORA abs,Y` | `0x19` | Abs,Y | A = A \| [addr+Y] |
| `ORA (zp,X)` | `0x01` | Ind,X | A = A \| [ptr+X] |
| `ORA (zp),Y` | `0x11` | Ind,Y | A = A \| [ptr]+Y |
| `EOR #imm` | `0x49` | Imm | A = A ^ imm |
| `EOR zp` | `0x45` | ZP | A = A ^ [zp] |
| `EOR zp,X` | `0x55` | ZP,X | A = A ^ [(zp+X)] |
| `EOR abs` | `0x4D` | Abs | A = A ^ [addr] |
| `EOR abs,X` | `0x5D` | Abs,X | A = A ^ [addr+X] |
| `EOR abs,Y` | `0x59` | Abs,Y | A = A ^ [addr+Y] |
| `EOR (zp,X)` | `0x41` | Ind,X | A = A ^ [ptr+X] |
| `EOR (zp),Y` | `0x51` | Ind,Y | A = A ^ [ptr]+Y |
| `BIT zp` | `0x24` | ZP | testuj bity A & [zp] |
| `BIT abs` | `0x2C` | Abs | testuj bity A & [addr] |

### 5.6 Przesunięcia i rotacje

| Instrukcja | Opcode | Tryb | Opis |
|------------|--------|------|------|
| `ASL A` | `0x0A` | Acc | A = A << 1, bit 0 = 0 |
| `ASL zp` | `0x06` | ZP | [zp] = [zp] << 1 |
| `ASL zp,X` | `0x16` | ZP,X | [(zp+X)] <<= 1 |
| `ASL abs` | `0x0E` | Abs | [addr] <<= 1 |
| `ASL abs,X` | `0x1E` | Abs,X | [addr+X] <<= 1 |
| `LSR A` | `0x4A` | Acc | A = A >> 1, bit 7 = 0 |
| `LSR zp` | `0x46` | ZP | [zp] >>= 1 |
| `LSR zp,X` | `0x56` | ZP,X | [(zp+X)] >>= 1 |
| `LSR abs` | `0x4E` | Abs | [addr] >>= 1 |
| `LSR abs,X` | `0x5E` | Abs,X | [addr+X] >>= 1 |
| `ROL A` | `0x2A` | Acc | rotate left through carry |
| `ROL zp` | `0x26` | ZP | [zp] <<= 1, bit 0 = old C |
| `ROL zp,X` | `0x36` | ZP,X | [(zp+X)] <<= 1, bit 0 = old C |
| `ROL abs` | `0x2E` | Abs | [addr] <<= 1, bit 0 = old C |
| `ROL abs,X` | `0x3E` | Abs,X | [addr+X] <<= 1, bit 0 = old C |
| `ROR A` | `0x6A` | Acc | rotate right through carry |
| `ROR zp` | `0x66` | ZP | [zp] >>= 1, bit 7 = old C |
| `ROR zp,X` | `0x76` | ZP,X | [(zp+X)] >>= 1, bit 7 = old C |
| `ROR abs` | `0x6E` | Abs | [addr] >>= 1, bit 7 = old C |
| `ROR abs,X` | `0x7E` | Abs,X | [addr+X] >>= 1, bit 7 = old C |

### 5.7 Porównania

| Instrukcja | Opcode | Tryb | Opis |
|------------|--------|------|------|
| `CMP #imm` | `0xC9` | Imm | A - imm (tylko flagi) |
| `CMP zp` | `0xC5` | ZP | A - [zp] |
| `CMP zp,X` | `0xD5` | ZP,X | A - [(zp+X)] |
| `CMP abs` | `0xCD` | Abs | A - [addr] |
| `CMP abs,X` | `0xDD` | Abs,X | A - [addr+X] |
| `CMP abs,Y` | `0xD9` | Abs,Y | A - [addr+Y] |
| `CMP (zp,X)` | `0xC1` | Ind,X | A - [ptr+X] |
| `CMP (zp),Y` | `0xD1` | Ind,Y | A - [ptr]+Y |
| `CPX #imm` | `0xE0` | Imm | X - imm |
| `CPX zp` | `0xE4` | ZP | X - [zp] |
| `CPX abs` | `0xEC` | Abs | X - [addr] |
| `CPY #imm` | `0xC0` | Imm | Y - imm |
| `CPY zp` | `0xC4` | ZP | Y - [zp] |
| `CPY abs` | `0xCC` | Abs | Y - [addr] |

### 5.8 Skoki warunkowe (Branches)

Wszystkie relative: offset 1 bajt (signed -128..+127).

| Instrukcja | Opcode | Opis |
|------------|--------|------|
| `BCC rel` | `0x90` | if (C==0) PC += offset |
| `BCS rel` | `0xB0` | if (C==1) PC += offset |
| `BEQ rel` | `0xF0` | if (Z==1) PC += offset |
| `BNE rel` | `0xD0` | if (Z==0) PC += offset |
| `BMI rel` | `0x30` | if (N==1) PC += offset |
| `BPL rel` | `0x10` | if (N==0) PC += offset |
| `BVC rel` | `0x50` | if (V==0) PC += offset |
| `BVS rel` | `0x70` | if (V==1) PC += offset |

### 5.9 Skoki bezwarunkowe i podprogramy

| Instrukcja | Opcode | Tryb | Opis |
|------------|--------|------|------|
| `JMP abs` | `0x4C` | Abs | PC = addr |
| `JMP (abs)` | `0x6C` | Ind | PC = [addr] + [addr+1] (indirect) |
| `JSR abs` | `0x20` | Abs | push PC+2, PC = addr |
| `RTS` | `0x60` | Impl | PC = pop() + 1 |
| `BRK` | `0x00` | Impl | Push P+PC, jump to IRQ vector |
| `RTI` | `0x40` | Impl | Pop P+PC (zwraca z przerwania) |

### 5.10 Stos

| Instrukcja | Opcode | Opis |
|------------|--------|------|
| `PHA` | `0x48` | Push A na stos |
| `PLA` | `0x68` | Pop A ze stosu |
| `PHP` | `0x08` | Push P na stos |
| `PLP` | `0x28` | Pop P ze stosu |

### 5.11 Flagi / System

| Instrukcja | Opcode | Opis |
|------------|--------|------|
| `NOP` | `0xEA` | Brak operacji |
| `SEC` | `0x38` | C = 1 |
| `CLC` | `0x18` | C = 0 |
| `SEI` | `0x78` | I = 1 |
| `CLI` | `0x58` | I = 0 |
| `SED` | `0xF8` | D = 1 (BCD) |
| `CLD` | `0xD8` | D = 0 |
| `CLV` | `0xB8` | V = 0 |

---

## 6. Zbiór opcode — pełna tabela 256 bajtów

```
       00    01    02    03    04    05    06    07    08    09    0A    0B    0C    0D    0E    0F
    +-----------------------------------------------------------------------------------------------
00  | BRK   ORA   ---   ---   ---   ORA   ASL   ---   PHP   ORA   ASL   ---   ---   ORA   ASL   ---
10  | BPL   ORA   ---   ---   ---   ORA   ASL   ---   CLC   ORA   ---   ---   ---   ORA   ASL   ---
20  | JSR   AND   ---   ---   BIT   AND   ROL   ---   PLP   AND   ROL   ---   BIT   AND   ROL   ---
30  | BMI   AND   ---   ---   ---   AND   ROL   ---   SEC   AND   ---   ---   ---   AND   ROL   ---
40  | RTI   EOR   ---   ---   ---   EOR   LSR   ---   PHA   EOR   LSR   ---   JMP   EOR   LSR   ---
50  | BVC   EOR   ---   ---   ---   EOR   LSR   ---   CLI   EOR   ---   ---   ---   EOR   LSR   ---
60  | RTS   ADC   ---   ---   ---   ADC   ROR   ---   PLA   ADC   ROR   ---   JMP   ADC   ROR   ---
70  | BVS   ADC   ---   ---   ---   ADC   ROR   ---   SEI   ADC   ---   ---   ---   ADC   ROR   ---
80  | ---   STA   ---   ---   STY   STA   STX   ---   DEY   ---   TXA   ---   STY   STA   STX   ---
90  | BCC   STA   ---   ---   STY   STA   STX   ---   TYA   TXS   ---   ---   STY   STA   STX   ---
A0  | LDY   LDX   ---   ---   LDY   LDX   LDY   ---   TAY   LAX   TAX   ---   LDY   LDX   LDY   ---
B0  | BCS   LAX   ---   ---   LDY   LAX   LDY   ---   CLV   LAX   TSX   ---   LDY   LAX   LDY   ---
C0  | CPY   CPX   ---   ---   CPY   CPX   DEC   ---   INY   CMP   DEX   ---   CPY   CPX   DEC   ---
D0  | BNE   CMP   ---   ---   ---   CMP   DEC   ---   CLD   CMP   ---   ---   ---   CMP   DEC   ---
E0  | CPX   SBC   ---   ---   CPX   SBC   INC   ---   INX   SBC   NOP   ---   CPX   SBC   INC   ---
F0  | BEQ   SBC   ---   ---   ---   SBC   INC   ---   SED   SBC   ---   ---   ---   SBC   INC   ---
```

> `---` = undefined opcode (zachowanie niezdefiniowane na prawdziwym 6502)

---

## 7. Model pamięci (64 KB)

```
+----------------------------------------------+
| Adres       | Zawartość                      |
|-------------|--------------------------------|
| $0000-$00FF | Zero Page (szybki dostęp)      |
| $0100-$01FF | Stos (rośnie w dół od $01FF)   |
| $0200-$7FFF | RAM                            |
| $8000-$BFFF | ROM (program)                  |
| $C000-$FFFB | ROM / wolne                    |
| $FFFC-$FFFD | Wektor RESET (adres startu)    |
| $FFFE-$FFFF | Wektor IRQ/BRK                 |
+----------------------------------------------+
```

Po resecie:
- PC = [*$FFFC] + [*$FFFD] (little-endian)
- SP = $FD
- P = $24 (I=1, unused bit=1)

---

## 8. Cykl instrukcji (Fetch-Decode-Execute)

```
1. FETCH
   - Odczytaj bajt z [PC]
   - Zapisz w IR (Instruction Register)
   - PC++

2. DECODE
   - Rozpoznaj tryb adresowania z opcode
   - Odczytaj operand jeśli wymagany:
     • Immediate: PC++
     • Zero Page: PC++ (adres 8-bit)
     • Absolute: PC += 2 (adres 16-bit little-endian)
     • Relative: PC++ (offset signed 8-bit)
     • Implied/Accumulator: brak operandu

3. EXECUTE
   - Wykonaj operację
   - Aktualizuj flagi (jeśli ALU)
   - Zapisz wynik (rejestr / pamięć)
   - Dodatkowe cykle:
     • Page crossing: +1 cykl (abs,X / abs,Y / ind,Y)
     • Branch taken: +1 cykl (+2 jeśli page crossing)

4. Powrót do kroku 1 (jeśli nie BRK/RTI/RTS)
```

---

## 9. Przykład programu — Fibonacci do 255

```asm
; Fibonacci: 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233
; Wynik zapisywany w Zero Page: $02, $03, ... $0E
;
; $00 = fib(n-2)
; $01 = fib(n-1)
;
; Asm           | Hex       | Opis
; --------------|-----------|---------------------------
    LDA #$01    | A9 01     | A = 1
    STA $00     | 85 00     | $00 = 1 (fib[n-2])
    STA $01     | 85 01     | $01 = 1 (fib[n-1])
    LDX #$00    | A2 00     | X = 0 (indeks zapisu)
LOOP:
    LDA $00     | A5 00     | A = fib[n-2]
    CLC         | 18        | C = 0
    ADC $01     | 65 01     | A = fib[n-2] + fib[n-1]
    BCS DONE    | B0 xx     | jeśli C=1 (>255), koniec
    STA $02,X   | 95 02     | zapisz wynik pod $02+X
    INX         | E8        | X++
    LDY $01     | A4 01     | Y = stare fib[n-1]
    STY $00     | 84 00     | fib[n-2] = stare fib[n-1]
    STA $01     | 85 01     | fib[n-1] = wynik (A)
    JMP LOOP    | 4C xx xx  | skocz na początek pętli
DONE:
    BRK         | 00        | STOP

; Wynik w pamięci:
; $02=$01  $03=$01  $04=$02  $05=$03  $06=$05
; $07=$08  $08=$0D  $09=$15  $0A=$22  $0B=$37
; $0C=$59  $0D=$90  $0E=$E9
;
; Sekwencja: 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233
```

---

## 10. Implementacja .NET 8 — architektura "jedna instrukcja = jeden plik"

### 10.1 Założenia

- **Jedna instrukcja = jeden plik** — pełna izolacja, łatwy testing
- **Lookup table** — 256-elementowa tablica `InstructionHandler[]`, dekodowanie O(1)
- **Delegate `Action<Cpu>`** — każda instrukcja to statyczna metoda `Execute(Cpu cpu)`
- **Zero abstraction** — brak interfejsów, abstrakcji, refleksji — tylko plain static methods
- **151 plików** z instrukcjami + 1 plik z tabelą = **152 pliki**

### 10.2 Struktura projektu

```
cpu-vibe-001/
├── src/
│   └── Mos6502/
│       ├── Mos6502.csproj
│       ├── Program.cs
│       ├── Core/
│       │   ├── Cpu.cs              ← Główna pętla (fetch-decode-execute)
│       │   ├── Alu.cs              ← Logika ADC, SBC, shifts (wywoływana z instrukcji)
│       │   ├── Registers.cs        ← A, X, Y, SP, PC (16-bit), P
│       │   ├── Memory.cs           ← 64 KB RAM
│       │   └── Flags.cs            ← [Flags] enum: N, V, -, B, D, I, Z, C
│       ├── Instructions/
│       │   ├── InstructionHandler.cs  ← delegate void InstructionHandler(Cpu cpu)
│       │   ├── InstructionTable.cs    ← static InstructionHandler[256]
│       │   ├── Adc/
│       │   │   ├── AdcImmediate.cs    ← $69
│       │   │   ├── AdcZeroPage.cs     ← $65
│       │   │   ├── AdcZeroPageX.cs    ← $75
│       │   │   ├── AdcAbsolute.cs     ← $6D
│       │   │   ├── AdcAbsoluteX.cs    ← $7D
│       │   │   ├── AdcAbsoluteY.cs    ← $79
│       │   │   ├── AdcIndirectX.cs    ← $61
│       │   │   └── AdcIndirectY.cs    ← $71
│       │   ├── And/
│       │   │   ├── AndImmediate.cs    ← $29
│       │   │   └── ... (8 plików)
│       │   ├── Asl/  (5 plików)
│       │   ├── Bcc/  (1 plik)
│       │   ├── Bcs/  (1 plik)
│       │   ├── Beq/  (1 plik)
│       │   ├── Bit/  (2 pliki)
│       │   ├── Bmi/  (1 plik)
│       │   ├── Bne/  (1 plik)
│       │   ├── Bpl/  (1 plik)
│       │   ├── Brk/  (1 plik)
│       │   ├── Bvc/  (1 plik)
│       │   ├── Bvs/  (1 plik)
│       │   ├── Clc/  (1 plik)
│       │   ├── Cld/  (1 plik)
│       │   ├── Cli/  (1 plik)
│       │   ├── Clv/  (1 plik)
│       │   ├── Cmp/  (8 plików)
│       │   ├── Cpx/  (3 pliki)
│       │   ├── Cpy/  (3 pliki)
│       │   ├── Dec/  (4 pliki)
│       │   ├── Dex/  (1 plik)
│       │   ├── Dey/  (1 plik)
│       │   ├── Eor/  (8 plików)
│       │   ├── Inc/  (4 pliki)
│       │   ├── Inx/  (1 plik)
│       │   ├── Iny/  (1 plik)
│       │   ├── Jmp/  (2 pliki)
│       │   ├── Jsr/  (1 plik)
│       │   ├── Lda/  (8 plików)
│       │   ├── Ldx/  (5 plików)
│       │   ├── Ldy/  (5 plików)
│       │   ├── Lsr/  (5 plików)
│       │   ├── Nop/  (1 plik)
│       │   ├── Ora/  (8 plików)
│       │   ├── Pha/  (1 plik)
│       │   ├── Php/  (1 plik)
│       │   ├── Pla/  (1 plik)
│       │   ├── Plp/  (1 plik)
│       │   ├── Rol/  (5 plików)
│       │   ├── Ror/  (5 plików)
│       │   ├── Rti/  (1 plik)
│       │   ├── Rts/  (1 plik)
│       │   ├── Sbc/  (8 plików)
│       │   ├── Sec/  (1 plik)
│       │   ├── Sed/  (1 plik)
│       │   ├── Sei/  (1 plik)
│       │   ├── Sta/  (7 plików)
│       │   ├── Stx/  (3 pliki)
│       │   ├── Sty/  (3 pliki)
│       │   ├── Tax/  (1 plik)
│       │   ├── Tay/  (1 plik)
│       │   ├── Tsx/  (1 plik)
│       │   ├── Txa/  (1 plik)
│       │   ├── Txs/  (1 plik)
│       │   └── Tya/  (1 plik)
│       └── Assembler/
│           ├── Assembler.cs
│           └── Lexer.cs
├── tests/
│   └── Mos6502.Tests/
│       ├── CpuTests.cs
│       ├── AluTests.cs
│       ├── InstructionTests.cs
│       └── AssemblerTests.cs
└── docs/
    └── architecture.md
```

### 10.3 Delegate i Lookup Table

```csharp
// InstructionHandler.cs
namespace Mos6502.Instructions;

public delegate void InstructionHandler(Cpu cpu);
```

```csharp
// InstructionTable.cs — 256 wpisów, O(1) decode
namespace Mos6502.Instructions;

public static class InstructionTable
{
    public static readonly InstructionHandler[] Handlers = new InstructionHandler[256];

    static InstructionTable()
    {
        // Default: undefined = NOP
        for (int i = 0; i < 256; i++)
            Handlers[i] = Nop.Nop.Execute;

        // ADC
        Handlers[0x69] = AdcImmediate.Execute;
        Handlers[0x65] = AdcZeroPage.Execute;
        // ... 151 wpisów
    }
}
```

### 10.4 Przykład: jedna instrukcja = jeden plik

Każdy plik zawiera **dokumentację XML** ( opcode, rozmiar, cykle, flagi ) i **statyczną metodę Execute**.

```csharp
// Lda/LdaImmediate.cs — LDA #imm  (opcode $A9, 2B, 2 cykle)
namespace Mos6502.Instructions.Lda;

public static class LdaImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte value = cpu.Memory.Read(cpu.Regs.PC++);
        cpu.Regs.A = value;
        cpu.Regs.SetNZ(value);
    }
}
```

```csharp
// Adc/AdcImmediate.cs — ADC #imm  (opcode $69, 2B, 2 cykle)
namespace Mos6502.Instructions.Adc;

public static class AdcImmediate
{
    public static void Execute(Cpu cpu)
    {
        byte operand = cpu.Memory.Read(cpu.Regs.PC++);
        byte a = cpu.Regs.A;
        byte carry = cpu.Regs.IsCarry ? (byte)1 : (byte)0;

        ushort result = (ushort)(a + operand + carry);

        cpu.Regs.SetFlag(CpuFlags.Zero, (byte)(result & 0xFF) == 0);
        cpu.Regs.SetFlag(CpuFlags.Negative, (result & 0x80) != 0);
        cpu.Regs.SetFlag(CpuFlags.Carry, result > 0xFF);
        cpu.Regs.SetFlag(CpuFlags.Overflow,
            (~(a ^ operand) & (a ^ result) & 0x80) != 0);

        cpu.Regs.A = (byte)(result & 0xFF);
    }
}
```

```csharp
// Bne/BneRelative.cs — BNE rel  (opcode $D0, 2B, 2-4 cykle)
namespace Mos6502.Instructions.Bne;

public static class BneRelative
{
    public static void Execute(Cpu cpu)
    {
        sbyte offset = (sbyte)cpu.Memory.Read(cpu.Regs.PC++);

        if (!cpu.Regs.IsZero)
        {
            ushort oldPc = cpu.Regs.PC;
            cpu.Regs.PC = (ushort)(cpu.Regs.PC + offset);
            cpu.Cycles++;

            if ((oldPc & 0xFF00) != (cpu.Regs.PC & 0xFF00))
                cpu.Cycles += 2;
        }
    }
}
```

```csharp
// Nop/Nop.cs — NOP  (opcode $EA, 1B, 2 cykle)
namespace Mos6502.Instructions.Nop;

public static class Nop
{
    public static void Execute(Cpu cpu) { /* nic */ }
}
```

### 10.5 Główna pętla CPU

```csharp
public class Cpu
{
    public Registers Regs { get; } = new();
    public Memory Memory { get; } = new();
    public byte Cycles { get; set; }
    private bool _running;

    public void Reset()
    {
        Regs.Reset();
        Cycles = 0;
        _running = true;

        byte lo = Memory.Read(0xFFFC);
        byte hi = Memory.Read(0xFFFD);
        Regs.PC = (ushort)((hi << 8) | lo);
    }

    public ushort ReadAddress()
    {
        byte lo = Memory.Read(Regs.PC++);
        byte hi = Memory.Read(Regs.PC++);
        return (ushort)((hi << 8) | lo);
    }

    public void StackPush(byte value)
    {
        Memory.Write((ushort)(0x0100 + Regs.SP), value);
        Regs.SP--;
    }

    public byte StackPop()
    {
        Regs.SP++;
        return Memory.Read((ushort)(0x0100 + Regs.SP));
    }

    public void Step()
    {
        byte opcode = Memory.Read(Regs.PC++);
        InstructionTable.Handlers[opcode](this);
    }

    public void Run()
    {
        Reset();
        while (_running)
            Step();
    }
}
```

### 10.6 Flags helper

```csharp
public class Registers
{
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte SP { get; set; }
    public ushort PC { get; set; }
    public CpuFlags P { get; set; }

    public bool IsZero => P.HasFlag(CpuFlags.Zero);
    public bool IsCarry => P.HasFlag(CpuFlags.Carry);
    public bool IsNegative => P.HasFlag(CpuFlags.Negative);
    public bool IsOverflow => P.HasFlag(CpuFlags.Overflow);

    public void Reset()
    {
        A = 0; X = 0; Y = 0;
        SP = 0xFD;
        PC = 0x0000;
        P = CpuFlags.Unused | CpuFlags.Interrupt;
    }

    public void SetNZ(byte value)
    {
        SetFlag(CpuFlags.Zero, value == 0);
        SetFlag(CpuFlags.Negative, (value & 0x80) != 0);
    }

    public void SetFlag(CpuFlags flag, bool set)
    {
        if (set) P |= flag;
        else P &= ~flag;
    }
}
```

### 10.7 Zalety patternu

| Aspekt | Korzyść |
|--------|---------|
| **Izolacja** | Każda instrukcja w osobnym pliku — easy test, refactor |
| **Compile-time check** | Brak refleksji, brak stringów — błąd = błąd kompilacji |
| **Lookup table** | `Handlers[opcode](cpu)` — O(1), branchless, cache-friendly |
| **Brak dziedziczenia** | Tylko static methods — zero allocation, zero virtual call |
| **Dokumentacja** | XML comment w każdym pliku: opcode, size, cycles, flags |
| **151 plików** | Każdy plik ~15-30 linii — łatwy do zrozumienia |

### 10.8 Porównanie z alternatywami

| Pattern | Dekodowanie | Allocation | Readability | Rozmiar |
|---------|------------|------------|-------------|---------|
| **Static method + table** | O(1) | zero | wysoka | 151 plików |
| Switch statement | O(1) | zero | średnia | 1 plik |
| Dictionary<byte,Delegate> | O(1) | alloc | niska | 1 plik |
| Reflection | O(n) | alloc | niska | 1 plik |
| Interface + classes | O(1) | alloc | średnia | 151 plików |

---

## 11. Ważne uwagi implementacyjne

1. **BCD mode (D flag)** — oryginalny 6502 wspiera BCD (Decimal), ale NMOS 6502 ma bugi w BCD. W implementacji możemy pominąć lub poprawić.
2. **Page crossing** — instrukcje abs,X / abs,Y / ind,Y dodają +1 cykl przy przejściu stroną.
3. **Branch penalties** — branch taken = +1 cykl, branch taken + page crossing = +2 cykle.
4. **Stack** — rośnie w dół od $01FF do $0100. Push: [SP] = value, SP--. Pop: SP++, value = [SP].
5. **NMI** — niemaskowalne przerwanie (wektor $FFFA-$FFFB).
6. **NOP** — nie wszystkie undefined opcodes to NOP — niektóre mają dziwne zachowanie.

---

## 12. Rozszerzenia (przyszłe)

- **IRQ / NMI** — przerwania maskowalne i niemaskowalne
- **DMA** — bezpośredni dostęp do pamięci
- **Display** — framebuffer 16x16 px
- **Debugger** — krokowy, breakpoint, watchpoint
- **Asembler** — pełny 6502 asm → bajty
- **Test suite** — testy kompatybilności z prawdziwym 6502 (Klaus Dormann test)
