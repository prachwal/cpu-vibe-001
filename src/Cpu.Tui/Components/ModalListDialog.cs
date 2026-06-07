using Cpu.Tui.Rendering;

namespace Cpu.Tui.Components;

public sealed class ModalListDialog
{
    private readonly string _title;
    private readonly string[] _items;
    private int _itemWidth;

    public int Selected { get; private set; }
    public bool IsOpen { get; private set; }
    public int? Result { get; set; }

    public ModalListDialog(string title, string[] items)
    {
        _title = title;
        _items = items;
        _itemWidth = title.Length;
        foreach (var item in items)
            if (item.Length > _itemWidth)
                _itemWidth = item.Length;
    }

    public void Open(int selectedIndex = 0)
    {
        IsOpen = true;
        Selected = selectedIndex;
        Result = null;
    }

    public void Close() => IsOpen = false;

    public bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsOpen) return false;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                Selected = (Selected - 1 + _items.Length) % _items.Length;
                return true;
            case ConsoleKey.DownArrow:
                Selected = (Selected + 1) % _items.Length;
                return true;
            case ConsoleKey.Enter:
                Result = Selected;
                IsOpen = false;
                return true;
            case ConsoleKey.Escape:
            case ConsoleKey.F1:
            case ConsoleKey.F2:
            case ConsoleKey.F4:
            case ConsoleKey.F5:
            case ConsoleKey.F6:
            case ConsoleKey.F7:
            case ConsoleKey.F8:
            case ConsoleKey.F9:
            case ConsoleKey.F10:
            case ConsoleKey.F11:
            case ConsoleKey.F12:
                IsOpen = false;
                return true;
            default:
                return false;
        }
    }

    public void Render(ITerminalRenderer r, int screenW, int screenH)
    {
        if (!IsOpen) return;

        int dialogW = Math.Max(30, _itemWidth + 6);
        int dialogH = Math.Min(_items.Length + 4, screenH - 4);
        int x0 = Math.Max(0, (screenW - dialogW) / 2);
        int y0 = Math.Max(0, (screenH - dialogH) / 2);
        int visibleCount = dialogH - 4;

        for (int dy = 0; dy < dialogH; dy++)
        {
            for (int dx = 0; dx < dialogW; dx++)
            {
                int rx = x0 + dx;
                int ry = y0 + dy;
                if (rx >= screenW || ry >= screenH) continue;

                bool isBorder = dy == 0 || dy == dialogH - 1 || dx == 0 || dx == dialogW - 1;
                if (isBorder)
                {
                    char edge = dy switch
                    {
                        0 when dx == 0 => '\u250C',
                        0 when dx == dialogW - 1 => '\u2510',
                        _ when dy == dialogH - 1 && dx == 0 => '\u2514',
                        _ when dy == dialogH - 1 && dx == dialogW - 1 => '\u2518',
                        _ when dy == 0 || dy == dialogH - 1 => '\u2500',
                        _ => '\u2502'
                    };
                    r.SetCell(rx, ry, edge, ConsoleColor.White, ConsoleColor.DarkBlue);
                }
                else if (dy == 1)
                {
                    r.SetCell(rx, ry, dx >= 1 && dx - 1 < _title.Length ? _title[dx - 1] : ' ',
                        ConsoleColor.Cyan, ConsoleColor.DarkBlue);
                }
                else if (dy >= 2 && dy < 2 + visibleCount)
                {
                    int itemIdx = dy - 2;
                    bool sel = itemIdx == Selected;
                    char prefix = sel ? '\u25B6' : ' ';
                    string label = itemIdx < _items.Length ? _items[itemIdx] : "";
                    char ch = dx switch
                    {
                        0 => prefix,
                        1 => ' ',
                        _ => dx - 2 < label.Length ? label[dx - 2] : ' '
                    };
                    var fg = sel ? ConsoleColor.Black : ConsoleColor.Gray;
                    var bg = sel ? ConsoleColor.White : ConsoleColor.DarkBlue;
                    r.SetCell(rx, ry, ch, fg, bg);
                }
                else
                {
                    r.SetCell(rx, ry, ' ', ConsoleColor.Gray, ConsoleColor.DarkBlue);
                }
            }
        }
    }
}
