# Lekcje implementacyjne — VIC-20 / PET

## Najczęstsze błędy i jak ich unikać

| # | Problem | Przyczyna | Lekcja na przyszłość |
|---|---------|-----------|---------------------|
| 1 | **Ekran czytany z $9000 zamiast $1000** | `ToCpuAddress` — C# `~` na `int` = 32-bit NOT, kod oczekiwał 16-bit. VIC $3000 → CPU $9000 (VIC regs) zamiast $1000 | Używaj jawnych masek 16-bit (`(vic & 0x2000) == 0`). Testuj każde przesunięcie bitowe osobnym testem |
| 2 | **Profil nie znajdował ROM-ów** | `profile.Name` → katalog ROM-ów. "VIC-20 PAL" → `commododre-vic-20-pal/` zamiast `commodore-vic-20/` | Nadaj profilom nazwy pasujące do katalogów ROM-ów. Testuj czy ROM się ładuje (`Assert.NotEqual(0, Read(firstByte))`) |
| 3 | **KERNAL nie skanuje matrycy** | ROM ma martwy kod — IRQ handler `$FF72` to stub, nie woła skanera. Procedura zapisu do $0277 istnieje ($EBC0) ale nikt jej nie woła | Przed implementacją klawiatury: zdekompiluj ROM, znajdź gdzie i jak KERNAL czyta matrycę. Jeśli nie ma → implementuj w `Machine` |
| 4 | **Brak 1 bajtu RAM-u → brak bootu** | `"0x0FFF"` zamiast `"0x1000"` w profilu. KERNAL nie wykrył RAM-u ($28/$29 = $0000) | JSON size = end - start + 1. Weryfikuj skryptem: `size = int(size,16); assert size == end - start + 1` |
| 5 | **Pierwszy znak w negatywie** | Do bufora $0277 trafiał kod ekranowy (0x01) zamiast PETSCII (0x41). Edytor w KERNAL-u AND #$3F konwertuje PETSCII→screen code | Zrozum jaki kod KERNAL oczekuje w buforze (PETSCII, nie screen code). Sprawdź przez `$E742: CMP #$20, BCC, AND #$3F` |
| 6 | **Logika biznesowa w widoku** | `FillKeyboardBuffer`, `ProcessPendingKeys`, PIA handshake — wszystko w view zamiast w `Machine`/`Devices` | **C16/TED7360.cs** = tylko układ; **C16Machine.cs** = logika maszyny; **C16View** = tylko `Render()`. view może TYLKO wołać metody maszyny |
| 7 | **Raster IRQ na VIC-I** | Zaimplementowałem VIC-II (C64) feature na VIC-I (VIC-20). Lock w pętli IRQ, boot nie startuje | Sprawdź w datasheet: **VIC-I (6560/6561) = NO raster IRQ**. VIC-II (6567/6569) = HAS raster IRQ. Zawsze czytaj dokumentację układu |
| 8 | **Timer VIA 3× wolniejszy** | `Update()` wołany raz na instrukcję zamiast raz na cykl CPU. VIA timer odmierzał 1/6 czasu | Iteruj po cycle count CPU (`_board.Cpu.Cycles` delta), nie po instrukcjach. `Step(cycles)` = cycles CPU, nie instructions |
| 9 | **Kursor nie mrugał / biały negatyw** | Sprawdzałem `raw == 0xA0` zamiast `(raw & 0x80) != 0`. Kursor VIC przełącza bit 7, nie tylko między 0x20 a 0xA0 | Kursor włącza/wyłącza bit 7. Po wpisaniu znaku przełącza między `char` a `char|0x80`. Sprawdź `bit7` a nie konkretną wartość |
| 10 | **test passes with VIC register data** | Test `Boot_ScreenRam_ContainsBasicBannerText` sprawdzał screen na $9000 (VIC regs) i znajdował fałszywe 'C' z wartości rejestru | Test czytający screen RAM musi czytać z poprawnego adresu. Zdiagnozuj `ScreenAddr` przed testem. Nie ufaj testom które "przechodzą" |

## Reguły przy debugowaniu

1. **Zanim napiszesz kod → przeczytaj ROM**. Sprawdź czy firmware w ogóle ma kod który chcesz emulować (np. skaner matrycy)
2. **Testuj każdą zmianę w izolacji**. `dotnet test --filter "NowyTest"` przed `dotnet test --filter "All"`
3. **Zapis do profilu JSON → zweryfikuj rozmiar**. `size = end - start + 1`
4. **Adres ekranu → potwierdź przez $9005**. $9005 = $C0 → screen = `(0xC0 >> 4) * 0x400` = $3000 VIC → CPU z `ToCpuAddress`
5. **Bufor klawiatury KERNAL → znajdź adres przez ROM**. Szukaj `STA $0277,X` lub `$0276` w ROM-ie
6. **IRQ → sprawdź czy układ MA przerwania zanim je implementujesz**. VIC-I: NIE. VIC-II: TAK. TED7360: ?
7. **Kod do bufora → PETSCII, nie screen code**. KERNAL sam konwertuje przez `AND #$3F`
