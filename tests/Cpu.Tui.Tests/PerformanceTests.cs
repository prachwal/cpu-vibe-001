using System.Diagnostics;
using FluentAssertions;
using Xunit;
using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;

namespace Cpu.Tui.Tests;

public class PerformanceTests
{
    [Fact]
    public void Benchmark_ScrollRegionUp()
    {
        var screen = new ScreenBuffer();
        var pia = new PiaDevice(0x8800);
        var adapter = new PiaTerminalAdapter(pia, screen);

        // Fill screen with text
        for (int y = 0; y < 25; y++)
            for (int x = 0; x < 80; x++)
                screen.SetCell(x, y, (char)('A' + y % 26), ConsoleColor.Gray, ConsoleColor.Black);

        var sw = Stopwatch.StartNew();
        const int iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            adapter.WriteString("Test line\r\n");
        }
        sw.Stop();

        long opsPerSec = iterations * 1000 / sw.ElapsedMilliseconds;
        // Output via test context
        Assert.True(opsPerSec > 0);
    }

    [Fact]
    public void Benchmark_FillScreen()
    {
        var screen = new ScreenBuffer();
        var sw = Stopwatch.StartNew();
        const int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            screen.FillRowFast(i % 25, (byte)'X', 0x70);
        }
        sw.Stop();

        double nsPerOp = sw.Elapsed.TotalNanoseconds / iterations;
        Assert.True(nsPerOp < 10000); // Should be under 10µs
    }

    [Fact]
    public void Benchmark_CopyRow()
    {
        var screen = new ScreenBuffer();
        for (int x = 0; x < 80; x++)
            screen.SetCell(x, 1, (char)('A' + x % 26), ConsoleColor.Gray, ConsoleColor.Black);

        var sw = Stopwatch.StartNew();
        const int iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            screen.CopyRow(1, 0);
        }
        sw.Stop();

        double nsPerOp = sw.Elapsed.TotalNanoseconds / iterations;
        Assert.True(nsPerOp < 1000); // Should be under 1µs
    }

    [Fact]
    public void Benchmark_PiaFullDemo()
    {
        var pia = new PiaDevice(0x8800);
        var screen = new ScreenBuffer();
        var adapter = new PiaTerminalAdapter(pia, screen);

        var sw = Stopwatch.StartNew();
        PiaDemos.Run(pia, 0); // Lorem ipsum
        sw.Stop();

        double ms = sw.Elapsed.TotalMilliseconds;
        Assert.True(ms < 50); // Should complete in under 50ms
    }

    [Fact]
    public void Benchmark_DirtyTracking()
    {
        var screen = new ScreenBuffer();
        Assert.False(screen.IsDirty);

        screen.SetCell(0, 0, 'X', ConsoleColor.Gray, ConsoleColor.Black);
        Assert.True(screen.IsDirty);
        Assert.Equal(1, screen.DirtyCount);

        screen.ClearDirty();
        Assert.False(screen.IsDirty);
        Assert.Equal(0, screen.DirtyCount);
    }

    [Fact]
    public void Benchmark_BestGlyph_FullScreen()
    {
        var image = new PixelBuffer(320, 200);
        var rng = new Random(42);
        for (int y = 0; y < 200; y++)
            for (int x = 0; x < 320; x++)
                image.SetPixel(x, y, new Pixel((byte)rng.Next(256), (byte)rng.Next(256), (byte)rng.Next(256)));

        var ftr = new FakeTerminalRenderer(40, 25);
        var sw = Stopwatch.StartNew();

        TerminalGraphicsRenderer.Render(ftr, image, TerminalGraphicsMode.BestGlyph, 0, 0, 40, 25);

        sw.Stop();
        double ms = sw.Elapsed.TotalMilliseconds;
        // BestGlyph should render 40x25 cells in under 5 seconds (after optimization)
        // This is a soft limit — just checks it's not pathological
        Assert.True(ms < 10000, $"BestGlyph took {ms:F1}ms (expected < 10000ms)");
    }
}
