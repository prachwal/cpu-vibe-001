# Component Checklist

Common lifecycle for every terminal component.

## Before creation / Activate()

- [ ] **Clear target area** — `TermArea.Clear(renderer, area)` removes old content
- [ ] **Calculate position** — `TermRect.CenterFrame(contentW, contentH)` or manual layout
- [ ] **Draw frame** — `TermFrame.Draw(renderer, frame, style, title?)`
- [ ] **Seed content** — fill PixelCanvas / prepare data (naiveive, once)
- [ ] **Render** — write cells inside the inner `frame.Inner` rect

## After finish / Deactivate()

- [ ] **Clear area** — `TermArea.Clear(renderer, area)` — eliminates artifacts
- [ ] **Reset dirty state** — set `_fullRedraw = true` in parent App

## ITermView interface (all views must implement)

| Method | Must do |
|--------|---------|
| `Activate(r, area)` | Seed content if needed + full Render |
| `Render(r, area)` | Draw current state into area |
| `Deactivate(r, area)` | Clear area (TermArea.Clear) |

## BaseTermView (abstract)

To reduce boilerplate, all concrete views should extend a base class
that handles the common pattern:

```csharp
public abstract class BaseTermView : ITermView
{
    public abstract string Name { get; }
    protected bool _seeded;

    public virtual void Activate(ITerminalRenderer r, TermRect area)
    { if (!_seeded) { Seed(); _seeded = true; } Render(r, area); }

    public virtual void Deactivate(ITerminalRenderer r, TermRect area)
    { TermArea.Clear(r, area); }

    public abstract void Render(ITerminalRenderer r, TermRect area);
    protected abstract void Seed();
}
```

## App rendering responsibility

The `Render(TerminalLayout, bool)` method in App.Render.cs MUST:

- [ ] Call `renderer.Clear` if `fullRedraw`
- [ ] Dispatch to the active view OR inline render method
- [ ] Call `RenderFunctionBar(layout)`
- [ ] Call `renderer.Flush()`

## Current component audit

| Component | Has frame | Clears on Deactivate | Seeded | Uses ITermView | BaseTermView | Pass |
|-----------|-----------|---------------------|--------|---------------|-------------|------|
| `ScreenView` | ✅ | ✅ (via BaseTermView) | N/A | ✅ | ✅ | ✅ |
| `CanvasView` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `RenderScreen` (inline) | ✅ | ❌ (no lifecycle) | N/A | ❌ | ❌ | ❌ |
| `RenderImage` (inline) | ✅ | ❌ (no lifecycle) | N/A | ❌ | ❌ | ❌ |
| `RenderHelp` (inline) | ❌ | ❌ (no lifecycle) | N/A | ❌ | ❌ | ❌ |
| `RenderDemoMenu` (inline) | ❌ | ❌ (no lifecycle) | N/A | ❌ | ❌ | ❌ |
| `RenderFunctionBar` (inline) | ❌ | ❌ (no lifecycle) | N/A | ❌ | ❌ | ❌ |

## Migration status

- [x] `BaseTermView` — abstract class with Activate/Deactivate/Render + Seed
- [x] `CanvasView` — extends BaseTermView, full lifecycle
- [x] `ScreenView` — extends BaseTermView, dynamic size
- [ ] `ImageView` — BaseTermView stub (needs JpegImageLoader)
- [ ] `HelpView` — BaseTermView stub
- [ ] `DemoMenuView` — BaseTermView stub
- [ ] `RenderScreen` → `ScreenView` via TermViewManager
- [ ] `RenderImage` → `ImageView`
- [ ] `RenderHelp` → `HelpView`
- [ ] `RenderDemoMenu` → `DemoMenuView`
- [x] `TermViewManager` — resize-safe
- [x] `TermArea` — static helpers (Clear, Write, Centered, Canvas, Screen)
