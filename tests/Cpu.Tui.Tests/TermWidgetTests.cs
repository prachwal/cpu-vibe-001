using FluentAssertions;
using Xunit;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

public class TermWidgetTests
{
    [Fact]
    public void Center_ComputesCorrectPosition()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        var term = new TermRect(80, 25);

        w.Center(term, 40, 20);
        w.W.Should().Be(42);  // 40+2
        w.H.Should().Be(22);  // 20+2
        w.X.Should().Be(19);  // (80-42)/2
        w.Y.Should().Be(1);   // (25-22)/2

        // Content area
        w.CX.Should().Be(20);
        w.CY.Should().Be(2);
        w.CW.Should().Be(40);
        w.CH.Should().Be(20);
    }

    [Fact]
    public void DrawFrame_DrawsAllBorderCells()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 10, 5);
        w.DrawFrame(FrameStyle.Ascii);

        // Top-left corner
        var tl = renderer.GetCell(w.X, w.Y);
        tl.Ch.Should().Be('+');

        // Top-right corner
        var tr = renderer.GetCell(w.X + w.W - 1, w.Y);
        tr.Ch.Should().Be('+');

        // Bottom-left corner
        var bl = renderer.GetCell(w.X, w.Y + w.H - 1);
        bl.Ch.Should().Be('+');

        // Top edge
        var top = renderer.GetCell(w.X + 1, w.Y);
        top.Ch.Should().Be('-');

        // Left edge
        var left = renderer.GetCell(w.X, w.Y + 1);
        left.Ch.Should().Be('|');
    }

    [Fact]
    public void DrawFrame_Unicode_UsesCorrectGlyphs()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 10, 5);
        w.DrawFrame(FrameStyle.Unicode);

        renderer.GetCell(w.X, w.Y).Ch.Should().Be('┌');
        renderer.GetCell(w.X + w.W - 1, w.Y).Ch.Should().Be('┐');
        renderer.GetCell(w.X, w.Y + w.H - 1).Ch.Should().Be('└');
        renderer.GetCell(w.X + w.W - 1, w.Y + w.H - 1).Ch.Should().Be('┘');
        renderer.GetCell(w.X + 1, w.Y).Ch.Should().Be('─');
        renderer.GetCell(w.X, w.Y + 1).Ch.Should().Be('│');
    }

    [Fact]
    public void DrawFrame_Title_WritesTitle()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 30, 10);
        w.DrawFrame(FrameStyle.Ascii, "Hello");

        renderer.GetCell(w.X, w.Y).Ch.Should().Be('+');           // top-left corner
        renderer.GetCell(w.X + 1, w.Y).Ch.Should().Be('-');       // frame edge
        // Title is " Hello " (space + Hello + space = 7 chars) starting at X+2
        renderer.GetCell(w.X + 2, w.Y).Ch.Should().Be(' ');       // leading space
        renderer.GetCell(w.X + 3, w.Y).Ch.Should().Be('H');
        renderer.GetCell(w.X + 4, w.Y).Ch.Should().Be('e');
        renderer.GetCell(w.X + 7, w.Y).Ch.Should().Be('o');
        renderer.GetCell(w.X + 8, w.Y).Ch.Should().Be(' ');       // trailing space
    }

    [Fact]
    public void SetCell_WritesInsideContent()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 10, 5);

        w.SetCell(0, 0, 'X', ConsoleColor.Red, ConsoleColor.Blue);

        var cell = renderer.GetCell(w.CX, w.CY);
        cell.Ch.Should().Be('X');
    }

    [Fact]
    public void SetText_WritesTextInsideContent()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 20, 5);

        w.SetText(2, 1, "TEST", ConsoleColor.Gray, ConsoleColor.Black);

        renderer.GetCell(w.CX + 2, w.CY + 1).Ch.Should().Be('T');
        renderer.GetCell(w.CX + 5, w.CY + 1).Ch.Should().Be('T');
    }

    [Fact]
    public void Center_PositionChange_ClearsOldArea()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);

        // First position — small
        w.Center(new TermRect(80, 25), 10, 5);
        w.DrawFrame(FrameStyle.Ascii);
        int oldX = w.X, oldY = w.Y, oldW = w.W, oldH = w.H;

        // Second position — larger
        w.Center(new TermRect(80, 25), 40, 20);
        w.DrawFrame(FrameStyle.Ascii);

        // Old frame corner should be cleared (space)
        var oldTl = renderer.GetCell(oldX, oldY);
        oldTl.Should().Be(TerminalCell.Empty, "old position should be cleared");
    }

    [Fact]
    public void DrawTextCentered_CentersLines()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 20, 10);
        w.Clear(ConsoleColor.Gray, ConsoleColor.Black);

        w.DrawTextCentered(["AB", "CD"], ConsoleColor.Gray, ConsoleColor.Black);

        // Lines centered in 20-wide area: "AB" starts at col 9
        renderer.GetCell(w.CX + 9, w.CY + 4).Ch.Should().Be('A');
        renderer.GetCell(w.CX + 9, w.CY + 5).Ch.Should().Be('C');
    }

    [Fact]
    public void DrawCanvas_UsesRenderer()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 10, 5);

        var canvas = new PixelCanvas();
        canvas.SetPixel(10, 10, Pixel.Red);
        w.DrawCanvas(canvas, TerminalGraphicsMode.HalfBlockColor);

        // Content area should have some cells set
        var cell = renderer.GetCell(w.CX, w.CY);
        cell.Ch.Should().NotBe('\0');
    }

    [Fact]
    public void Clear_FillsContentWithBg()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var w = new TermWidget(renderer);
        w.Center(new TermRect(80, 25), 10, 5);

        w.SetCell(0, 0, 'X', ConsoleColor.Red, ConsoleColor.Blue);
        w.Clear(ConsoleColor.Gray, ConsoleColor.Black);

        renderer.GetCell(w.CX, w.CY).Ch.Should().Be(' ');
    }
}
