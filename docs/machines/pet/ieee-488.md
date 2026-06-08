# IEEE-488 / Stacja Dysków — Pełna Dokumentacja

**Wersja:** 2.0 | **Data:** 08.06.2026  
**Profil docelowy:** Commodore PET 2001-32 (BASIC 2 / KERNAL 2)  
**Zgodność testów:** `Comm_*` i `Cli_*` — 100% | Integracja z KERNAL — wymaga expansion ROM

---

## Spis Treści

1. [Architektura Sprzętowa](#1-architektura-sprzętowa)
2. [Interfejs Fizyczny IEEE-488](#2-interfejs-fizyczny-ieee-488)
3. [Protokół Sygnałów IEEE-488](#3-protokół-sygnałów-ieee-488)
4. [Adresacja Urządzeń](#4-adresacja-urządzeń)
5. [PetIeeeBus — State Machine](#5-petieeebus--state-machine)
6. [Bindingi PIA2](#6-bindingi-pia2)
7. [CbmDosEngine — Silnik DOS](#7-cbmdosengine--silnik-dos)
8. [Format D64](#8-format-d64)
9. [VIA 6522 — Rejestry](#9-via-6522--rejestry)
10. [PetPia6520 — Rejestry i CRB](#10-petpia6520--rejestry-i-crb)
11. [Kody Błędów CBM DOS](#11-kody-błędów-cbm-dos)
12. [Testy](#12-testy)
13. [Co Nie Działa i Dlaczego](#13-co-nie-działa-i-dlaczego)
14. [Pliki Źródłowe](#14-pliki-źródłowe)

---

## 1. Architektura Sprzętowa

### 1.1 Układy I/O

| Układ | Adres | Funkcja |
|-------|-------|---------|
| **PIA1** | `$E810–$E81F` | Matryca klawiatury + cassette; EOI input (PA6) |
| **PIA2** | `$E820–$E82F` | IEEE-488 DIO (magistrala danych) |
| **VIA 6522** | `$E840–$E84F` | Handshake IEEE-488 (ATN/DAV/NRFD/NDAC) + CRTC VSync |
| **CRTC 6545** | `$E880–$E88F` | Kontroler wyświetlacza |

### 1.2 Mapa Pamięci PET 2001-32

```
$0000–$7FFF  RAM (ZP: $00–$FF, stos: $0100, bufory: $0200, wektory: $0300)
$8000–$83FF  Video RAM (1000 komórek, 40×25)
$C000–$CFFF  BASIC ROM (c000)
$D000–$DFFF  BASIC ROM (d000)
$E000–$E7FF  Editor ROM
$E810–$E81F  PIA1 (klawiatura)
$E820–$E82F  PIA2 (IEEE-488 DIO)
$E840–$E84F  VIA 6522
$E880–$E88F  CRTC 6545
$F000–$FFFF  KERNAL ROM
```

### 1.3 Mapowanie Sygnałów IEEE-488 na Układy PET

| Sygnał | Chip | Pin | Kierunek | Polaryzacja |
|--------|------|-----|----------|-------------|
| DIO1–8 | PIA2 Port A (`$E820`) | — | read (RX) | transceiver, `EOR #$FF` |
| DIO1–8 | PIA2 Port B (`$E822`) | — | write (TX) | transceiver, `EOR #$FF` |
| ATN | VIA | PB2 | out | `0` = asserted (command mode) |
| DAV | VIA | PB7 | in | active-low: `0` = data valid |
| NRFD | VIA | PB6 | in | active-low: `0` = busy |
| NRFD | VIA | PB1 | out | active-low: `0` = busy |
| NDAC | VIA | PB0 | in | active-low: `0` = waiting |
| NDAC | PIA2 | CA2 | out | active-low |
| DAV | PIA2 | CB2 | out | active-low (`$34` = asserted) |
| EOI | PIA1 | PA6/CA2 | in/out | **nie zaimplementowany** |
| Display Enable | VIA | PB5 | in | z CRTC |

---

## 2. Interfejs Fizyczny IEEE-488

### 2.1 Złącze i Sygnały

**Złącze:** 24-pinowe (IEC 60625-2)  
**Logika:** TTL, **aktywne niskie** (0 V = aktywny, +5 V = nieaktywny)

| Pin | Sygnał | Kierunek | Opis |
|-----|--------|----------|------|
| 1 | DIO1 | I/O | Bit 1 magistrali danych |
| 2 | DIO2 | I/O | Bit 2 |
| 3 | DIO3 | I/O | Bit 3 |
| 4 | DIO4 | I/O | Bit 4 |
| 5 | EOI | O | End Or Identify — ostatni bajt transmisji |
| 6 | DAV | O | Data Valid — dane na magistrali są ważne |
| 7 | NRFD | I | Not Ready For Data — listener nie gotowy |
| 8 | NDAC | I | Not Data Accepted — listener nie przyjął danych |
| 9 | IFC | I | Interface Clear — reset magistrali |
| 10 | SRQ | I | Service Request |
| 11 | ATN | O | Attention — tryb komend |
| 12 | SHIELD | — | Ekran |
| 13 | DIO5 | I/O | Bit 5 |
| 14 | DIO6 | I/O | Bit 6 |
| 15 | DIO7 | I/O | Bit 7 |
| 16 | DIO8 | I/O | Bit 8 |
| 17 | REN | I | Remote Enable |
| 18–24 | GND | — | Masy sygnałowe i logiczna |

### 2.2 Grupy Sygnałów

1. **DIO1–8** — 8-bitowa magistrala danych (bidirekcjonalna)
2. **Handshake** — DAV, NRFD, NDAC (3-przewodowy, patrz sekcja 3)
3. **Zarządzanie magistralą** — ATN, IFC, REN, SRQ, EOI

---

## 3. Protokół Sygnałów IEEE-488

### 3.1 Tryby Magistrali

| Stan ATN | Tryb | Opis |
|----------|------|------|
| LOW (0) | **Command Mode** | DIO przenosi komendy IEEE-488 (LISTEN, TALK, …) |
| HIGH (1) | **Data Mode** | DIO przenosi dane aplikacji |

### 3.2 Komendy Magistrali (ATN = LOW)

| Komenda | Bajt | Opis |
|---------|------|------|
| LISTEN device N | `$20 \| N` | Wyznacz listenera (np. device 8 → `$28`) |
| TALK device N | `$40 \| N` | Wyznacz talkera (np. device 8 → `$48`) |
| SECONDARY addr N | `$60 \| N` | Kanał wtórny (np. kanał 0 → `$60`) |
| UNLISTEN | `$3F` | Zwolnij wszystkich listenerów |
| UNTALK | `$5F` | Zwolnij talkera |
| GTL | `$01` | Go To Local |
| SDC | `$04` | Selected Device Clear |
| SPE | `$18` | Serial Poll Enable |
| SPD | `$19` | Serial Poll Disable |

### 3.3 3-Przewodowy Handshake

Handshake kontroluje prędkość transmisji — dostosowuje się do **najwolniejszego urządzenia**. NRFD i NDAC działają jako **wired-AND** (open-collector): wszystkie aktywne listenery muszą jednocześnie zwolnić sygnał.

**Sekwencja dla jednego bajtu (talker → listener):**

```
Talker                       Listener(s)
──────                       ───────────
                             NRFD = 0  (gotowy)
DIO ← dane
DAV = 0  (dane ważne)
                             odczyt DIO
                             NDAC = 0  (zaakceptowane)
DAV = 1  (zwolnij)
                             NDAC = 1  (zwolnij)
                             NRFD = 0  (gotowy na następny)
```

Ostatni bajt: talker ustawia **EOI = 0** razem z **DAV = 0**.

### 3.4 Polaryzacja w PET

Magistrala IEEE-488 jest **active-low** — logiczne "asserted" = 0 V na kablu.  
VIA czyta i zapisuje zanegowane wartości przez transceiver:

| Sygnał | Magistrala asserted | VIA pin (odczyt) | Opis |
|--------|---------------------|------------------|------|
| DAV | LOW | PB7 = 0 | dane ważne |
| NRFD | LOW | PB6 = 0 | busy |
| NDAC | LOW | PB0 = 0 | oczekuje na dane |

---

## 4. Adresacja Urządzeń

### 4.1 Standard IEEE-488

Magistrala obsługuje do 15 urządzeń (adresy 0–30; 31 zarezerwowany).

### 4.2 Konwencja Commodore

| Adres IEEE-488 | Zastosowanie |
|---------------|-------------|
| 0 | Zarezerwowany (kontroler) |
| 4–7 | Drukarki |
| 8–15 | Stacje dysków (8 = domyślna) |
| 16–30 | Inne urządzenia |

---

## 5. PetIeeeBus — State Machine

**Plik:** `src/Cpu.Pet/Devices/PetIeeeBus.cs`

### 5.1 Stany

```
IDLE ──ATN ON──→ COMMAND ──ATN OFF──→ DATA_OUT / DATA_IN ──ATN ON──→ COMMAND ──…
```

| Stan | Opis |
|------|------|
| `Idle` | Brak aktywności, ATN = false |
| `Command` | ATN = true; bajty DIO to komendy IEEE-488 |
| `DataOut` | ATN = false; PET wysyła dane do urządzenia |
| `DataIn` | ATN = false; urządzenie wysyła dane do PET |

### 5.2 Kluczowe Pola

```csharp
private List<IIeeeDevice> _devices = [];
private BusState _state = BusState.Idle;
private int _listenerAddr = -1;
private int _talkerAddr = -1;
private byte _listenerSec;
private byte _talkerSec;
private IIeeeDevice? _listenerDevice;
private IIeeeDevice? _talkerDevice;
private byte _lastDio;
private byte _cachedInput;
private bool _hasCachedInput;
private bool _pendingCommand;
public bool DAV { get; private set; }
public bool NRFD { get; private set; }
public bool NDAC { get; private set; }
```

### 5.3 GetViaPortBInput — Polaryzacja

VIA Port B czyta **zanegowane** wartości sygnałów (active-low → `1` = idle):

```csharp
public byte GetViaPortBInput()
{
    byte result = 0;
    if (!NDAC) result |= 0x01;  // PB0: NDAC idle = 1 (accepted)
    if (!NRFD) result |= 0x40;  // PB6: NRFD idle = 1 (ready)
    if (!DAV)  result |= 0x80;  // PB7: DAV  idle = 1
    return result;
}
```

| Stan | NDAC (PB0) | NRFD (PB6) | DAV (PB7) | Opis |
|------|-----------|-----------|----------|------|
| Reset | 0 | 0 | 1 | idle |
| AcceptHandshake | 1 | 1 | 1 | dane przyjęte |
| CommandHandshake | 1 | 1 | 1 | komenda OK |
| CompleteHandshake | 0 | 1 | 1 | gotowy na następny |

### 5.4 Sekwencja CIOUT (wysyłanie bajtu przez PET)

KERNAL: `$FFA8` (vector) → `$F0D5`

```
1.  KERNAL: LDA $E840, BPL loop       ← czeka PB7=1 (DAV idle)
2.  KERNAL: AND #$FB, STA $E840       ← ATN = 0 (koniec fazy komendy)
3.  KERNAL: LDA #$3C, STA $E823       ← PIA2 CRB = $3C (DAV deassert)
4.  BUS:    OnCrbWrite($3C)            ← SetDAVState(false) → VIA PB7 = 1
5.  KERNAL: LDA $E840, AND #$41       ← sprawdza PB0, PB6
6.  KERNAL: EOR #$FF, STA $E822       ← wysyła bajt (odwrócony)
7.  BUS:    OnDioWrite(data ^ $FF)     ← podwójna inwersja → oryginalna wartość
8.  BUS:    ProcessCommandByte(data)   ← jeśli _pendingCommand
            lub _listenerDevice.Write  ← jeśli DataOut
9.  BUS:    AcceptHandshake()          ← NRFD=false, NDAC=false
10. KERNAL: BIT $E840, BVC loop       ← czeka PB6=1 (NRFD ready)
11. KERNAL: LDA #$34, STA $E823       ← PIA2 CRB = $34 (DAV assert)
12. BUS:    OnCrbWrite($34)            ← FlushOutput(); SetDAVState(true)
13. KERNAL: LDA #$FF, STA $E845       ← VIA T1CH = $FF (timer start)
14. KERNAL: LDA $E840, BIT $E84D      ← czeka PB0=1 (NDAC accepted) lub timeout
15. BUS:    NDAC=false → PB0=1        ← exit loop
16. KERNAL: LDA #$3C, STA $E823       ← PIA2 CRB = $3C (DAV deassert)
17. BUS:    OnCrbWrite($3C)            ← CompleteHandshake()
18. KERNAL: LDA #$FF, STA $E822       ← release DIO
```

---

## 6. Bindingi PIA2

### 6.1 PetIeeePortBBinding — Zapis DIO (TX)

**Plik:** `src/Cpu.Pet/Devices/PetIeeePortBBinding.cs`

```csharp
public byte ReadPins() => (byte)(_bus.GetCurrentDio() ^ 0xFF);
public void WritePins(byte value, byte ddMask) => _bus.OnDioWrite((byte)(value ^ 0xFF));
```

- KERNAL wykonuje `EOR #$FF` przed `STA $E822` → odwraca bity
- Binding odwraca ponownie → magistrala dostaje oryginalną wartość

### 6.2 PetIeeePortABinding — Odczyt DIO (RX)

**Plik:** `src/Cpu.Pet/Devices/PetIeeePortABinding.cs`

```csharp
public byte ReadPins()
{
    if (_bus.GetCurrentDio() == 0xFF)
        return 0;                           // brak danych
    return (byte)(_bus.OnDioRead() ^ 0xFF); // konsumuje i odwraca
}
```

- `GetCurrentDio()` — peek bez konsumpcji
- `OnDioRead()` — konsumuje jeden bajt z cache
- `0` (nie `$FF`) oznacza "brak danych" — to odróżnia od "dane = $FF"

---

## 7. CbmDosEngine — Silnik DOS

**Plik:** `src/Cpu.Pet/Devices/CbmDos/CbmDosEngine.cs`

### 7.1 Kanały (Secondary Address)

| SecAddr | Typ | Operacja |
|---------|-----|----------|
| 0 | Load | Odbiera filename → wysyła zawartość pliku (jako talker) |
| 1 | Save | Odbiera filename → odbiera dane → zapisuje na D64 |
| 15 | Command | Odbiera komendę DOS (`$I0`, `$S0:...`) → zwraca error channel |

### 7.2 Sekwencja LOAD "$",8 (katalog)

```
PET → Stacja (ATN=LOW):
  LISTEN 8   ($28)  → listener = drive #8
  SECONDARY 0 ($60) → kanał 0
  ATN = HIGH        → DataOut
  TX '$' ($24)      → CbmDosEngine.ReceiveByte($24)
  ATN = LOW
  UNLISTEN ($3F)    → CbmDosEngine.CloseChannel()
                      → ProcessFilename()
                      → "$" → GenerateDirectoryListing()

Stacja → PET (ATN=LOW):
  TALK 8     ($48)  → talker = drive #8
  SECONDARY 0 ($60) → kanał 0
  ATN = HIGH        → DataIn
  RX bajty listingu → CbmDosEngine.TryGetByte()
  …                 → program BASIC w _fileOutput
  RX $00 $00        → koniec programu BASIC
  [EOI brak]        → KERNAL nie wie o końcu transmisji
```

### 7.3 Format Listingu Katalogu

Wyjście: program BASIC z load address `$0401` (BASIC start na PET 2001):

```
Offset  Zawartość            Opis
──────  ─────────            ───
$0000   $01 $04              Load address (little-endian)
$0002   [link_lo][link_hi]   Link do następnej linii
$0004   [line_lo][line_hi]   Numer linii
$0006   [tekst…] $00         Tekst linii + terminator
…       …                    Kolejne linie
końcowy $00 $00              Terminator programu BASIC
```

### 7.4 Sekwencja LOAD "HELLO",8 (plik PRG)

```
LISTEN 8 + SEC 0 → TX $48 $45 $4C $4C $4F → UNLISTEN
  → ProcessFilename: szuka "HELLO" w katalogu D64
  → ładuje plik: load address (2B) + dane

TALK 8 + SEC 0 → DataIn → RX load address + dane pliku
```

---

## 8. Format D64

**Plik:** `src/Cpu.Pet/Devices/CbmDos/D64Image.cs`

### 8.1 Struktura Torów (1541 / CBM 2040/4040)

| Tory | Sektory/tor | Offset bajtu startowego |
|------|-------------|------------------------|
| 1–17 | 21 | `(track−1) × 21 × 256` |
| 18–24 | 19 | `17×21×256 + (track−18)×19×256` |
| 25–30 | 18 | `17×21×256 + 7×19×256 + (track−25)×18×256` |
| 31–35 | 17 | `17×21×256 + 7×19×256 + 6×18×256 + (track−31)×17×256` |

**Łącznie:** 683 sektory × 256 B = **174 848 B** (~170 KB)

### 8.2 BAM (Track 18, Sector 0)

| Offset | Rozmiar | Zawartość |
|--------|---------|-----------|
| `$00` | 1 | Track pierwszego sektora katalogu (zawsze `$12` = 18) |
| `$01` | 1 | Sector pierwszego sektora katalogu (zawsze `$01`) |
| `$02` | 1 | DOS version (`$41` = 'A'; `$00` lub `$41` = wolny zapis) |
| `$03` | 1 | Unused (zazwyczaj `$AA`) |
| `$04–$8F` | 35×4 | Wpisy BAM: [free_count][bitmap_lo][bitmap_mid][bitmap_hi] |
| `$90–$9F` | 16 | Nazwa dysku (PETASCII, padding `$A0`) |
| `$A0–$A1` | 2 | Wypełnienie `$A0` |
| `$A2–$A3` | 2 | Disk ID |
| `$A4` | 1 | Wypełnienie `$A0` |
| `$A5–$A6` | 2 | DOS type (zazwyczaj `"2A"`) |

### 8.3 Katalog (Track 18, Sector 1+)

Każdy wpis katalogu ma 32 bajty:

| Offset | Rozmiar | Zawartość |
|--------|---------|-----------|
| `$00` | 1 | Track następnego sektora katalogu (`$00` = ostatni) |
| `$01` | 1 | Sector następnego sektora katalogu |
| `$02` | 1 | Typ pliku + flagi (bit7=closed, bit6=locked, bit0–3=type) |
| `$03` | 1 | Start track pliku |
| `$04` | 1 | Start sector pliku |
| `$05–$14` | 16 | Nazwa pliku (PETASCII, padding `$A0`) |
| `$15–$16` | 2 | Track/Sector side-sector (tylko REL) |
| `$17` | 1 | Record length (tylko REL, max 254) |
| `$18–$1D` | 6 | Unused (może zawierać info GEOS) |
| `$1E–$1F` | 2 | Rozmiar pliku w sektorach (little-endian) |

**Typy plików** (bity 0–3 offsetu `$02`):

| Wartość | Typ | W katalogu (bit7=closed) |
|---------|-----|--------------------------|
| `$00` | DEL | `$00` |
| `$01` | SEQ | `$81` |
| `$02` | PRG | `$82` |
| `$03` | USR | `$83` |
| `$04` | REL | `$84` |

### 8.4 Łańcuch Sektorów PRG

```
Sektor 0:  [next_T][next_S][load_addr_lo][load_addr_hi][dane…]
Sektor 1+: [next_T][next_S][dane…]
Ostatni:   [$00][valid_bytes][dane…]
```

- Bajt 0 sektora: następny track (`$00` = ostatni sektor)
- Bajt 1 sektora: następny sektor (lub liczba ważnych bajtów gdy track=0)
- Pierwsze 2 bajty danych sektora 0 = load address programu

---

## 9. VIA 6522 — Rejestry

**Adres bazowy:** `$E840`  
**Plik:** `src/Cpu.Chips/Via6522/VIA6522Chip.cs`

| Adres | Rejestr | Opis |
|-------|---------|------|
| `$E840` | ORB | Port B — read/write |
| `$E841` | ORA | Port A — read/write |
| `$E842` | DDRB | Data Direction B (1=out, 0=in) |
| `$E843` | DDRA | Data Direction A |
| `$E844` | T1CL | Timer 1 counter low |
| `$E845` | T1CH | Timer 1 counter high (zapis startuje timer) |
| `$E846` | T1LL | Timer 1 latch low |
| `$E847` | T1LH | Timer 1 latch high |
| `$E848` | T2CL | Timer 2 counter low |
| `$E849` | T2CH | Timer 2 counter high |
| `$E84A` | SR | Shift Register |
| `$E84B` | ACR | Auxiliary Control Register |
| `$E84C` | PCR | Peripheral Control Register |
| `$E84D` | IFR | Interrupt Flag Register |
| `$E84E` | IER | Interrupt Enable Register |
| `$E84F` | ORA | Port A (bez handshake) |

**Mapowanie Port B na IEEE-488:**

| Bit | Sygnał | Kierunek | Uwaga |
|-----|--------|----------|-------|
| PB0 | NDAC | in | odczyt ze stacji |
| PB1 | NRFD | out | zapis do magistrali |
| PB2 | ATN | out | `0` = command mode |
| PB5 | Display Enable | in | z CRTC (VSync) |
| PB6 | NRFD | in | odczyt ze stacji |
| PB7 | DAV | in | `0` = data valid |

---

## 10. PetPia6520 — Rejestry i CRB

**Plik:** `src/Cpu.Pet/Devices/PetPia6520.cs`

### 10.1 Adresy Rejestrów

```
PIA1 ($E810) — klawiatura:
  $E810 = Port A / DDRA  (wiersze klawiatury)
  $E811 = CRA
  $E812 = Port B / DDRB  (kolumny klawiatury)
  $E813 = CRB

PIA2 ($E820) — IEEE-488 DIO:
  $E820 = Port A / DDRA  (odczyt DIO)
  $E821 = CRA             → steruje CA2 = NDAC out
  $E822 = Port B / DDRB  (zapis DIO)
  $E823 = CRB             → steruje CB2 = DAV out
```

### 10.2 Obsługa CRB i CRA (DAV / NDAC handshake)

Bity CRB/CRA kontrolujące CB2/CA2 (oba działają identycznie):

| Bity [5:3] | Wartość | CB2/CA2 | Opis |
|------------|---------|---------|------|
| `111` | `$38` | HIGH | deasserted — idle |
| `110` | `$30` | LOW | asserted — active |

W praktyce KERNAL używa tylko dwóch wartości CRB (bit2=1 → zawsze dostęp do rejestru danych):

| Wartość CRB | Bity [5:2] | CB2 (DAV) | Opis |
|-------------|------------|-----------|------|
| `$3C` (`0011 1100`) | `1111` | HIGH | DAV deasserted — dane nieważne |
| `$34` (`0011 0100`) | `1101` | LOW | DAV asserted — dane ważne |

**Implementacja w `PetMachine.cs`:**

```csharp
// CRA → NDAC out (CA2)
_pia2.OnCraWrite = (value) =>
{
    bool ndacAccepted = (value & 0x3C) == 0x34;
    _ieeeBus.SetNdacAccepted(ndacAccepted);
};

// CRB → DAV out (CB2)
byte crbState = 0;
_pia2.OnCrbWrite = (value) =>
{
    bool davAsserted = (value & 0x3C) == 0x34;   // maska 0x3C, nie 0x38!
    if (davAsserted)
        ieeeOutputBinding.FlushOutput();           // flush przed DAV
    if (crbState == 0x34 && value == 0x3C)
        _ieeeBus.CompleteHandshake();              // DAV deassert po assert = koniec bajtu
    crbState = value;
    _ieeeBus.SetDAVState(davAsserted);
};
```

**Weryfikacja maski `0x3C`:**
- `$34 & $3C = $34` → `== $34` → `true` (DAV asserted) ✓
- `$3C & $3C = $3C` → `≠ $34` → `false` (DAV idle) ✓
- (maska `0x38` byłaby błędna: `$34 & $38 = $30 ≠ $34` — nigdy nie byłoby true)

---

## 11. Kody Błędów CBM DOS

Format odpowiedzi kanału 15: `NN,"KOMUNIKAT",TT,SS\r` (kod, opis, track, sector)

| Kod | Opis |
|-----|------|
| `00` | OK |
| `01` | FILES SCRATCHED |
| `20` | READ ERROR (block header not found) |
| `21` | READ ERROR (no sync character) |
| `22` | READ ERROR (data block not present) |
| `23` | READ ERROR (checksum error in data) |
| `25` | WRITE ERROR (write-verify) |
| `26` | WRITE PROTECT ON |
| `30` | SYNTAX ERROR (general) |
| `31` | SYNTAX ERROR (invalid command) |
| `33` | SYNTAX ERROR (invalid filename) |
| `62` | FILE NOT FOUND |
| `63` | FILE EXISTS |
| `64` | FILE TYPE MISMATCH |
| `65` | NO BLOCK |
| `66` | ILLEGAL TRACK AND SECTOR |
| `70` | NO CHANNEL |
| `72` | DISK FULL |
| `73` | CBM DOS V2.6 (status po reset/power-on) |
| `74` | DRIVE NOT READY |

---

## 12. Testy

### 12.1 Komunikacja (bezpośrednie API magistrali)

```bash
dotnet test tests/Cpu.Pet.Tests/ --filter "Comm_FullLoadSequence"  # LOAD "$",8 → 1003B listingu
dotnet test tests/Cpu.Pet.Tests/ --filter "Comm_LoadHello"          # LOAD "HELLO",8 → 4484B
dotnet test tests/Cpu.Pet.Tests/ --filter "Comm_ErrorAnalysis"      # analiza błędów
```

### 12.2 CLI do stacji (CbmDosEngine)

```bash
dotnet test tests/Cpu.Pet.Tests/ --filter "Cli_ListFiles"           # lista plików na D64
dotnet test tests/Cpu.Pet.Tests/ --filter "Cli_DownloadAndVerify"   # pobierz + weryfikuj
dotnet test tests/Cpu.Pet.Tests/ --filter "Cli_FullDownloadAll"     # wszystkie PRG
```

### 12.3 Wszystkie testy PET

```bash
dotnet test tests/Cpu.Pet.Tests/   # ~108 testów
```

---

## 13. Co Nie Działa i Dlaczego

| Funkcja | Status | Przyczyna |
|---------|--------|-----------|
| `Comm_*` — API magistrali | ✅ | Pełna emulacja IEEE-488 |
| `Cli_*` — CbmDosEngine | ✅ | D64 + DOS + SAVE/LOAD |
| `LOAD "$",8` z BASIC | ❌ | KERNAL `$F3C2` to handler CASSETTE, nie IEEE |
| `OPEN 1,8,0,"…"` z BASIC | ❌ | Brak kodu IEEE w KERNAL/BASIC 2 ROM |
| EOI na ostatnim bajcie | ❌ | Nieużywany — KERNAL nie sprawdza (PIA1 PA6) |
| Expansion ROM (`$B000`) | ❌ | Nie istnieje w repo |

### 13.1 Rozwiązanie: Expansion ROM w `$B000`

Wymagane operacje ROM:
1. Zastąp `$FFD5` (LOAD) handlerem dyskowym
2. Zarejestruj urządzenie 8 w KERNAL (`$0251`, `$AE`, `$D2`)
3. Zaimplementuj IEEE CHRIN/CHKIN z detekcją końca transmisji (EOI)
4. Ustaw wektory `$033C–$0346` na trampoliny do `$F0D5` / `$F1BA`

---

## 14. Pliki Źródłowe

| Plik | Opis |
|------|------|
| `Devices/IPortBinding.cs` | Interfejs bindingów PIA |
| `Devices/PetPia6520.cs` | PIA 6520 — 2 porty (A+B), CRA/CRB callbacks |
| `Devices/PetKeyboardPiaBinding.cs` | Binding klawiatury (PIA1) |
| `Devices/PetIeeePortABinding.cs` | Binding DIO read (PIA2 Port A) |
| `Devices/PetIeeePortBBinding.cs` | Binding DIO write z inwersją (PIA2 Port B) |
| `Devices/IIeeeDevice.cs` | Interfejs urządzenia IEEE-488 |
| `Devices/PetIeeeBus.cs` | State machine magistrali + handshake |
| `Devices/CbmDos/D64Image.cs` | Parser D64 (BAM, katalog, łańcuchy sektorów) |
| `Devices/CbmDos/CbmDosEngine.cs` | DOS (LOAD, SAVE, INIT, error channel) |
| `Devices/CbmDos/PetIeeeDiskDrive.cs` | Adapter: IIeeeDevice → CbmDosEngine |
| `Devices/CbmDos/DirEntry.cs` | Wpis katalogu |
| `Devices/CbmDos/FileType.cs` | Enum typów plików |
| `System/PetMachine.cs` | Integracja: IEEE bus + PIA2 callbacks |
| `src/Cpu.Chips/Via6522/VIA6522Chip.cs` | Emulacja VIA 6522 |
