using FluentAssertions;
using Xunit;
using Cpu.Canvas.Rendering;
using Cpu.Canvas.Rendering.Views;
using Cpu.Tui;
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
                renderer.GetCell(x, y).Should().Be(TerminalCell.Black, $"cell ({x},{y}) should be canonical black after deactivate");
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
    public void ApplySettings_PreservesCycledGraphicsMode()
    {
        var view = new CanvasView();
        view.CycleMode();
        TerminalGraphicsMode cycled = view.Mode;

        view.ApplySettings(new TuiAppSettings
        {
            DefaultGraphicsMode = TerminalGraphicsMode.HalfBlockColor,
            FrameStyle = FrameStyle.Unicode
        });

        view.Mode.Should().Be(cycled);
    }

    [Fact]
    public void MandelbrotDemo_HasNonBlackPixels()
    {
        var view = new CanvasView();
        view.NextDemo();
        view.NextDemo();
        view.NextDemo();
        view.DemoIndex.Should().Be(3);

        var renderer = new FakeTerminalRenderer(80, 25);
        view.Activate(renderer, new TermRect(0, 0, 80, 25));

        bool hasColor = false;
        for (int y = 0; y < PixelCanvas.CanvasHeight && !hasColor; y++)
            for (int x = 0; x < PixelCanvas.CanvasWidth && !hasColor; x++)
                if (view.Canvas.GetPixel(x, y) != Pixel.Black)
                    hasColor = true;
        hasColor.Should().BeTrue();
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

    [Fact]
    public void TermViewManager_SwitchTo_DoesNotOverwriteDeactivateClear()
    {
        var renderer = new FakeTerminalRenderer(10, 5);
        var manager = new TermViewManager(renderer);
        var area = new TermRect(0, 0, 10, 5);

        manager.SwitchTo(new BlackDeactivateView(), area);
        manager.SwitchTo(new EmptyView(), area);

        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 10; x++)
                renderer.GetCell(x, y).Should().Be(TerminalCell.Black);
    }

    private sealed class BlackDeactivateView : ITermView
    {
        public string Name => "BlackDeactivate";
        public void Activate(ITerminalRenderer renderer, TermRect terminalArea) { }
        public void Deactivate(ITerminalRenderer renderer, TermRect terminalArea)
            => TermArea.Clear(renderer, terminalArea, TerminalCell.Black, "BlackDeactivateView");
        public void Render(ITerminalRenderer renderer, TermRect terminalArea) { }
        public void Render(ITerminalRenderer renderer, TermRect terminalArea, PresentationSession session) { }
    }

    private sealed class EmptyView : ITermView
    {
        public string Name => "Empty";
        public void Activate(ITerminalRenderer renderer, TermRect terminalArea) { }
        public void Deactivate(ITerminalRenderer renderer, TermRect terminalArea) { }
        public void Render(ITerminalRenderer renderer, TermRect terminalArea) { }
        public void Render(ITerminalRenderer renderer, TermRect terminalArea, PresentationSession session) { }
    }
}
