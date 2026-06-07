using Cpu.Canvas.Rendering;
using Cpu.Tui.Graphics;
using Cpu.Tui;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class GraphicsModeSettingsTests
{
    [Fact]
    public void CanvasView_ApplySettings_DoesNotResetCycledMode()
    {
        var view = new Canvas.Rendering.Views.CanvasView();
        view.CycleMode();
        TerminalGraphicsMode mode = view.Mode;

        view.ApplySettings(TuiAppSettings.Default);

        view.Mode.Should().Be(mode);
    }

    [Fact]
    public void ImageView_ApplySettings_DoesNotResetCycledMode()
    {
        var view = new Cpu.Image.Rendering.Views.ImageView();
        view.CycleMode();
        TerminalGraphicsMode mode = view.Mode;

        view.ApplySettings(TuiAppSettings.Default);

        view.Mode.Should().Be(mode);
    }
}
