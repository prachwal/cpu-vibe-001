# Key Action Map

## Module Tree

```
MainMenuModule (root)
  ├── F1  HelpModule
  ├── F2  ScreenModule
  ├── F3  ScreenModule (echo)
  ├── F4  DemoMenuModule
  │       └── Enter  DemoPlayerModule
  ├── F7  ImageModule
  ├── F8  Apple1Module
  └── F9  CanvasModule
```

## Key Dispatch Rules

```
App.ReadInput()
  └─ key handled? → ModuleManager.OnKey()
       └─ MainMenuModule.OnKey()
            ├─ child exists?
            │    ├─ child.OnKey(key) → handled? → return true
            │    └─ child returned false → MainMenuModule fallback keys
            └─ no child → MainMenuModule keys directly
```

Keys flow from leaf → parent → root. First handler that returns `true` wins.

---

## Esc — per-module behavior

| Module | Esc behavior |
|--------|-------------|
| **MainMenu** (menu mode) | Quit application |
| **Help / Screen / Image / Canvas** | Return to main menu |
| **Demo Menu** (no player) | Return to main menu |
| **Demo Menu** (player open) | Close player, stay in demo list |
| **DemoPlayer** (playing) | Ignored (Esc during playback does nothing) |
| **DemoPlayer** (ended) | Return to demo list |
| **Apple 1** | Return to main menu |

---

## Per-module Key Map

### MainMenuModule (menu mode)

| Key | Action |
|-----|--------|
| `↑` `↓` | Select module |
| `Enter` | Open selected module |
| `F1` | Open Help |
| `F4` | Open Demo Menu |
| `F7` | Open Image |
| `F8` | Open Apple 1 |
| `F9` | Open Canvas |
| `F12` | Toggle error panel |
| `Esc` | Quit application |

### HelpModule

No keys handled. `Esc` → main menu.

### ScreenModule

| Key | Action |
|-----|--------|
| `F2` | Cycle screen size: 25×80 → 24×40 → 25×40 |
| `F3` | Toggle echo mode |
| `F5` | (refresh, consumed) |
| `F6` | Toggle frame style ASCII/Unicode |
| `Esc` | Main menu |

### DemoMenuModule (demo list)

| Key | Action |
|-----|--------|
| `↑` `↓` | Select demo |
| `Enter` | Start selected demo |
| `Esc` | Main menu |

### DemoPlayerModule (playing)

| Key | Action |
|-----|--------|
| `P` | Pause / resume |
| `+` | Speed up (max 10×) |
| `-` | Speed down (min 1×) |
| `Esc` | (ignored while playing) |

### DemoPlayerModule (ended)

| Key | Action |
|-----|--------|
| `Esc` | Return to demo list |

### ImageModule

| Key | Action |
|-----|--------|
| `←` | Previous image |
| `→` `F7` | Next image |
| `↑` `↓` `F8` | Cycle graphics mode |
| `Esc` | Main menu |

### CanvasModule

| Key | Action |
|-----|--------|
| `←` | Previous demo |
| `→` | Next demo |
| `F10` | Cycle graphics mode |
| `W` `A` `S` `D` | 3D rotate (demo 2 only) |
| `↑` `↓` | 3D zoom (demo 2 only) |
| `Esc` | Main menu |

### Apple1Module

| Key | Action |
|-----|--------|
| `↑` `↓` `F10` | Switch profile (Woz Monitor ↔ BASIC) |
| `Enter` | Send CR (`\r`) to emulated CPU |
| `Backspace` | Send BS (`\b`) to emulated CPU |
| `0-9` `A-Z` `a-z` etc. | Send character to emulated CPU |
| `Esc` | Main menu |
| `F1` `F4` `F7` `F8` `F9` | Switch to another module |

---

## Consistency Check

| Feature | Status |
|---------|--------|
| **Can always return to parent?** | ✅ Yes — `Esc` propagates up the chain |
| **Can always reach main menu?** | ✅ Yes — each leaf returns `false` for Esc |
| **F-keys work from any module?** | ✅ Yes — F1/F4/F7/F8/F9 are handled by MainMenuModule fallback |
| **Can't get stuck?** | ✅ No module consumes all keys (Apple1 was fixed) |
| **DemoPlayer playing → can't Esc?** | ✅ Intentional — prevents accidental exit during playback |
| **DemoPlayer ended → must Esc?** | ✅ Shows "Esc to return" before allowing exit |
