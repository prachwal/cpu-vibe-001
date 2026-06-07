using Cpu.Chips.Vic20;
using Cpu.Vic20.System;
using Cpu.Vic20.Video;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Cpu.Vic20.Tests;

public class Vic20BootTests
{
    private readonly ITestOutputHelper _output;

    public Vic20BootTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Boot_ReachesBasicReady_AndScreenHasText()
    {
        using var machine = Vic20Machine.Load("vic20-ntsc.json");
        machine.Run(5_000_000);

        var vic = machine.Vic.Chip;
        int screenAddr = vic.ScreenAddr;
        int cols = vic.Columns;
        int rows = vic.Rows;

        _output.WriteLine($"PC=${machine.Board.Cpu.Regs.PC:X4}");
        _output.WriteLine($"VIC cols={cols} rows={rows} screen=${screenAddr:X4} scrCol={vic.ScreenColor:X}");
        _output.WriteLine($"VIC r2=${vic[2]:X2} r3=${vic[3]:X2} r5=${vic[5]:X2} rF=${vic[0xF]:X2}");

        cols.Should().BeGreaterThan(0, "KERNAL should configure VIC columns");
        rows.Should().BeGreaterThan(0, "KERNAL should configure VIC rows");
        screenAddr.Should().BeGreaterThan(0, "KERNAL should configure screen address");

        int textChars = CountNonSpaceScreenChars(machine, screenAddr, cols * rows);
        _output.WriteLine($"Non-space screen chars: {textChars}");
        textChars.Should().BeGreaterThan(10, "BASIC startup banner should appear on screen");

        machine.RenderVideo();
        byte[] pixels = machine.Video.Pixels.ToArray();
        pixels.Distinct().Count().Should().BeGreaterOrEqualTo(2, "framebuffer needs ink and paper colors");
        pixels.Count(p => p != 0).Should().BeGreaterThan(100, "startup text should paint visible pixels");
    }

    [Fact]
    public void Print1_DisplaysDigitOnScreen()
    {
        using var machine = Vic20Machine.Load("vic20-ntsc.json");
        machine.Run(5_000_000);

        TypeLine(machine, "PRINT 1");
        PressReturn(machine);
        machine.Run(500_000);

        var vic = machine.Vic.Chip;
        int screenAddr = vic.ScreenAddr;
        int cols = vic.Columns;
        if (cols == 0)
            cols = Vic20Video.MaxCharsPerRow;

        _output.WriteLine($"PC=${machine.Board.Cpu.Regs.PC:X4}");
        _output.WriteLine($"Screen at ${screenAddr:X4}, cols={cols}");

        bool hasOne = ScreenContainsChar(machine, screenAddr, cols, 50, (byte)'1');
        _output.WriteLine($"Screen contains '1' (0x31): {hasOne}");

        hasOne.Should().BeTrue("PRINT 1 should write digit 1 to screen RAM");

        machine.RenderVideo();
        bool pixelHasInk = FrameContainsCharGlyph(machine, (byte)'1');
        _output.WriteLine($"Framebuffer renders '1' glyph: {pixelHasInk}");
        pixelHasInk.Should().BeTrue("rendered frame should show digit 1 pixels");
    }

    [Fact]
    public void CursorCell_TogglesDuringIrqFlash()
    {
        using var machine = Vic20Machine.Load("vic20-ntsc.json");
        machine.Run(5_000_000);

        ushort cursorAddr = machine.GetCursorScreenAddress();
        cursorAddr.Should().BeGreaterOrEqualTo((ushort)0x1000);

        var bus = machine.Board.Bus;
        var seen = new HashSet<byte>();
        for (int i = 0; i < 40; i++)
        {
            seen.Add(bus.Read(cursorAddr));
            machine.Run(100_000);
        }

        _output.WriteLine($"Cursor @ ${cursorAddr:X4}, samples: {string.Join(' ', seen.Select(b => $"{b:X2}"))}");
        seen.Count.Should().BeGreaterThan(1, "KERNAL cursor flash should toggle screen code at cursor");
        seen.Should().Contain(new byte[] { 0x20, 0xA0 });
    }

    private static int CountNonSpaceScreenChars(Vic20Machine machine, int screenAddr, int count)
    {
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            byte ch = machine.Board.Bus.Read((ushort)(screenAddr + i));
            if (ch != 0 && ch != 0x20)
                n++;
        }
        return n;
    }

    private static bool ScreenContainsChar(Vic20Machine machine, int screenAddr, int cols, int maxRows, byte ch)
    {
        for (int row = 0; row < maxRows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (machine.Board.Bus.Read((ushort)(screenAddr + row * cols + col)) == ch)
                    return true;
            }
        }
        return false;
    }

    private static bool FrameContainsCharGlyph(Vic20Machine machine, byte charCode)
    {
        var vic = machine.Vic.Chip;
        int charAddr = vic.CharAddr;
        byte[] glyphInk = new byte[Vic20Video.CharWidth * Vic20Video.CharWidth];
        for (int y = 0; y < 8; y++)
        {
            byte row = machine.Board.Bus.Read((ushort)(charAddr + charCode * 8 + y));
            for (int x = 0; x < Vic20Video.CharWidth; x++)
            {
                if ((row & (1 << (7 - x))) != 0)
                    glyphInk[y * Vic20Video.CharWidth + x] = 1;
            }
        }

        ReadOnlySpan<byte> pixels = machine.Video.Pixels;
        int width = Vic20Video.MaxWidth;
        for (int py = 0; py < Vic20Video.MaxHeight - 8; py++)
        {
            for (int px = 0; px < Vic20Video.MaxWidth - Vic20Video.CharWidth; px++)
            {
                if (MatchesGlyphAt(pixels, width, px, py, glyphInk))
                    return true;
            }
        }
        return false;
    }

    private static bool MatchesGlyphAt(ReadOnlySpan<byte> pixels, int width, int px, int py, byte[] glyphInk)
    {
        int inkCount = 0;
        int matchCount = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < Vic20Video.CharWidth; x++)
            {
                if (glyphInk[y * Vic20Video.CharWidth + x] == 0)
                    continue;
                inkCount++;
                byte pixel = pixels[(py + y) * width + px + x];
                if (pixel != 0)
                    matchCount++;
            }
        }
        return inkCount >= 4 && matchCount >= inkCount * 3 / 4;
    }

    private static void TypeLine(Vic20Machine machine, string text)
    {
        foreach (char ch in text)
        {
            if (!Vic20KeyInput.TryGetMatrixCell(ch, out int row, out int col))
                throw new InvalidOperationException($"No matrix mapping for '{ch}'");

            TapKey(machine, row, col);
        }
    }

    private static void PressReturn(Vic20Machine machine) => TapKey(machine, 3, 0);

    private static void TapKey(Vic20Machine machine, int row, int col)
    {
        machine.PressKey(row, col);
        machine.Run(200_000);
        machine.ReleaseKey(row, col);
        machine.Run(50_000);
    }
}

internal static class Vic20KeyInput
{
    private static readonly (char Ch, int Row, int Col)[] Map =
    [
        ('P', 2, 0), ('R', 2, 2), ('I', 1, 1), ('N', 1, 6), ('T', 2, 4),
        (' ', 4, 0), ('1', 6, 1),
        ('0', 6, 0), ('2', 6, 2), ('3', 6, 3), ('4', 6, 4),
        ('5', 6, 5), ('6', 6, 6), ('7', 6, 7),
        ('8', 7, 0), ('9', 7, 1),
    ];

    public static bool TryGetMatrixCell(char ch, out int row, out int col)
    {
        foreach ((char c, int r, int co) in Map)
        {
            if (c == ch)
            {
                row = r;
                col = co;
                return true;
            }
        }
        row = 0;
        col = 0;
        return false;
    }
}
