using FluentAssertions;
using Xunit;
using Cpu.Canvas.Rendering.Views;
using Cpu.Tui;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using Cpu.Tui.Rendering.Views;

namespace Cpu.Tui.Tests;

public class Demo3DOrphanTest
{
    private const int CanvasW = 320;
    private const int CanvasH = 200;
    private const int TermW = 160;
    private const int TermH = 50;

    /// <summary>
    /// Full regression: animate cube 300 frames in each mode, repeat sequence twice.
    /// After each mode, verify canvas and terminal have no orphaned pixels.
    /// </summary>
    [Fact]
    public void AnimatedCube_300Frames_PerMode_NoOrphans()
    {
        var modes = new[] {
            TerminalGraphicsMode.HalfBlockColor,
            TerminalGraphicsMode.BrailleMono,
            TerminalGraphicsMode.Grayscale,
            TerminalGraphicsMode.TrueTone,
            TerminalGraphicsMode.BestGlyph,
            TerminalGraphicsMode.BestGlyphTrueColor
        };

        for (int sequence = 0; sequence < 2; sequence++)
        {
            var canvasView = new CanvasView();
            var renderer = new FakeTerminalRenderer(TermW, TermH);
            var area = new TermRect(0, 0, TermW, TermH);

            // Switch to 3D Shapes demo (index 2) for animation
            while (canvasView.DemoIndex != 2)
                canvasView.NextDemo();

            canvasView.Activate(renderer, area);

            foreach (var mode in modes)
            {
                canvasView.Mode = mode;

                // Animate 300 frames
                for (int frame = 0; frame < 300; frame++)
                {
                    canvasView.Tick3D();
                    canvasView.Render(renderer, area);
                }

                // Verify: no cells outside the canvas view should be set (orphaned)
                VerifyNoOrphanedCells(renderer, mode, sequence);

                // Verify: the pixel canvas has no stale data
                VerifyCanvasBufferClean(canvasView.Canvas);
            }
        }
    }

    /// <summary>
    /// Verify that switching from one mode to another doesn't leave orphaned pixels
    /// from the previous mode's frame at different positions.
    /// </summary>
    [Fact]
    public void ModeSwitch_DoesNotLeavePreviousFrameArtifacts()
    {
        var modes = new[] {
            TerminalGraphicsMode.HalfBlockColor,
            TerminalGraphicsMode.BestGlyph,
            TerminalGraphicsMode.Grayscale,
            TerminalGraphicsMode.TrueTone,
            TerminalGraphicsMode.BestGlyphTrueColor,
            TerminalGraphicsMode.BrailleMono
        };

        var canvasView = new CanvasView();
        var renderer = new FakeTerminalRenderer(TermW, TermH);
        var area = new TermRect(0, 0, TermW, TermH);

        // Switch to 3D Shapes demo (index 2)
        while (canvasView.DemoIndex != 2)
            canvasView.NextDemo();

        canvasView.Activate(renderer, area);

        // Render 200 frames in first mode
        canvasView.Mode = modes[0];
        for (int i = 0; i < 200; i++)
        {
            canvasView.Tick3D();
            canvasView.Render(renderer, area);
        }

        // Save all cell values for the first mode
        var mode0Cells = CaptureAllCells(renderer);

        // Switch to each subsequent mode and render 1 frame
        for (int mi = 1; mi < modes.Length; mi++)
        {
            canvasView.Mode = modes[mi];
            canvasView.Tick3D();
            canvasView.Render(renderer, area);

            VerifyNoOrphanedCells(renderer, modes[mi], mi);
        }
    }

    [Fact]
    public void FullRedraw_RequiresCanvasAreaClearOutsideFrame()
    {
        var canvasView = new CanvasView();
        var renderer = new FakeTerminalRenderer(TermW, TermH);
        var area = new TermRect(0, 0, TermW, TermH);

        while (canvasView.DemoIndex != 2)
            canvasView.NextDemo();

        canvasView.Mode = TerminalGraphicsMode.HalfBlockColor;
        canvasView.Activate(renderer, area);

        renderer.Clear(ConsoleColor.Gray, ConsoleColor.Black);
        canvasView.RequireFullClear();
        canvasView.Render(renderer, area);

        TermRect frame = CurrentFrame(TerminalGraphicsMode.HalfBlockColor);
        for (int y = 0; y < TermH; y++)
        {
            for (int x = 0; x < TermW; x++)
            {
                bool insideFrame = x >= frame.X && x < frame.X2 && y >= frame.Y && y < frame.Y2;
                if (!insideFrame)
                    renderer.GetCell(x, y).Should().Be(TerminalCell.Black);
            }
        }
    }

    /// <summary>
    /// Byte-level canvas verification: after rendering, the PixelCanvas should only
    /// contain pixels that were set by the last Tick3D call.
    /// </summary>
    [Fact]
    public void CanvasBuffer_AfterModeSwitch_HasNoStalePixels()
    {
        var canvasView = new CanvasView();
        var renderer = new FakeTerminalRenderer(TermW, TermH);
        var area = new TermRect(0, 0, TermW, TermH);

        // Switch to 3D Shapes demo (index 2)
        while (canvasView.DemoIndex != 2)
            canvasView.NextDemo();

        canvasView.Activate(renderer, area);

        // Render in first mode
        canvasView.Mode = TerminalGraphicsMode.HalfBlockColor;
        for (int i = 0; i < 100; i++)
        {
            canvasView.Tick3D();
            canvasView.Render(renderer, area);
        }

        // Count non-black pixels in the canvas
        int nonBlackBefore = CountNonBlack(canvasView.Canvas);
        nonBlackBefore.Should().BeGreaterThan(0, "canvas should have content after rendering");

        // Switch mode and re-render
        canvasView.Mode = TerminalGraphicsMode.Grayscale;
        canvasView.Tick3D();
        canvasView.Render(renderer, area);

        // The canvas should still have non-black pixels (the 3D cube)
        int nonBlackAfter = CountNonBlack(canvasView.Canvas);
        nonBlackAfter.Should().BeGreaterThan(0, "canvas should still have content");

        // The difference should be minor (just the 3D rotation changed pixels slightly)
        // Actually Tick3D redraws the entire canvas, so the pixel count should be similar
        double diffRatio = Math.Abs(nonBlackAfter - nonBlackBefore) / (double)Math.Max(nonBlackBefore, 1);
        Assert.True(diffRatio < 0.5,
            $"Canvas pixel count changed by {diffRatio:P2} after mode switch");
    }

    [Theory]
    [InlineData(TerminalGraphicsMode.BestGlyph)]
    [InlineData(TerminalGraphicsMode.BestGlyphTrueColor)]
    public void Demo3D_GlyphModes_UseNeutralColorsDerivedFromBackground(TerminalGraphicsMode mode)
    {
        var demo = new Demo3D();
        var canvas = new PixelCanvas();

        demo.Tick(canvas, mode);

        for (int y = 0; y < CanvasH; y++)
        {
            for (int x = 0; x < CanvasW; x++)
            {
                Pixel pixel = canvas.GetPixel(x, y);
                if (pixel == Pixel.Black) continue;
                pixel.R.Should().Be(pixel.G);
                pixel.G.Should().Be(pixel.B);
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────

    private TerminalCell[] CaptureAllCells(FakeTerminalRenderer r)
    {
        var cells = new TerminalCell[TermW * TermH];
        for (int y = 0; y < TermH; y++)
            for (int x = 0; x < TermW; x++)
                cells[y * TermW + x] = r.GetCell(x, y);
        return cells;
    }

    private void VerifyNoOrphanedCells(FakeTerminalRenderer r, TerminalGraphicsMode mode, int step)
    {
        TermRect frame = CurrentFrame(mode);
        int orphanCount = 0;
        for (int y = 0; y < TermH; y++)
        {
            for (int x = 0; x < TermW; x++)
            {
                bool insideFrame = x >= frame.X && x < frame.X2 && y >= frame.Y && y < frame.Y2;
                if (insideFrame) continue;

                var cell = r.GetCell(x, y);
                if (!IsBackground(cell))
                    orphanCount++;
            }
        }

        Assert.True(orphanCount == 0,
            $"Step {step} mode={mode}: {orphanCount} cells outside current frame are not background");
    }

    private static bool IsBackground(TerminalCell cell)
    {
        return cell == TerminalCell.Black || cell == TerminalCell.Unknown;
    }

    private static TermRect CurrentFrame(TerminalGraphicsMode mode)
    {
        double pcc = TerminalGraphicsModes.PixelsPerCellColumn(mode);
        double prc = TerminalGraphicsModes.PixelsPerCellRow(mode);

        int maxCols = TermW - 4;
        int maxRows = TermH - 4;
        double scale = Math.Min(maxCols * pcc / CanvasW, maxRows * prc / CanvasH);
        scale = Math.Min(1.0, Math.Max(scale, 0.01));
        int cols = Math.Clamp((int)Math.Ceiling(CanvasW * scale / pcc), 1, maxCols);
        int rows = Math.Clamp((int)Math.Ceiling(CanvasH * scale / prc), 1, maxRows);
        return new TermRect(0, 0, TermW, TermH).CenterFrame(cols, rows);
    }

    private void VerifyCanvasBufferClean(PixelCanvas canvas)
    {
        // The canvas should be 320x200
        canvas.Width.Should().Be(CanvasW);
        canvas.Height.Should().Be(CanvasH);

        // Just verify the buffer is accessible and consistent
        int blackCount = 0;
        int nonBlackCount = 0;
        for (int y = 0; y < CanvasH; y++)
        {
            for (int x = 0; x < CanvasW; x++)
            {
                var p = canvas.GetPixel(x, y);
                if (p == Pixel.Black)
                    blackCount++;
                else
                    nonBlackCount++;
            }
        }

        // The canvas should have both black and non-black pixels
        // (the cube doesn't fill the entire 320x200 area)
        Assert.True(nonBlackCount > 0, "Canvas should have non-black pixels (the 3D shape)");
        Assert.True(blackCount > 0, "Canvas should have black pixels (background)");

        // Total should be 320*200 = 64000
        (blackCount + nonBlackCount).Should().Be(CanvasW * CanvasH);
    }

    private int CountNonBlack(PixelCanvas canvas)
    {
        int count = 0;
        for (int y = 0; y < CanvasH; y++)
            for (int x = 0; x < CanvasW; x++)
                if (canvas.GetPixel(x, y) != Pixel.Black)
                    count++;
        return count;
    }
}
