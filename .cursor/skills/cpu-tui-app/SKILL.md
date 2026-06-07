---
name: cpu-tui-app
description: "Use when working on Cpu.Tui application logic — module chain, key routing, MainMenuModule, ModuleBase, feature modules, HelpView, navigation or echo bugs. Read before any OnKey or module hierarchy change."
---

# Cpu.Tui — logika aplikacji

Pełna treść skillu projektu: [.opencode/skills/cpu-tui-app.md](../../.opencode/skills/cpu-tui-app.md)

## Szybki start

1. `docs/README.md` — indeks
2. `docs/tui/key-map.md` — źródło prawdy klawiszy
3. `docs/tui/overview.md` — chain modułów

## Zasada nr 1

`OnKey` → `true` blokuje parent. Global F-keys działają tylko gdy leaf zwraca `false`.

## Testy po zmianie

```bash
dotnet test tests/Cpu.Tui.Tests --filter "ModuleKeyTests|ScreenModuleTests|DemoMenuModuleTests|DemoPlayerModuleTests"
```
