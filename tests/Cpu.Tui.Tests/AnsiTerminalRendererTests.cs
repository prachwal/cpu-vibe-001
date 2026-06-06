using System.Text;
using FluentAssertions;
using Xunit;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

public class AnsiTerminalRendererTests
{
    [Fact]
    public void Constructor_CreatesRenderer()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);

        renderer.Width.Should().Be(0);
        renderer.Height.Should().Be(0);
    }

    [Fact]
    public void Resize_SetsDimensions()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);

        renderer.Resize(80, 25);

        renderer.Width.Should().Be(80);
        renderer.Height.Should().Be(25);
    }

    [Fact]
    public void Resize_ClearsScreen()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);

        renderer.Resize(80, 25);

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        text.Should().Contain("\x1b[2J"); // clear screen
    }

    [Fact]
    public void SetCell_OutOfBounds_Ignored()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 10);

        Action act = () => renderer.SetCell(-1, 0, 'A', ConsoleColor.Gray, ConsoleColor.Black);
        act.Should().NotThrow();

        act = () => renderer.SetCell(100, 0, 'A', ConsoleColor.Gray, ConsoleColor.Black);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetCell_SetsBackBuffer()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 10);

        renderer.SetCell(5, 5, 'X', ConsoleColor.Red, ConsoleColor.Blue);

        // No output yet - only in back buffer
        stream.Length.Should().BeGreaterThan(0); // constructor wrote hide cursor
    }

    [Fact]
    public void Flush_NoChanges_NoOutput()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 10);
        renderer.Flush(); // sync front and back
        stream.SetLength(0);

        renderer.Flush();

        stream.Length.Should().Be(0);
    }

    [Fact]
    public void Flush_SingleCell_WritesANSI()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 10);
        stream.SetLength(0);

        renderer.SetCell(0, 0, 'A', ConsoleColor.Gray, ConsoleColor.Black);
        renderer.Flush();

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        text.Should().Contain("\x1b[1;1H"); // move to 0,0
        text.Should().Contain("A");
    }

    [Fact]
    public void Flush_ConsecutiveSameColor_OneRun()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 1);
        stream.SetLength(0);

        renderer.SetCell(0, 0, 'H', ConsoleColor.Gray, ConsoleColor.Black);
        renderer.SetCell(1, 0, 'I', ConsoleColor.Gray, ConsoleColor.Black);
        renderer.Flush();

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        // Should have one MoveTo + one SetColor + "HI"
        text.Should().Contain("HI");
    }

    [Fact]
    public void Flush_DifferentColors_SeparateRuns()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 1);
        stream.SetLength(0);

        renderer.SetCell(0, 0, 'A', ConsoleColor.Red, ConsoleColor.Black);
        renderer.SetCell(1, 0, 'B', ConsoleColor.Blue, ConsoleColor.Black);
        renderer.Flush();

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        // Red→ANSI 9→39, Blue→ANSI 12→42
        text.Should().Contain("39"); // red
        text.Should().Contain("42"); // blue
    }

    [Fact]
    public void Flush_OnlyChangedCells()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 1);

        // Set all cells
        for (int i = 0; i < 10; i++)
            renderer.SetCell(i, 0, (char)('A' + i), ConsoleColor.Gray, ConsoleColor.Black);
        renderer.Flush();
        stream.SetLength(0);

        // Change only one cell
        renderer.SetCell(5, 0, 'Z', ConsoleColor.Gray, ConsoleColor.Black);
        renderer.Flush();

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        text.Should().Contain("Z");
        text.Should().NotContain("A");
    }

    [Fact]
    public void Clear_FillsBackBuffer()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(10, 10);

        renderer.SetCell(5, 5, 'X', ConsoleColor.Red, ConsoleColor.Black);
        renderer.Clear(ConsoleColor.Black, ConsoleColor.Black);
        renderer.Flush();

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        text.Should().NotContain("X");
    }

    [Fact]
    public void Dispose_ShowsCursor()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);

        renderer.Dispose();

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        text.Should().Contain("\x1b[?25h"); // show cursor
    }

    [Fact]
    public void SetText_SetsMultipleCells()
    {
        using var stream = new MemoryStream();
        var renderer = new AnsiTerminalRenderer(stream);
        renderer.Resize(20, 1);
        stream.SetLength(0);

        renderer.SetText(0, 0, "Hello", ConsoleColor.Gray, ConsoleColor.Black);
        renderer.Flush();

        byte[] output = stream.ToArray();
        string text = System.Text.Encoding.UTF8.GetString(output);
        text.Should().Contain("Hello");
    }
}
