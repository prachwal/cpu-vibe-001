using Cpu.Module;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Input;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Modules;

public sealed class MainMenuModule : IAppModule
{
    private readonly List<IAppModule> _items;
    private readonly ErrorCollector? _errors;
    private int _selected;
    private IAppModule? _child;
    private bool _showErrors;
    private int _lastRenderH;

    public string Name => "Main Menu";
    public IAppModule? Parent { get; set; }
    public IAppModule? Child => _child;
    public bool IsActive { get; private set; }

    public MainMenuModule(IEnumerable<IAppModule> items, ErrorCollector? errors = null)
    {
        _items = items.ToList();
        _errors = errors;
    }

    public void SetChild(IAppModule? child)
    {
        _child = child;
        if (child != null) child.Parent = this;
    }

    public void OnActivate()
    {
        IsActive = true;
        _child?.OnActivate();
    }

    public void OnDeactivate()
    {
        IsActive = false;
        if (_child != null) { _child.OnDeactivate(); _child.Parent = null; _child = null; }
    }

    private void CloseChild()
    {
        if (_child == null) return;
        _child.OnDeactivate();
        _child.Parent = null;
        _child = null;
    }

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (_child != null)
        {
            if (_child.OnKey(key))
                return true;
            if (key.Key == ConsoleKey.Escape)
            {
                CloseChild();
                return true;
            }
            switch (key.Key)
            {
                case ConsoleKey.F1: return SwitchTo("Help");
                case ConsoleKey.F2: return SwitchTo("Setup");
                case ConsoleKey.F4: return SwitchTo("Demo Menu");
                case ConsoleKey.F7: return SwitchTo("Image");
                case ConsoleKey.F8: return SwitchTo("Apple 1");
                case ConsoleKey.F9: return SwitchTo("Canvas");
                case ConsoleKey.F11: return SwitchTo("Commodore PET");
                case ConsoleKey.F6: return SwitchTo("Commodore VIC-20");
            }
            return false;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                _selected = (_selected - 1 + _items.Count) % _items.Count;
                return true;
            case ConsoleKey.DownArrow:
                _selected = (_selected + 1) % _items.Count;
                return true;
            case ConsoleKey.Enter:
                OpenChild(_items[_selected]);
                return true;
            case ConsoleKey.F1: return SwitchTo("Help");
            case ConsoleKey.F2: return SwitchTo("Setup");
            case ConsoleKey.F4: return SwitchTo("Demo Menu");
            case ConsoleKey.F7: return SwitchTo("Image");
            case ConsoleKey.F8: return SwitchTo("Apple 1");
            case ConsoleKey.F9: return SwitchTo("Canvas");
            case ConsoleKey.F11: return SwitchTo("Commodore PET");
            case ConsoleKey.F6: return SwitchTo("Commodore VIC-20");
            case ConsoleKey.F12:
                _showErrors = !_showErrors;
                return true;
        }
        return false;
    }

    public bool OnMouse(MouseEvent e)
    {
        if (e.IsMotion || e.IsRelease || e.Button != 0 || _child != null)
            return false;

        if (!TryHitMenuItem(e.X, e.Y, out int index))
            return false;

        _selected = index;
        OpenChild(_items[index]);
        return true;
    }

    public bool OnTick() => _child?.OnTick() ?? false;

    public void OnRender(ITerminalRenderer r, int w, int h)
    {
        _lastRenderH = h;

        if (_child != null)
        {
            _child.OnRender(r, w, h);
            string bar = "Esc back";
            TermArea.Write(r, new TermRect(0, 0, w, h), 0, h - 1, bar, ConsoleColor.DarkGray, ConsoleColor.Black);
            return;
        }

        var area = new TermRect(0, 0, w, h - 1);

        for (int row = 0; row < h - 1; row++)
            for (int col = 0; col < w; col++)
                r.SetCell(col, row, ' ', ConsoleColor.Gray, ConsoleColor.Black);

        int startY = MenuStartY(h);

        TermArea.Write(r, area, 1, startY - 2, "CPU-VIBE-001", ConsoleColor.Cyan, ConsoleColor.Black);

        for (int i = 0; i < _items.Count; i++)
        {
            bool sel = i == _selected;
            var m = _items[i];
            string label = sel ? $"> {m.Name}" : $"  {m.Name}";
            var fg = sel ? ConsoleColor.Black : ConsoleColor.Gray;
            var bg = sel ? ConsoleColor.White : ConsoleColor.Black;
            TermArea.Write(r, area, 1, startY + i, label, fg, bg);
        }
        TermArea.Write(r, area, 1, startY + _items.Count + 1,
            "Enter select  F2 setup  F1/4/7/8/9/11  F12 errors", ConsoleColor.DarkGray, ConsoleColor.Black);

        if (_showErrors && _errors != null)
            RenderErrorPanel(r, w, h - 1);
    }

    private int MenuStartY(int h) => Math.Max(2, (h - _items.Count) / 2);

    private bool TryHitMenuItem(int x, int y, out int index)
    {
        index = -1;
        int startY = MenuStartY(_lastRenderH);
        if (x < 1 || y < startY || y >= startY + _items.Count)
            return false;
        index = y - startY;
        return true;
    }

    private void RenderErrorPanel(ITerminalRenderer r, int w, int h)
    {
        int panelH = Math.Min(10, h - 3);
        int panelY = h - panelH;
        var bg = ConsoleColor.DarkRed;
        var fg = ConsoleColor.White;

        for (int y = panelY; y < h; y++)
            for (int x = 0; x < w; x++)
                r.SetCell(x, y, ' ', fg, bg);

        var errors = _errors!.GetErrors();
        int count = errors.Length;
        TermArea.Write(r, new TermRect(0, 0, w, h), 0, panelY,
            $" Errors: {count}  (F12 hide)", ConsoleColor.Yellow, bg);

        int start = Math.Max(0, count - panelH + 1);
        for (int i = 0; i < panelH - 1 && start + i < count; i++)
        {
            string line = errors[start + i];
            if (line.Length > w) line = line[..w];
            TermArea.Write(r, new TermRect(0, 0, w, h), 0, panelY + 1 + i, line, fg, bg);
        }
    }

    private void OpenChild(IAppModule module)
    {
        CloseChild();
        _child = module;
        module.Parent = this;
        module.OnActivate();
    }

    private bool SwitchTo(string name)
    {
        var m = _items.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (m == null || m == _child) return false;
        OpenChild(m);
        return true;
    }
}
