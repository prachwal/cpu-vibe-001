using Cpu.Tui;
using Cpu.Tui.Configuration;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class TuiSettingsStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsSettings()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tui-store-{Guid.NewGuid():N}.json");
        var store = new TuiSettingsStore(path);
        var settings = new TuiAppSettings
        {
            Theme = TuiTheme.Neon,
            FrameStyle = Rendering.FrameStyle.Ascii,
            PanelSide = PanelSide.Right,
            DefaultGraphicsMode = TerminalGraphicsMode.BrailleMono
        };

        store.Save(settings);
        var loaded = store.Load();

        loaded.Theme.Should().Be(TuiTheme.Neon);
        loaded.FrameStyle.Should().Be(Rendering.FrameStyle.Ascii);
        loaded.PanelSide.Should().Be(PanelSide.Right);
        loaded.DefaultGraphicsMode.Should().Be(TerminalGraphicsMode.BrailleMono);
        loaded.MouseEnabled.Should().BeFalse();
    }

    [Fact]
    public void Default_MouseEnabled_IsFalse()
    {
        TuiAppSettings.Default.MouseEnabled.Should().BeFalse();
    }

    [Fact]
    public void Apply_UpdatesCurrentAndPersists()
    {
        var config = TestTuiConfiguration.Create();
        var updated = new TuiAppSettings
        {
            Theme = TuiTheme.Monochrome,
            FrameStyle = Rendering.FrameStyle.Unicode,
            PanelSide = PanelSide.Right,
            DefaultGraphicsMode = TerminalGraphicsMode.Grayscale
        };

        bool fired = false;
        config.Changed += () => fired = true;
        config.Apply(updated);

        config.Current.Theme.Should().Be(TuiTheme.Monochrome);
        config.Current.PanelSide.Should().Be(PanelSide.Right);
        fired.Should().BeTrue();
    }
}
