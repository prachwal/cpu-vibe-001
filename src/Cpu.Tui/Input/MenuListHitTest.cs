namespace Cpu.Tui.Input;

public static class MenuListHitTest
{
    public static int ComputeStartRow(int areaHeight, int itemCount, int headerLines = 2)
    {
        return Math.Max(headerLines, (areaHeight - itemCount - headerLines) / 2) + headerLines;
    }

    public static bool TryHitRow(int x, int y, int areaHeight, int itemCount, int startRow, int minX, out int index)
    {
        index = -1;
        if (x < minX || y < startRow || y >= startRow + itemCount)
            return false;
        index = y - startRow;
        return true;
    }
}
