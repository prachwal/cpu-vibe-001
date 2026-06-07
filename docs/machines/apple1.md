# Apple 1 Emulation

## Memory Map

```
$0000-$0FFF  RAM (4 KB)
$1000-$BFFF  expansion (unmapped → reads $00)
$D010-$D015  PIA 6520 (6 rejestrów)
$D0F2        BASIC DSP — alternatywny port wyjścia BASIC ROM
$E000-$EFFF  BASIC ROM (4 KB, Integer BASIC)
$FF00-$FFFF  Woz Monitor (256 B)
```

## I/O — PIA 6520

Bazowy adres: `$D010` (6 bajtów).

| Adres | Offset | Rejestr | Kierunek | Opis |
|-------|--------|---------|----------|------|
| $D010 | +0 | PRA / DDRA | R/W | Port A data / kierunek (CRA.2=0 → DDRA, =1 → PRA) |
| $D011 | +1 | CRA | R/W | Control register A (bity 6-7 read-only IRQ flags) |
| $D012 | +2 | PRB / DDRB | R/W | Port B data / kierunek (CRB.2=0 → DDRB, =1 → PRB) |
| $D013 | +3 | CRB | R/W | Control register B |

Domyślna konfiguracja Seed():
- DDRA = `$FF` (port A jako wyjście, ale tylko do odczytu klawiatury przez CA1)
- DDRB = `$7F` (port B bity 0-6 wyjście dla wyświetlacza, bit 7 wejście)
- CRA = `$04` (data register, CA1 positive edge, IRQ disabled)
- CRB = `$04` (data register, CB1 positive edge, IRQ disabled)

### Poddpięcia Apple 1

| Port | Kierunek | Podłączenie |
|------|----------|------------|
| Port A | input (CA1) | Klawiatura — dane + strobe CA1 |
| Port B | output (CB1) | Wyświetlacz — dane (bity 0-6) + strobe CB1 |

## Profile JSON

Dwa pliki w `src/Cpu.Board/profiles/`:

### `apple-1.json` — Woz Monitor (domyślny)

```json
{
  "name": "Apple 1",
  "cpu": { "type": "mos6502", "resetVector": "0xFFFC" },
  "memory": [
    { "type": "ram",  "start": "0x0000", "size": "0x1000", "label": "Main RAM" },
    { "type": "rom",  "start": "0xFF00", "file": "wozmon.bin", "label": "Woz Monitor" }
  ],
  "pia": { "address": "0xD010", ... },
  "display": { "cols": 40, "rows": 24, "font": "apple1.vid" }
}
```

### `apple-1-basic.json` — Integer BASIC + Woz Monitor (F10)

```json
{
  "name": "Apple 1",
  "cpu": {
    "type": "mos6502",
    "resetVector": "0xFFFC",
    "entryPoint": "0xE000"
  },
  "memory": [
    { "type": "ram",  "start": "0x0000", "size": "0x1000", "label": "Main RAM" },
    { "type": "rom",  "start": "0xE000", "size": "0x1000", "file": "basic.bin", "label": "Integer BASIC" },
    { "type": "rom",  "start": "0xFF00", "file": "wozmon.bin", "label": "Woz Monitor" }
  ],
  ...
}
```

### `entryPoint` — autostart

Pole opcjonalne w `cpu`. Jeśli ustawione, `MachineBoard.Reset()` po standardowym resecie nadpisuje `PC` wartością z `entryPoint`, pomijając wektor `$FFFC`/`$FFFD`. Używane dla BASIC ROM — start bezpośrednio z `$E000`, bez Woz Monitora.

## BASIC ROM — I/O

Integer BASIC (plik `basic.bin`, 4096 B, `$E000-$EFFF`) używa niestandardowego portu wyjścia:

| Operacja | Adres | Opis |
|----------|-------|------|
| Odczyt klawiatury | `$D011` / `$D010` | PIA CRA / PRA (status + dane, przez CA1) |
| Wyjście znaku | `$D0F2` | **NIE** `$D012` — BASIC pisze do `$D0F2` |

Procedura wyjścia znaku (`$E3C9-$E3DD`):
```asm
E3C9:  CMP #$8D        ; czy CR?
E3CB:  BNE E3D3        ; nie — skip
E3CD:  LDA #$00        ; (cold start entry)
E3CF:  STA $24         ; reset licznika
E3D1:  LDA #$8D        ; załaduj CR
E3D3:  INC $24         ; licznik wierszy
E3D5:  BIT $D0F2       ; czekaj na gotowość (bit 7 = 0)
E3D8:  BMI E3D5
E3DA:  STA $D0F2       ; wyślij znak
E3DD:  RTS
```

**`$D0F2` jest poza zakresem PIA** (`$D010-$D015`). Obsługę dodaje `BasicDspDevice` (`src/Cpu.Board/Adapters/BasicDspDevice.cs`) — prosty `IDevice` na `$D0F2`, który:
- **Read**: zwraca `$00` (zawsze gotowy, bit 7 = 0)
- **Write**: przekazuje znak do `Apple1DisplayAdapter`

Device jest automatycznie podpinany w konstruktorze `Apple1View` (`src/Cpu.Tui/Rendering/Views/Apple1View.cs:56`).

## Pliki binarne ROM

| Plik | Lokacja | Rozmiar | Adres docelowy |
|------|---------|---------|---------------|
| `wozmon.bin` | `src/Cpu.Board/roms/apple-1/` | 256 B | `$FF00-$FFFF` |
| `basic.bin` | `src/Cpu.Board/roms/apple-1/` | 4096 B | `$E000-$EFFF` |

## Przebieg uruchomienia BASIC (entryPoint)

1. `MachineBoard.Reset()` → `_cpu.Reset()` (reset 6502: rejestry, pamięć, wektor z `$FFFC`)
2. `entryPoint` = `$E000` → `PC = $E000`
3. `$E000`: `JMP $E2B0` (cold start BASIC)
4. BASIC init: ustawia wskaźniki, czyści obszary robocze
5. BASIC init: wyjście CR (`$8D`) + prompt (`$BE` → na ekranie `>`) przez `$D0F2`
6. BASIC wchodzi w pętlę wejścia (czeka na klawisz przez `$D011`/`$D010`)

## Testy

```bash
dotnet test tests/Cpu.Apple1.Tests/Cpu.Apple1.Tests.csproj
```

Kluczowe testy dla Apple 1:

| Test | Opis |
|------|------|
| `BasicRom_EntryJumpsToInitializer` | `$E000` = JMP `$E2B0` |
| `BasicProfile_E000R_StartsBasicRom` | BASIC init w pętli wejścia (PC=`$E003`) |
| `BasicProfile_Print1_ShowsResult` | `PRINT 1\r` → wyświetlacz zawiera `1` |
| `KeyboardAdapter_QueuesKeysAndNormalizesToUppercase` | Klawiatura normalizuje na uppercase |
| `WozMonitor_LoadedAtCorrectAddress` | Woz Monitor pod `$FF00` |
