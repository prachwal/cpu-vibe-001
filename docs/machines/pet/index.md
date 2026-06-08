# Commodore PET Emulation

Moduł: `Cpu.Pet` | Klawisz: **F11**

Szczegółowa dokumentacja protokołu i implementacji: [ieee-488.md](ieee-488.md)

---

## I/O Map

| Adres | Układ | Funkcja |
|-------|-------|---------|
| `$8000–$83FF` | Video RAM | 40×25 znaków |
| `$E810–$E813` | PIA1 6520 | Klawiatura / kaseta / EOI (PA6) |
| `$E820–$E823` | PIA2 6520 | IEEE-488 DIO + NDAC out (CA2) / DAV out (CB2) |
| `$E840–$E84F` | VIA 6522 | IEEE-488 handshake (PB0–7) + CRTC VSync (CB1) |
| `$E880–$E88F` | CRTC 6545 | Kontroler wideo |
| `$C000–$FFFF` | ROM | BASIC + Editor + KERNAL |

---

## PIA 6520

### PIA1 ($E810) — klawiatura

| Adres | Rejestr | Opis |
|-------|---------|------|
| `$E810` | PRA / DDRA | Port A: wiersze klawiatury; PA6 = EOI input |
| `$E811` | CRA | Control register A |
| `$E812` | PRB / DDRB | Port B: kolumny klawiatury |
| `$E813` | CRB | Control register B |

### PIA2 ($E820) — IEEE-488 DIO

| Adres | Rejestr | Opis |
|-------|---------|------|
| `$E820` | PRA / DDRA | IEEE DIO input (odczyt danych z magistrali) |
| `$E821` | CRA | CA2 = IEEE NDAC output |
| `$E822` | PRB / DDRB | IEEE DIO output (zapis danych na magistralę) |
| `$E823` | CRB | CB2 = IEEE DAV output |

---

## IEEE-488 — Architektura

```
KERNAL
  │
  ├─ PIA2 $E822 (DIO write) ──→ PetIeeePortBBinding (EOR $FF) ──→ PetIeeeBus
  ├─ PIA2 $E820 (DIO read)  ←── PetIeeePortABinding (EOR $FF) ←──────────┤
  ├─ PIA2 CA2/CB2 (NDAC/DAV) ──────────────────────────────────→ PetIeeeBus
  └─ VIA $E840 PB2=ATN(w) PB1=NRFD(w) PB0/6/7=NDAC/NRFD/DAV(r) ────────┘
                                                                      │
                                                               PetIeeeDiskDrive #8
                                                                      │
                                                               CbmDosEngine
                                                                      │
                                                               D64Image (.d64)
```

### Mapowanie Sygnałów

| Sygnał IEEE-488 | Chip | Pin | Kierunek |
|-----------------|------|-----|----------|
| DIO1–8 | PIA2 Port A | `$E820` | read (RX) |
| DIO1–8 | PIA2 Port B | `$E822` | write (TX) |
| ATN | VIA | PB2 | out |
| DAV | VIA | PB7 | in |
| DAV | PIA2 | CB2 | out (`$34`=asserted, `$3C`=idle) |
| NRFD | VIA | PB6 | in |
| NRFD | VIA | PB1 | out |
| NDAC | VIA | PB0 | in |
| NDAC | PIA2 | CA2 | out |

---

## Obsługiwane Operacje DOS

| Operacja | Opis |
|----------|------|
| `LOAD"filename",8` | Wczytanie pliku PRG z D64 |
| `SAVE"filename",8` | Zapis pliku PRG do D64 |
| `LOAD"$",8` | Listing katalogu (jako program BASIC od `$0401`) |
| `OPEN 15,8,15` + komenda | Kanał komend DOS (SCRATCH, RENAME, INIT…) |

> `LOAD"$",8` z BASIC nie działa z KERNAL 2 — wymaga expansion ROM w `$B000`.  
> Szczegóły: [ieee-488.md#13-co-nie-działa-i-dlaczego](ieee-488.md#13-co-nie-działa-i-dlaczego)

---

## Format D64

Standard 35-track, 683 sektorów, 174 848 B (~170 KB).

| Tory | Sektory/tor |
|------|-------------|
| 1–17 | 21 |
| 18–24 | 19 |
| 25–30 | 18 |
| 31–35 | 17 |

BAM: track 18 / sector 0. Katalog: track 18 / sector 1+.

---

## Pliki Źródłowe

| Plik | Opis |
|------|------|
| `Devices/IIeeeDevice.cs` | Interfejs urządzenia IEEE-488 |
| `Devices/PetIeeeBus.cs` | State machine magistrali + handshake |
| `Devices/PetIeeePortABinding.cs` | Binding PIA2 Port A (DIO read) |
| `Devices/PetIeeePortBBinding.cs` | Binding PIA2 Port B (DIO write, EOR $FF) |
| `Devices/IPortBinding.cs` | Interfejs bindingów PIA |
| `Devices/CbmDos/D64Image.cs` | Parser D64 (BAM, katalog, sektory) |
| `Devices/CbmDos/CbmDosEngine.cs` | Silnik DOS (komendy, pliki, error channel) |
| `Devices/CbmDos/PetIeeeDiskDrive.cs` | Adapter IIeeeDevice → CbmDosEngine |
| `Devices/CbmDos/DirEntry.cs` | Struktura wpisu katalogu |
| `Devices/CbmDos/FileType.cs` | Enum typów plików |
| `System/PetMachine.cs` | Integracja: PIA2 callbacks, VIA, CRTC |

---

## Montowanie Obrazu

- **F12** w module PET → dialog wyboru pliku `.d64`
- Obrazy domyślne: `src/Cpu.Pet/roms/pet-test-disks/` (games-1.d64, utils.d64)

---

## Testy

```bash
dotnet test tests/Cpu.Pet.Tests/                              # wszystkie (~108)
dotnet test tests/Cpu.Pet.Tests/ --filter "Comm_"             # protokół IEEE-488
dotnet test tests/Cpu.Pet.Tests/ --filter "Cli_"              # CbmDosEngine
dotnet test tests/Cpu.Pet.Tests/ --filter "D64Image"          # parser D64
dotnet test tests/Cpu.Pet.Tests/ --filter "CbmDos"            # silnik DOS
```
