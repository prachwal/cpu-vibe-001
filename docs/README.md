# Dokumentacja CPU-VIBE-001

Indeks dokumentacji repozytorium. **Przed zmianą TUI** czytaj [tui/overview.md](tui/overview.md) i stosuj skill `.opencode/skills/cpu-tui-app.md`.

## Szybki start

| Cel | Dokument |
|-----|----------|
| Uruchomienie, skróty użytkownika | [README.md](../README.md) |
| Reguły dla agentów | [AGENTS.md](../AGENTS.md) |
| Terminal UI — architektura | [tui/overview.md](tui/overview.md) |
| Mapa klawiszy | [tui/key-map.md](tui/key-map.md) |
| Apple 1 | [machines/apple1.md](machines/apple1.md) |

## Terminal UI (`docs/tui/`)

| Plik | Zawartość |
|------|-----------|
| [overview.md](tui/overview.md) | Projekty, pętla `App`, chain modułów, rendering |
| [key-map.md](tui/key-map.md) | **Źródło prawdy** — dispatch klawiszy per moduł |
| [views-checklist.md](tui/views-checklist.md) | Lifecycle widoków, audyt komponentów |
| [graphics.md](tui/graphics.md) | Tryby graficzne, skalowanie, JPG |
| [graphics-fix-plan.md](tui/graphics-fix-plan.md) | Błędy, niespójności, plan naprawczy (G1–G10) |
| [ansi-renderer.md](tui/ansi-renderer.md) | Design `AnsiTerminalRenderer` (referencja) |
| [roadmap.md](tui/roadmap.md) | Plan uspójnienia — done + backlog |

Skill graficzny (pixele, API render): [.opencode/skills/cpu-tui-graphics.md](../.opencode/skills/cpu-tui-graphics.md)

Skill aplikacji (moduły, klawisze, antywzorce): [.opencode/skills/cpu-tui-app.md](../.opencode/skills/cpu-tui-app.md)

## CPU MOS 6502 (`docs/cpu/`)

| Plik | Zawartość |
|------|-----------|
| [6502-architecture.md](cpu/6502-architecture.md) | Pełna specyfikacja 6502 |
| [variants.md](cpu/variants.md) | Warianty 2A03, 65C02 |

Flow implementacji: [FLOW.md](../FLOW.md)

## CPU Z80 (`docs/z80/`)

| Plik | Zawartość |
|------|-----------|
| [hot-path.md](z80/hot-path.md) | Design hot path |
| [zexall-dd-prefix-bug.md](z80/zexall-dd-prefix-bug.md) | Diagnostyka DD/FD prefix |

## Maszyny (`docs/machines/`)

| Plik | Zawartość |
|------|-----------|
| [machines/apple1.md](machines/apple1.md) | Profile JSON, I/O, BASIC quirk |
| [machines/pet/index.md](machines/pet/index.md) | PET 2001: I/O map, architektura IEEE-488, testy |
| [machines/pet/ieee-488.md](machines/pet/ieee-488.md) | Protokół IEEE-488, handshake, D64, CbmDosEngine |

Skill IEEE-488: [.opencode/skills/pet-ieee488.md](../.opencode/skills/pet-ieee488.md)

## Meta

| Plik | Zawartość |
|------|-----------|
| [meta/opencode-models.md](meta/opencode-models.md) | Porównanie modeli OpenCode Go |

## Konwencja nazw

- `docs/<domena>/` — folder tematyczny (cpu, z80, tui, machines)
- krótkie kebab-case (`key-map.md`, nie `tui-consistency-plan.md`)
- jeden temat = jeden plik; unikaj duplikacji między README, AGENTS i docs

## Aktualizacja po zmianach

| Zmiana | Zaktualizuj |
|--------|-------------|
| Routing klawiszy / moduły | `tui/key-map.md`, `AGENTS.md`, `HelpView`, testy `ModuleKeyTests` |
| Nowy moduł / widok | `tui/overview.md`, `tui/views-checklist.md`, `AGENTS.md` |
| Tryb graficzny | `tui/graphics.md` |
| Apple 1 I/O | `machines/apple1.md`, `AGENTS.md` |
