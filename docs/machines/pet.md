# Commodore PET Emulation

Moduł: `Cpu.Pet` | Klawisz: **F11**

## I/O Map

| Adres | Układ | Funkcja |
|-------|-------|---------|
| `$8000-$83FF` | Video RAM | 40×25 znaków |
| `$E810-$E813` | PIA 6520 | Port A: matryca klawiatury (wiersze), Port B: kolumny + IEEE-488 DIO |
| `$E840-$E84F` | VIA 6522 | CRTC VSync (CB1), IEEE-488 handshake (PB0-7) |
| `$E880-$E88F` | CRTC 6545 | Kontroler wideo |
| `$C000-$FFFF` | ROM | BASIC + Editor + KERNAL |

## PIA 6520 ($E810)

| Adres | Offset | Rejestr | Opis |
|-------|--------|---------|------|
| `$E810` | +0 | PRA / DDRA | Port A: wiersze klawiatury (wyjście) / DIO read (wejście, IEEE-488) |
| `$E811` | +1 | CRA | Control register A |
| `$E812` | +2 | PRB / DDRB | Port B: kolumny klawiatury (wejście) / DIO write (wyjście, IEEE-488) |
| `$E813` | +3 | CRB | Control register B |

Port B jest współdzielony między matrycą klawiszy a magistralą danych IEEE-488 (DIO).
Kierunek zależy od DDRB: `$00` = tryb klawiatury (wejście), `$FF` = tryb IEEE-488 (wyjście).

## IEEE-488 / Stacja dysków

Emulacja stacji dysków Commodore (urządzenie #8) przez wirtualną magistralę IEEE-488.

### Architektura

```
KERNAL → PIA $E812 (DIO) → PetIeeePortBBinding → PetIeeeBus → PetIeeeDiskDrive → CbmDosEngine → D64Image
         VIA $E840 (PB2=ATN) ────┘                                       ↑
         VIA $E840 (PB0/6/7=NDAC/NRFD/DAV) ──────┘                      │
                                                                   plik .d64
```

### Mapowanie sygnałów

| Sygnał IEEE-488 | W PET | Kierunek |
|-----------------|-------|----------|
| DIO1-DIO8 | PIA Port B ($E812) | dwukierunkowy (przez DDRB) |
| ATN | VIA PB2 (write), PIA CA1 (read) | sterowanie PET |
| DAV | VIA PB7 (read) | odczyt z szyny |
| NRFD | VIA PB6 (read), VIA PB1 (write) | uzgadnianie |
| NDAC | VIA PB0 (read), PIA CA2 (write) | uzgadnianie |
| SRQ | PIA CB1 (read) | nieużywany |

### Obsługiwane komendy DOS

- `I` — Initialize (reset stacji)
- `LOAD"filename",8` — wczytanie pliku PRG
- `SAVE"filename",8` — zapis pliku PRG
- `LOAD"$",8` — listing katalogu (generuje program BASIC)
- Channel 15 — kanał komend/errorów (`OPEN 15,8,15`)

### Obsługa D64

Format: standard 35-track, 683 sektory, 174,848 bajtów.
Wsparcie: BAM, katalog, łańcuch sektorów, alokacja/zapis.

### Montowanie obrazu

- **F12** w module PET → dialog wyboru pliku `.d64`
- Obrazy domyślne: `roms/pet-test-disks/` (games-1.d64, utils.d64)

### Pliki

| Plik | Opis |
|------|------|
| `Devices/IIeeeDevice.cs` | Interfejs urządzenia IEEE-488 |
| `Devices/PetIeeeBus.cs` | Silnik protokołu magistrali (state machine) |
| `Devices/PetIeeePortBBinding.cs` | Binding PIA Port B do magistrali |
| `Devices/IPortBinding.cs` | Interfejs bindingów PIA |
| `Devices/CbmDos/D64Image.cs` | Parsowanie Obrazów D64 (BAM, katalog, sektory) |
| `Devices/CbmDos/CbmDosEngine.cs` | Silnik DOS (komendy, pliki, errory) |
| `Devices/CbmDos/PetIeeeDiskDrive.cs` | Adapter IIeeeDevice → CbmDosEngine |
| `Devices/CbmDos/DirEntry.cs` | Struktura wpisu katalogu |
| `Devices/CbmDos/FileType.cs` | Enum typów plików |

### Testy

```bash
dotnet test tests/Cpu.Pet.Tests --filter "PetIeeeBus"
dotnet test tests/Cpu.Pet.Tests --filter "PetIeeeDiskDrive"
dotnet test tests/Cpu.Pet.Tests --filter "D64Image"
dotnet test tests/Cpu.Pet.Tests --filter "CbmDos"
dotnet test tests/Cpu.Pet.Tests --filter "CbmDosSave"
dotnet test tests/Cpu.Pet.Tests --filter "PetMachineIeee"
```
