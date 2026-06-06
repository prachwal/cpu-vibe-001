using FluentAssertions;
using Xunit;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Tui.Tests;

public class CanvasViewTests
{
    [Fact]
    public void Activate_SeedsCanvas()
    {
        var renderer = new FakeTerminalRenderer(80, 25);
        var view = new CanvasView();

        view.Activate(renderer, new TermRect(0, 0, 80, 25));

        // Canvas should have some content now
        bool hasContent = false;
        for (int y = 0; y < 200 && !hasContent; y++)
            for (int x = 0; x < 320 && !hasContent; x++)
                if (view.Canvas.GetPixel(x, y) != Pixel.Black)
                    hasContent = true;
        hasContent.Should().BeTrue("canvas should have non-black pixels after seed");
    }

    [Fact]
    public void Deactivate_ClearsTerminalArea()
    {
        var renderer = new FakeTerminalRenderer(20, 10);
        var view = new CanvasView();
        var area = new TermRect(0, 0, 20, 10);

        // Activate → draws something
        view.Activate(renderer, area);
        // Deactivate → clears area
        view.Deactivate(renderer, area);

        // All cells should be space (cleared)
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 20; x++)
                renderer.GetCell(x, y).Ch.Should().Be(' ', $"cell ({x},{y}) should be space after deactivate");
    }

    [Fact]
    public void CycleMode_ChangesMode()
    {
        var view = new CanvasView();
        var original = view.Mode;

        view.CycleMode();
        view.Mode.Should().NotBe(original);
    }

    [Fact]
    public void NextPrevDemo_CyclesDemos()
    {
        var view = new CanvasView();
        int d0 = view.DemoIndex;

        view.NextDemo();
        view.DemoIndex.Should().NotBe(d0);

        view.PrevDemo();
        view.DemoIndex.Should().Be(d0);
    }

    [Fact]
    public void Tick3D_DoesNotThrow()
    {
        var view = new CanvasView();
        var renderer = new FakeTerminalRenderer(80, 25);

        view.Activate(renderer, new TermRect(0, 0, 80, 25));
        Action act = () => { for (int i = 0; i < 10; i++) view.Tick3D(); };
        act.Should().NotThrow();
    }
}
