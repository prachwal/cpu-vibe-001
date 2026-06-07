# Plan naprawczy — grafika TUI

> Stan po audycie grafiki i pierwszej fali ujednolicenia (Setup, `TerminalGraphicsModes`, tryb `TrueTone`).
> Priorytety: **P0** krytyczne / już naprawione, **P1** następna iteracja, **P2** architektura.

## Wykryte błędy i niespójności

| ID | Problem | Skutek | Status |
|----|---------|--------|--------|
| G1 | `DefaultGraphicsMode` z Setup nie trafiał do widoków po zapisie | Zmiana w F2 bez efektu w Image/Canvas | **Naprawione** — `ApplySettings` ustawia `Mode` |
| G2 | Cykl trybów zduplikowany w `ImageView` / `CanvasView` | Różna kolejność po dodaniu trybu | **Naprawione** — `TerminalGraphicsModes.Next()` |
| G3 | `FitImage`: `Grayscale` miał `prc=2`, renderer `1×1` | Złe proporcje obrazu w Grayscale | **Naprawione** — `TerminalGraphicsModes.PixelsPerCellRow` |
| G4 | Podwójne skalowanie: `FitImage` + `Resize*` w `TerminalGraphicsRenderer.Render` | Rozmycie / koszt CPU | **Otwarte (P1)** |
| G5 | `RenderColorShade()` niepodpięty do enum | Martwy kod; nazwa `Grayscale` myląca | **Otwarte (P1)** |
| G6 | Duplikat palety 16 kolorów (`TerminalGraphicsRenderer`, `ColorQuantizer`) | Dryf wartości | **Otwarte (P2)** |
| G7 | `BestGlyphRenderer` tworzony co klatkę | Alokacje w hot path | **Otwarte (P2)** |
| G8 | Różne skróty trybu: Image ↑↓/F8, Canvas F10 | Brak jednej mapy klawiszy | **Częściowo** — wspólny cykl + F8 w Canvas; Image nadal ↑↓ |
| G9 | Dokumentacja: F10 = FrameStyle w Screen | Sprzeczność z Setup | **Naprawione** w docs |
| G10 | `JpegImageLoader` — pełny decode przed `FitImage`, wymaga `ffmpeg` | RAM, brak ffmpeg → błąd UI | **Otwarte (P2)** |

## Co zostało zrobione (P0)

1. **`TerminalGraphicsModes`** (`Cpu.Tui.Abstractions`) — wspólna kolejność cyklu, gęstość pikseli/komórkę, helper resize.
2. **Tryb `TrueTone`** — 1 piksel / komórkę, truecolor RGB, znak spacji (`fg=bg=kolor`), bez atlasu glyph.
3. **`ApplySettings`** w Image/Canvas — propagacja `FrameStyle` + `DefaultGraphicsMode` przy każdym `SyncViewSettings`.
4. **`PresentationSession.FitImage`** — delegacja do `TerminalGraphicsModes` (naprawa Grayscale).
5. **Canvas F8** — ten sam skrót co Image do cyklu trybu (F10 nadal działa).

### Kolejność cyklu trybów

```
HalfBlockColor → BrailleMono → Grayscale → TrueTone → BestGlyph → BestGlyphTrueColor → …
```

## Plan naprawczy — backlog

### P1 — Jakość obrazu i spójność API

| Krok | Opis | Pliki |
|------|------|-------|
| P1a | **Jeden resize** — `FitImage` zwraca `(PixelBuffer scaled, TermRect cells)` albo renderer przyjmuje już przeskalowany bufor | `PresentationSession`, `TerminalGraphicsRenderer`, widoki |
| P1b | **ColorShade** — albo nowy enum `ColorShade`, albo rename `Grayscale` + dokumentacja; podpiąć `RenderColorShade` lub usunąć | `TerminalGraphicsMode`, renderer |
| P1c | **Key map** — `F8` globalny cykl trybu w modułach graficznych; ↑↓ tylko gdy moduł nie używa ich inaczej (Canvas demo 3D) | `key-map.md`, moduły |
| P1d | Test regresji `FitImage` × każdy tryb (wymiary komórek vs pikseli) | `Cpu.Tui.Tests` |

### P2 — Wydajność i architektura

| Krok | Opis |
|------|------|
| P2a | Cache `BestGlyphRenderer` / atlas (singleton lub pole statyczne — już częściowo) |
| P2b | Wspólna paleta `TerminalPalette16` dla quantizer + half-block |
| P2c | `PresentationContext` (record ze `FrameStyle`, `TerminalGraphicsMode`, rozmiarem) — widok tylko renderuje |
| P2d | `JpegImageLoader` — opcjonalny progressive/thumbnail; komunikat Setup gdy brak ffmpeg |
| P2e | `InputRouter` + centralny `KeyBindings.cs` — patrz [roadmap.md](roadmap.md) |

### P3 — Dokumentacja i UX

| Krok | Opis |
|------|------|
| P3a | HelpView — lista trybów + skróty zgodne z [key-map.md](key-map.md) |
| P3b | Setup — podgląd miniatur trybu (opcjonalnie) |
| P3c | README root — usunąć stare „F10 = ramki” |

## Kryteria „done” dla P1a (podwójny resize)

- [ ] `Render()` nie wywołuje `ResizeNearest`/`ResizeBilinear` gdy bufor ma już wymiary wynikające z `FitImage`.
- [ ] Test: ten sam plik JPG — piksel (100,100) identyczny przed/po refaktorze w `TrueTone` i `HalfBlockColor`.
- [ ] Benchmark lub test czasu renderu — brak regresji >10%.

## Powiązane pliki

| Obszar | Ścieżka |
|--------|---------|
| Enum + helper | `src/Cpu.Tui.Abstractions/TerminalGraphicsMode.cs`, `TerminalGraphicsModes.cs` |
| Renderer | `src/Cpu.Tui.Media/TerminalGraphicsRenderer.cs` |
| Fit | `src/Cpu.Tui/Rendering/PresentationSession.cs` |
| Widoki | `Cpu.Image/Rendering/Views/ImageView.cs`, `Cpu.Canvas/Rendering/Views/CanvasView.cs` |
| Config | `TuiAppSettings`, `SetupModule`, `ModuleBase.SyncViewSettings` |
| Docs | [graphics.md](graphics.md), [roadmap.md](roadmap.md), [key-map.md](key-map.md) |
