---
name: pet-ieee488
description: "Use when working on Commodore PET IEEE-488 bus, disk drive emulation, D64 images, CbmDosEngine, PIA2 bindings, or handshake signals (DAV/NRFD/NDAC/ATN). Read before any change to PetIeeeBus, PetMachine CRB/CRA callbacks, D64Image, or CbmDosEngine."
---

# PET IEEE-488 — Cheat-Sheet

## Obowiązkowy workflow

```
1. READ docs/machines/pet/ieee-488.md   → pełna specyfikacja protokołu + implementacji
2. READ docs/machines/pet/index.md      → I/O map, architektura, pliki
3. RUN dotnet test tests/Cpu.Pet.Tests/ --filter "Comm_"   → weryfikacja po zmianie
```

## Architektura (jeden widok)

```
KERNAL → PIA2 $E822 (write, EOR $FF) → PetIeeePortBBinding → PetIeeeBus.OnDioWrite
         PIA2 $E820 (read,  EOR $FF) ← PetIeeePortABinding ← PetIeeeBus.OnDioRead
         PIA2 CRB $E823 (DAV out)   ──────────────────────→ PetMachine.OnCrbWrite
         PIA2 CRA $E821 (NDAC out)  ──────────────────────→ PetMachine.OnCraWrite
         VIA  $E840 PB2 (ATN out)   ──────────────────────→ PetIeeeBus.OnATNWrite
         VIA  $E840 PB0/6/7 (in)    ←── PetIeeeBus.GetViaPortBInput()
```

## Mapowanie VIA Port B

| Bit | Sygnał | Kierunek |
|-----|--------|----------|
| PB0 | NDAC | in (active-low: 0=waiting) |
| PB1 | NRFD | out (active-low) |
| PB2 | ATN | out (0=command mode) |
| PB5 | Display Enable | in (z CRTC) |
| PB6 | NRFD | in (active-low: 0=busy) |
| PB7 | DAV | in (active-low: 0=data valid) |

## CRB / CRA Kluczowe Wartości

| Wartość | CB2/CA2 | Znaczenie |
|---------|---------|-----------|
| `$3C` | HIGH | DAV/NDAC deasserted (idle) |
| `$34` | LOW | DAV/NDAC asserted (active) |

```csharp
// POPRAWNA maska: 0x3C, NIE 0x38
bool davAsserted = (value & 0x3C) == 0x34;
// $34 & $3C = $34 → true ✓    $3C & $3C = $3C → false ✓
// (0x38 byłoby błędne: $34 & $38 = $30 ≠ $34 → zawsze false)
```

## Sekwencja CRB w PetMachine.cs

```csharp
_pia2.OnCraWrite = (value) => {
    _ieeeBus.SetNdacAccepted((value & 0x3C) == 0x34);
};
_pia2.OnCrbWrite = (value) => {
    bool davAsserted = (value & 0x3C) == 0x34;
    if (davAsserted) ieeeOutputBinding.FlushOutput();   // flush PRZED DAV
    if (crbState == 0x34 && value == 0x3C)
        _ieeeBus.CompleteHandshake();                   // DAV deassert = koniec bajtu
    crbState = value;
    _ieeeBus.SetDAVState(davAsserted);
};
```

## Komendy IEEE-488 (ATN=LOW)

| Komenda | Bajt | Przykład (dev 8) |
|---------|------|-----------------|
| LISTEN N | `$20\|N` | `$28` |
| TALK N | `$40\|N` | `$48` |
| SECONDARY N | `$60\|N` | `$60` (ch.0) |
| UNLISTEN | `$3F` | — |
| UNTALK | `$5F` | — |

## Kanały CBM DOS

| SA | Typ | Opis |
|----|-----|------|
| 0 | Load | filename IN → dane OUT |
| 1 | Save | filename IN → dane IN → zapis D64 |
| 15 | Command | komenda DOS IN → error channel OUT |

## D64 — Kluczowe Offsety

| Co | Track / Sector | Bajt |
|----|---------------|------|
| BAM | 18 / 0 | — |
| DOS version | 18 / 0 | `$02` = `$41` |
| BAM entries (35×4B) | 18 / 0 | `$04–$8F` |
| Disk name | 18 / 0 | `$90–$9F` |
| Katalog | 18 / 1+ | — |
| Filename w wpisie | — | `$05–$14` (16B) |
| File size (sektory) | — | `$1E–$1F` (LE) |

Tory 1–17: 21 sek. | 18–24: 19 sek. | 25–30: 18 sek. | 31–35: 17 sek.

## DIO Inwersja

KERNAL wykonuje `EOR #$FF` przed `STA $E822`.  
`PetIeeePortBBinding.WritePins` wykonuje drugi `EOR` → magistrala dostaje oryginalny bajt.

## Typowe Pułapki

1. **Maska CRB** — zawsze `0x3C`, nie `0x38`
2. **FlushOutput przed DAV** — binding buforuje DIO; flush musi być przed `SetDAVState(true)`
3. **CRA dla NDAC** — NDAC out idzie przez `OnCraWrite`, nie `OnCrbWrite`
4. **VIA PB aktywne niskie** — `GetViaPortBInput` zwraca `1` gdy sygnał idle (deasserted)
5. **D64 offsety hex** — katalog: `$05–$14` (hex), nie `$05-20` (decimal notation)
6. **LOAD z BASIC** — nie działa bez expansion ROM; `Comm_*` testy używają API bezpośrednio

## Testy

```bash
dotnet test tests/Cpu.Pet.Tests/ --filter "Comm_FullLoadSequence"  # LOAD "$",8
dotnet test tests/Cpu.Pet.Tests/ --filter "Comm_LoadHello"          # LOAD "HELLO",8
dotnet test tests/Cpu.Pet.Tests/ --filter "Cli_ListFiles"           # katalog D64
dotnet test tests/Cpu.Pet.Tests/ --filter "Cli_DownloadAndVerify"   # plik PRG
dotnet test tests/Cpu.Pet.Tests/                                     # wszystkie
```
