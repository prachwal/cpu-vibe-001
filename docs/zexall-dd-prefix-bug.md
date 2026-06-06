# ZEXALL Z80 Test Suite — diagnoza

## Stan zweryfikowany

- `src/Z80` jest aktywnym emulatorem Z80 obok `src/Mos6502`.
- `DD 23` / `FD 23` (`INC IX/IY`) i `DD 2B` / `FD 2B` (`DEC IX/IY`) są już zaimplementowane.
- `DD E9` / `FD E9`, `DD F9` / `FD F9` oraz wiele undocumented `IXh/IXl/IYh/IYl` też są już w `DDFdPrefixHandler.cs`.
- `dotnet test Z80.slnx --filter FullyQualifiedName~DdFdTests` przechodzi.
- `dotnet test Z80.slnx --filter FullyQualifiedName~AddIxTest` przechodzi.
- Część testów `Zexdoc*` ma celowe `Assert.Fail`, więc obecnie służą jako diagnostyka, nie jako testy regresyjne.

## Problemy, które już wystąpiły

### 1. Zamiana opcode `ED 7A`

`ED 7A` było mapowane jak `SBC HL,SP`, nadpisując poprawne `ADC HL,SP`.

Przyczyna:

- błąd w tabeli dispatchu `EDPrefixHandler`.

Efekt:

- błędne CRC/test result dla grup 16-bit ALU.

Naprawa:

- jedna mapacja `ED 7A` do `AdcHlRr.Execute`;
- test opcode dla `ADC HL,SP`.

### 2. Kolejność odczytu/zapisu w `LDIR`

`LDIR` nie zachowywał się jak pojedyncza operacja `read source -> write destination`.

Przyczyna:

- przy nakładających się zakresach pamięci zapis mógł zmienić bajt, który logika później traktowała jak źródło.

Efekt:

- pętle i błędne dane w ROM-ach testowych.

Naprawa:

- zawsze buforować bajt lokalnie: `value = Read(HL)`, potem `Write(DE, value)`;
- dodać test z overlapem.

### 3. Zamiana `LD A,(BC/DE)` z `LD (BC/DE),A`

Opcode `0x02`, `0x12`, `0x0A`, `0x1A` były podłączone do przeciwnych handlerów.

Przyczyna:

- błąd nazewnictwa/mapowania w `InstructionTable`.

Efekt:

- `LD A,(DE)` pisało `A` do pamięci;
- zexall miał korupcję licznika/terminala i wykonywał złą liczbę przypadków.

Naprawa:

- `0x02` -> `LD (BC),A`;
- `0x12` -> `LD (DE),A`;
- `0x0A` -> `LD A,(BC)`;
- `0x1A` -> `LD A,(DE)`;
- utrzymać testy diagnostyczne `CounterCorruptionTest`.

## Główny otwarty problem

### `NopDdFd` jest architektonicznie zły jako fallback

Aktualny fallback dla nieobsłużonych `DD`/`FD` robi tylko:

- zapamiętanie diagnostyki;
- `cpu.Cycles += 4`;
- brak wykonania bazowej instrukcji;
- brak konsumpcji operandów bazowej instrukcji.

Na prawdziwym Z80 wiele prefiksowanych instrukcji `DD xx` / `FD xx`, które nie używają `HL/H/L`, wykonuje bazowe `xx` z kosztem prefiksu. Instrukcje z operandami muszą też przesunąć `PC` o operand.

Efekt:

- desynchronizacja `PC`;
- ciche pomijanie instrukcji;
- możliwe losowe zawieszenia w `zexall`;
- błędna liczba emulowanych instrukcji na test case.

Naprawa:

- zastąpić domyślne `NopDdFd.Execute` fallbackiem, który deleguje do bazowego handlera dla opcode nieużywających `H/L/(HL)`;
- dla opcode używających `H/L/(HL)` jawnie zaimplementować wariant `IX/IY` albo undocumented `IXh/IXl/IYh/IYl`;
- prawdziwy NOP zostawić tylko dla opcode, które rzeczywiście są no-op / undefined w tym kontekście;
- dodać testy dla fallbacku z operandami, np. `DD 06 nn`, `DD C3 nn nn`, `DD 04`, `DD 0C`.

## Problem wydajności

ZEXALL może być poprawny logicznie, ale bardzo wolny. Tego nie wolno mieszać z liczbą emulowanych instrukcji.

Znane źródła kosztu:

- `Cpu.Step()` robi delegate dispatch per instrukcja;
- `Registers` używa property w hot path;
- flagi są ustawiane przez wiele wywołań `SetFlag`;
- `SetSZPV` liczy parity pętlą;
- `RegisterHelper.GetRegister/SetRegister` używa switchy w instrukcjach ALU/LD.

Naprawa po zgodności:

- tablica `SZPV[256]` dla flag `S/Z/PV`;
- składanie `F` lokalnie i pojedyncze przypisanie;
- wyspecjalizowane handlery dla częstych ALU/LD zamiast ogólnego `RegisterHelper`;
- profil dopiero po usunięciu błędów `PC`/dispatchu.

## Kolejność działań

1. Naprawić `NopDdFd` fallback i pokryć testami `PC`.
2. Przerobić `ZexdocTests` na realny test regresyjny bez bezwarunkowego `Assert.Fail`.
3. Uruchomić `zexdoc` jako szybki test zgodności.
4. Dopiero potem mierzyć `zexall` i optymalizować hot path.
