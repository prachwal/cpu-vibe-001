---
name: cpu-tui-app
description: "Use when working on Cpu.Tui application logic — module chain, key routing, MainMenuModule, ModuleBase, Screen/Help/Demo/Image/Canvas/Apple1 modules, HelpView text, or fixing navigation/echo bugs. Read before any OnKey or module hierarchy change."
---

# Cpu.Tui — logika aplikacji (moduły i klawisze)

## Obowiązkowy workflow

```
1. READ docs/README.md                          → indeks
2. READ docs/tui/key-map.md                     → mapa klawiszy (źródło prawdy)
3. READ docs/tui/overview.md                    → chain, ModuleBase, widoki
4. IF graphics/pixels only → cpu-tui-graphics skill
5. AFTER change → update key-map, HelpView, ModuleKeyTests, AGENTS.md if arch
```

## Model mentalny

```
App → ModuleManager → MainMenuModule → [optional child] → [optional nested child]
         OnKey/Tick/Render od root; active leaf = najgłębszy Child
```

- **Moduł** (`ModuleBase`): stan, `OnKeyCore`, panel boczny, woła widok
- **Widok** (`BaseTermView`): tylko render; dostaje stan przez właściwości przed `Render`

## Reguły dispatch klawiszy

### Semantyka `OnKey`

| Zwraca | Znaczenie |
|--------|-----------|
| `true` | Klawisz skonsumowany — parent **nie** dostaje fallback |
| `false` | Bubble — parent może obsłużyć (Esc, F-keys w MainMenu) |

### MainMenu (jedyny global F-key fallback)

Po `child.OnKey == false`: Esc → `CloseChild`; F1/F4/F7/F8/F9 → `SwitchTo(name)`.

### Parent z childem (DemoMenuModule)

```csharp
if (Child.OnKey(key)) return true;
if (key == Escape) { CloseChild(); return true; }
return false; // bubble F-keys to MainMenu
```

**Nigdy** Esc przed `Child.OnKey` gdy child ma własną politykę Esc (DemoPlayer playing).

### Leaf moduły — wzorzec Apple1

Global navigation → `return false`:

```csharp
case ConsoleKey.F1:
case ConsoleKey.F4:
case ConsoleKey.F7:
case ConsoleKey.F8:
case ConsoleKey.F9:
case ConsoleKey.Escape:
    return false;
```

### Screen + echo

Lokalne: F5/F6/F10 → `true`. W echo: F-keys/Esc → `false`; reszta → `_echo.ProcessKey`.

## Antywzorce (powtarzane błędy)

| Błąd | Skutek | Poprawka |
|------|--------|----------|
| `return true` bez handlera | F1/F7 nie działają | `return false` |
| Echo `ProcessKey` dla wszystkich klawiszy | Blokada nawigacji | Wyklucz F1–F9, Esc |
| Stan w module, nie w widoku | F5/F10 bez efektu | Ustaw `view.*` przed Render |
| DemoPlayer kończy `return true` | Brak F-keys w play | F-keys → false |
| HelpView / README bez sync | Użytkownik zdezorientowany | Aktualizuj z key-map |

## Propagacja stanu (Screen — wzorzec docelowy)

```csharp
protected override void RenderContent(...)
{
    _screenView.EchoEnabled = _echoMode;
    _screenView.ScreenMode = _screenMode;
    _screenView.FrameStyle = _frameStyle;
    _screenView.Render(r, area, session);
}
```

Nowe flagi modułu → nowa właściwość widoku → test w `ScreenModuleTests`.

## Pliki kluczowe

| Obszar | Plik |
|--------|------|
| Root router | `src/Cpu.Tui/Modules/MainMenuModule.cs` |
| DI / scan | `src/Cpu.Tui/AppServices.cs` |
| Base panel | `src/Cpu.Tui/Modules/ModuleBase.cs` |
| Screen | `src/Cpu.Screen/Modules/ScreenModule.cs` |
| Demo chain | `src/Cpu.DemoMenu/Modules/DemoMenuModule.cs`, `DemoPlayerModule.cs` |
| Passthrough wzór | `src/Cpu.Apple1/Modules/Apple1Module.cs` |
| Help text | `src/Cpu.Help/Rendering/Views/HelpView.cs` |

## Testy obowiązkowe po zmianie klawiszy

```bash
dotnet test tests/Cpu.Tui.Tests --filter "ModuleKeyTests|ScreenModuleTests|DemoMenuModuleTests|DemoPlayerModuleTests|ModuleChainTests"
```

Nowy moduł → dodaj wiersz w `ModuleKeyTests` + sekcję w `docs/tui/key-map.md`.

## Checklist PR (TUI logic)

- [ ] Leaf nie połyka F-keys bez powodu
- [ ] Echo nie blokuje globalnej nawigacji
- [ ] Stan modułu propagowany do widoku
- [ ] HelpView zgodny z key-map
- [ ] `docs/tui/key-map.md` zaktualizowany
- [ ] AGENTS.md jeśli zmiana architektury chain

## Backlog (nie implementuj bez zadania)

- P8: Unicode frames w pozostałych widokach
- P9: stabilna kolejność menu w AppServices
- InputRouter, PresentationContext — patrz `docs/tui/roadmap.md`

## Dokumentacja

| Temat | Plik |
|-------|------|
| Indeks | `docs/README.md` |
| Klawisze | `docs/tui/key-map.md` |
| Architektura | `docs/tui/overview.md` |
| Grafika pikseli | `.opencode/skills/cpu-tui-graphics.md` |
| Roadmap | `docs/tui/roadmap.md` |
