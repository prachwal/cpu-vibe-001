# IEEE-488 / Stacja Dysków — Implementacja w CPU-VIBE-001

**Wersja dokumentu:** 1.0 | **Data:** 08.06.2026  
**Profil docelowy:** Commodore PET 2001-32 (BASIC 2 / KERNAL 2)  
**Zgodność:** Testy `Comm_*` i `Cli_*` — 100% | Integracja z KERNAL — wymaga expansion ROM

---

## 1. Architektura Sprzętowa

### 1.1 Układy I/O

| Układ | Adres | Funkcja |
|-------|-------|---------|
| **PIA1** ($E810) | `$E810-$E81F` | Matryca klawiatury (wiersze/kolumny) |
| **PIA2** ($E820) | `$E820-$E82F` | IEEE-488 DIO (dane) |
| **VIA** ($E840) | `$E840-$E84F` | Handshake IEEE-488 + CRTC VSync |

### 1.2 Mapowanie Pamięci

```
$0000-$7FFF  RAM (w tym ZP $00FF, stos $0100, bufory $0200, wektory $0300)
$8000-$83FF  Video RAM
$C000-$CFFF  BASIC ROM (c000)
$D000-$DFFF  BASIC ROM (d000)  
$E000-$E7FF  Editor ROM
$E810-$E81F  PIA1 (klawiatura)
$E820-$E82F  PIA2 (IEEE-488 DIO)
$E840-$E84F  VIA 6522
$E880-$E88F  CRTC 6545
$F000-$FFFF  KERNAL ROM
```

### 1.3 Sygnały IEEE-488

| Sygnał | W PET | VIA pin | Polaryzacja (bus → VIA) |
|--------|-------|---------|------------------------|
| DIO1-8 | PIA2 Port A ($E820) / Port B ($E822) | — | przez transceiver, inwersja `EOR #$FF` |
| ATN | VIA PB2 (write) | PB2 | `0` = ATN asserted (command mode) |
| DAV | VIA PB7 (read) | PB7 | `!DAV` → `1` = idle, `0` = data valid |
| NRFD | VIA PB6 (read), VIA PB1 (write) | PB6/PB1 | `!NRFD` → `1` = ready, `0` = busy |
| NDAC | VIA PB0 (read), PIA2 CA2 (write) | PB0 | `!NDAC` → `1` = accepted, `0` = waiting |
| EOI | — | — | **nie zaimplementowany** (wymaga expansion ROM) |

---

## 2. PetIeeeBus — State Machine

### 2.1 Stany

```
IDLE ──ATN ON──→ COMMAND ──ATN OFF──→ DATA_OUT / DATA_IN ──ATN ON──→ COMMAND ──...
```

| Stan | Opis |
|------|------|
| `Idle` | Brak aktywności. ATN = false. |
| `Command` | ATN = true. Bajty na DIO to komendy IEEE-488 (LISTEN, TALK, SECONDARY, UNLISTEN, UNTALK). |
| `DataOut` | ATN = false. PET wysyła dane do urządzenia (filename, komendy DOS). |
| `DataIn` | ATN = false. Urządzenie wysyła dane do PET (zawartość pliku, listing katalogu, error). |

### 2.2 Pola Kluczowe

```csharp
// PetIeeeBus.cs
private List<IIeeeDevice> _devices = [];    // lista urządzeń (np. PetIeeeDiskDrive #8)
private BusState _state = BusState.Idle;
private int _listenerAddr = -1;              // adres listenera (LISTEN)
private int _talkerAddr = -1;               // adres talkera (TALK)
private byte _listenerSec;                  // secondary address listenera
private byte _talkerSec;                    // secondary address talkera
private IIeeeDevice? _listenerDevice;       // urządzenie listener
private IIeeeDevice? _talkerDevice;         // urządzenie talker
private byte _lastDio;                      // ostatni bajt na DIO
private byte _cachedInput;                  // prefetch cache dla DataIn
private bool _hasCachedInput;
private bool _pendingCommand;               // flaga: następny bajt to komenda (ATN był ON)
public bool DAV { get; private set; }       // stan DAV (logiczny)
public bool NRFD { get; private set; }      // stan NRFD (logiczny)
public bool NDAC { get; private set; }      // stan NDAC (logiczny)
```

### 2.3 Handshake — GetViaPortBInput

```csharp
// Polaryzacja active-low → VIA PB czyta zanegowane wartości
public byte GetViaPortBInput()
{
    byte result = 0;
    if (!NDAC) result |= 0x01;  // PB0: NDAC accepted = 1
    if (!NRFD) result |= 0x40;  // PB6: NRFD ready = 1
    if (!DAV)  result |= 0x80;  // PB7: DAV valid = 0
    return result;
}
```

#### Stany VIA PB dla KERNAL CIOUT

| Stan | NDAC (PB0) | NRFD (PB6) | DAV (PB7) | Opis |
|------|-----------|-----------|----------|------|
| Reset | 0 | 0 | 1 | idle |
| AcceptHandshake | 1 | 1 | 1 | dane przyjęte |
| CommandHandshake | 1 | 1 | 1 | komenda OK |
| CompleteHandshake | 0 | 1 | 1 | gotowy na następny |

### 2.4 Sekwencja CIOUT (wysyłanie bajtu)

KERNAL woła przez $FFA8 (vector) → $F0D5 / przez BASIC 4 → $DAA7 → $F0D5.

```
1. KERNAL: LDA $E840, BPL loop      ← czeka PB7=1 (DAV idle)
2. KERNAL: AND #$FB, STA $E840       ← ATN = 0 (command end)
3. KERNAL: LDA #$3C, STA $E823       ← PIA2 CRB = $3C (CB2=high, DAV deassert)
4. BUS:   OnCrbWrite(0x3C)           ← SetDAVState(false) → VIA PB7 = 1
5. KERNAL: LDA $E840, AND #$41       ← sprawdza PB0,PB6
6. KERNAL: EOR #$FF, STA $E822       ← wysyła bajt (z inwersją)
7. BUS:   OnDioWrite(data ^ 0xFF)     ← odwrócona inwersja → data oryginalna
8. BUS:   ProcessCommandByte(data)    ← jeśli _pendingCommand
          lub _listenerDevice.Write   ← jeśli DataOut
9. BUS:   AcceptHandshake()           ← NRFD=false, NDAC=false
10. KERNAL: BIT $E840, BVC loop      ← czeka PB6=1 (NRFD ready)
11. KERNAL: LDA #$34, STA $E823      ← PIA2 CRB = $34 (CB2=low, DAV assert)
12. BUS:   OnCrbWrite(0x34)           ← SetDAVState(true)
13. KERNAL: LDA #$FF, STA $E845      ← VIA T1CH = $FF (timer start)
14. KERNAL: LDA $E840, BIT $E84D     ← czeka PB0=1 (NDAC accepted) lub timeout
15. BUS:   NDAC=false → PB0=1        ← exit loop
16. KERNAL: LDA #$3C, STA $E823      ← PIA2 CRB = $3C (DAV deassert)
17. BUS:   OnCrbWrite(0x3C)           ← CompleteHandshake()
18. KERNAL: LDA #$FF, STA $E822      ← release DIO
```

---

## 3. PetIeeePortBBinding — PIA2 Port B (DIO write)

**Plik:** `src/Cpu.Pet/Devices/PetIeeePortBBinding.cs`

```csharp
public byte ReadPins() => (byte)(_bus.GetCurrentDio() ^ 0xFF);
public void WritePins(byte value, byte ddMask) => _bus.OnDioWrite((byte)(value ^ 0xFF));
```

- KERNAL woła `EOR #$FF` przed `STA $E822` → odwraca bity
- Binding odwraca ponownie → magistrala dostaje oryginalną wartość
- `ReadPins()`: gdy brak danych (`GetCurrentDio() == 0xFF`), zwraca 0

## 4. PetIeeePortABinding — PIA2 Port A (DIO read)

**Plik:** `src/Cpu.Pet/Devices/PetIeeePortABinding.cs`

```csharp
public byte ReadPins()
{
    if (_bus.GetCurrentDio() == 0xFF)
        return 0;                          // brak danych
    return (byte)(_bus.OnDioRead() ^ 0xFF); // konsumuje i odwraca
}
```

- `GetCurrentDio()` peekuje bez konsumpcji
- `OnDioRead()` konsumuje jeden bajt z cache
- Gdy `_hasCachedInput == false`, zwraca 0 (nie `$FF`) — to odróżnia "brak danych" od "dane = $FF"

---

## 5. CbmDosEngine — Silnik DOS

**Plik:** `src/Cpu.Pet/Devices/CbmDos/CbmDosEngine.cs`

### 5.1 Kanały

| SecAddr | Typ | Operacja |
|---------|-----|----------|
| 0 | Load | Odbiera filename → wysyła zawartość pliku |
| 1 | Save | Odbiera filename → odbiera dane → zapisuje na D64 |
| 15 | Command | Odbiera komendę DOS → zwraca error channel |

### 5.2 Sekwencja LOAD "$",8

```
PET → Stacja:
  ATN ON
  LISTEN 8       ($28) → listener = drive #8
  SECONDARY 0    ($60) → channel 0
  ATN OFF              → DataOut
  
  TX '$'         ($24) → CbmDosEngine.ReceiveByte(0x24)
  
  ATN ON
  UNLISTEN       ($3F) → CbmDosEngine.CloseChannel()
                        → ProcessFilename()
                        → _commandBuffer = [0x24]
                        → fn == "$" → GenerateDirectoryListing()

Stacja → PET:
  ATN ON
  TALK 8         ($48) → talker = drive #8
  SECONDARY 0    ($60) → channel 0
  ATN OFF              → DataIn

  RX bajty listingu    → CbmDosEngine.TryGetByte()
  ...                   → _fileOutput jako BASIC program
  RX $00 $00           → end of BASIC program
  [EOI — brak!]        → KERNAL nie wie że koniec
```

### 5.3 GenerateDirectoryListing — Format

Wyjście: program BASIC zaczynający się od load address `$0401`:

```
Offset  Zawartość           Opis
──────  ─────────           ───
$00     $01 $04             Load address $0401 (BASIC start)
$02     [link_lo][link_hi]  Link do następnej linii
$04     [line_lo][line_hi]  Numer linii
$06     [tekst...] $00      Tekst linii + terminator
...     ...                 Kolejne linie
końca   $00 $00             Terminator programu BASIC
```

### 5.4 Sekwencja LOAD "HELLO",8

```
LISTEN 8 + SEC 0 → TX $48 $45 $4C $4C $4F → UNLISTEN
  → ProcessFilename: szuka "HELLO" w katalogu D64
  → ładuje plik: load address (2B) + dane (4484B)
TALK 8 + SEC 0 → RX dane pliku → RX $00 $...
```

---

## 6. D64Image — Format D64

**Plik:** `src/Cpu.Pet/Devices/CbmDos/D64Image.cs`

### 6.1 Struktura

| Track | Sektory | Offset startu |
|-------|---------|---------------|
| 1-17  | 21 | `(track-1) × 21 × 256` |
| 18-24 | 19 | `17×21×256 + (track-18)×19×256` |
| 25-30 | 18 | ... |
| 31-35 | 17 | ... |

### 6.2 BAM (Track 18, Sector 0)

```
Offset  Rozmiar  Opis
$00     2        T/S pierwszego sektora katalogu
$02     1        DOS version ($41 = "A")
$04     35×4     BAM entries (free count + 24-bit bitmap)
$90     16       Nazwa dysku (PETASCII, $A0 padded)
$A2     2        Disk ID
$A5     2        DOS type ("2A")
```

### 6.3 Katalog (Track 18, Sector 1+)

Wpis katalogu (32B):

```
Offset  Opis
$00     Track następnego sektora (0 = ostatni)
$01     Sector następnego sektora
$02     File type + flags (bit7=closed, bit6=locked, bit0-3=type)
$03     Start track
$04     Start sector
$05-20  Nazwa pliku (16B, PETASCII, $A0 padded)
$21-22  Side-sector T/S (REL only)
$23     Record length (REL only)
$24-29  Reserved
$30-31  Rozmiar w sektorach (LE)
```

### 6.4 Łańcuch Sektorów PRG

```
Sektor 0:  [next_T][next_S][load_addr_lo][load_addr_hi][dane...]
Sektor 1:  [next_T][next_S][dane...]
...
Ostatni:   [0][valid_bytes][dane...]
```

---

## 7. VIA 6522 — Rejestry

**Plik:** `src/Cpu.Chips/Via6522/VIA6522Chip.cs`

| Adres | Rejestr | Opis |
|-------|---------|------|
| `$E840` | ORB | Port B (output) |
| `$E841` | ORA | Port A |
| `$E842` | DDRB | Data Direction B |
| `$E843` | DDRA | Data Direction A |
| `$E844` | T1CL | Timer 1 counter low |
| `$E845` | T1CH | Timer 1 counter high |
| `$E84C` | PCR | Peripheral Control |
| `$E84D` | IFR | Interrupt Flag |
| `$E84E` | IER | Interrupt Enable |

VIA PB wejścia IEEE-488:

| PBx | Sygnał | Kierunek |
|-----|--------|----------|
| PB0 | NDAC | wejście (z bus) |
| PB1 | NRFD | wyjście (do bus) |
| PB2 | ATN | wyjście (do bus) |
| PB5 | Display Enable | wejście (z CRTC) |
| PB6 | NRFD | wejście (z bus) |
| PB7 | DAV | wejście (z bus) |

---

## 8. PetPia6520 — Rejestry

**Plik:** `src/Cpu.Pet/Devices/PetPia6520.cs`

### 8.1 Adresy

```
PIA1: $E810 (klawiatura)
  $E810 = Port A / DDRA (rows)
  $E811 = CRA
  $E812 = Port B / DDRB (columns)
  $E813 = CRB

PIA2: $E820 (IEEE-488 DIO)
  $E820 = Port A (read DIO)
  $E821 = CRA
  $E822 = Port B (write DIO)
  $E823 = CRB
```

### 8.2 Obsługa CRB (DAV handshake)

```csharp
byte crbState = 0;
_pia.OnCrbWrite = (value) =>
{
    if (crbState == 0x34 && value == 0x3C)
        _ieeeBus.CompleteHandshake();
    crbState = value;
    _ieeeBus.SetDAVState((value & 0x38) == 0x34);
};
```

| CRB value | CB2 state | DAV | Opis |
|-----------|-----------|-----|------|
| `$3C` | high | deasserted | DAV idle / koniec |
| `$34` | low (pulse) | asserted | DAV active (dane ważne) |

---

## 9. Testy

### 9.1 Bezpośrednie API magistrali

```bash
# Pełna sekwencja LOAD "$",8 → 1003B listingu
dotnet test --filter "Comm_FullLoadSequence"

# LOAD "HELLO",8 → 4484B danych
dotnet test --filter "Comm_LoadHello"

# Analiza błędów (rozpoznanie winnej strony)
dotnet test --filter "Comm_ErrorAnalysis"
```

### 9.2 CLI do stacji (CbmDosEngine)

```bash
dotnet test --filter "Cli_ListFiles"       # lista plików na D64
dotnet test --filter "Cli_DownloadAndVerify" # pobierz + weryfikuj
dotnet test --filter "Cli_FullDownloadAll"   # wszystkie PRG
```

### 9.3 Wszystkie

```bash
dotnet test tests/Cpu.Pet.Tests       # 108 testów
```

---

## 10. Co nie działa i dlaczego

| Funkcja | Status | Przyczyna |
|---------|--------|-----------|
| `Comm_*` (API magistrali) | ✅ | Pełna emulacja IEEE-488 |
| `Cli_*` (CbmDosEngine) | ✅ | D64 + DOS + SAVE/LOAD |
| `LOAD "$",8` z BASIC | ❌ | KERNAL `$F3C2` to handler CASSETTE, nie DISK |
| `OPEN 1,8,0,"..."` z BASIC | ❌ | Brak kodu IEEE w KERNAL/BASIC 2 ROM |
| EOI na ostatnim bajcie | ❌ | Nieużywany — KERNAL nie sprawdza |
| Expansion ROM ($B000) | ❌ | Nie istnieje w repo |

**Rozwiązanie:** Expansion ROM w $B000 który:
1. Zastępuje `$FFD5` (LOAD) handlerem dyskowym
2. Rejestruje urządzenie 8 w KERNAL ($0251 + $AE + $D2)
3. Implementuje IEEE CHRIN/CHKIN z detekcją końca transmisji
4. Ustawia wektory $033C-$0346 na trampoliny do KERNAL `$F0D5`/`$F1BA`

---

## 11. Pliki Źródłowe

| Plik | Opis |
|------|------|
| `Devices/IPortBinding.cs` | Interfejs bindingów PIA |
| `Devices/PetPia6520.cs` | PIA 6520 z obsługą 2 portów |
| `Devices/PetKeyboardPiaBinding.cs` | Binding klawiatury (PIA1) |
| `Devices/PetIeeePortABinding.cs` | Binding DIO read (PIA2 Port A) |
| `Devices/PetIeeePortBBinding.cs` | Binding DIO write (PIA2 Port B) |
| `Devices/IIeeeDevice.cs` | Interfejs urządzenia IEEE-488 |
| `Devices/PetIeeeBus.cs` | State machine magistrali + handshake |
| `Devices/CbmDos/D64Image.cs` | Parser D64 (BAM, katalog, sektory) |
| `Devices/CbmDos/CbmDosEngine.cs` | DOS (LOAD, SAVE, INIT, error channel) |
| `Devices/CbmDos/PetIeeeDiskDrive.cs` | Adapter: IIeeeDevice → CbmDosEngine |
| `Devices/CbmDos/DirEntry.cs` | Wpis katalogu |
| `Devices/CbmDos/FileType.cs` | Enum typów plików |
| `System/PetMachine.cs` | Integracja wszystkiego |
| `Chips/Via6522/VIA6522Chip.cs` | Emulacja VIA 6522 |
