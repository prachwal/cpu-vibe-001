# TUI image rendering demo

## Sterowanie

- `F7 Image` — wejście do podglądu JPG z katalogu `samples/`.
- `F7 Next` — następny obraz.
- `Left/Right` — poprzedni/następny obraz.
- `F8` — zmiana trybu renderingu:
  - `HalfBlockColor`;
  - `BrailleMono`;
  - `Grayscale`;
  - `ColorShade`.
- `Esc Back` — wyjście z podglądu obrazu.

Sterowanie jest widoczne w dolnym pasku funkcyjnym.

## Skalowanie

Obraz jest skalowany do aktualnego obszaru roboczego z zachowaniem proporcji:

- `HalfBlockColor`: 1 komórka terminala = 1x2 piksele.
- `BrailleMono`: 1 komórka terminala = 2x4 piksele.
- `Grayscale`: 1 komórka terminala = 1x1 piksel logiczny, ale skalowanie kompensuje proporcje znaku terminala jako ok. 1x2.
- `ColorShade`: 1 komórka terminala = 1x1 piksel logiczny, znak z jasności, kolor z RGB; skalowanie kompensuje proporcje znaku terminala jako ok. 1x2.

## Dekoder JPG

`Cpu.Tui` używa lokalnego `ffmpeg`:

```text
ffmpeg -i image.jpg -f rawvideo -pix_fmt rgb24 -
```

Nie dodajemy zależności NuGet dla dekodowania obrazów. Jeśli `ffmpeg` nie istnieje, podgląd obrazu pokaże komunikat błędu.

## UTF i ramki

Renderer obsługuje UTF-8, co jest pokryte testami `AnsiBuilderTests` i `AnsiTerminalRendererTests`.

Ramki aplikacji są przełączane:

- domyślnie `ASCII`, stabilne w każdym terminalu;
- `F6 Frame:UTF`, jeśli terminal i font poprawnie obsługują box drawing.

Obraz może używać znaków Unicode:

- half-block: `▀`;
- braille: `U+2800..U+28FF`;
- grayscale: `░▒▓█`.
- color shade: `░▒▓█` + kolor foreground.
