using Cpu.Setup.Modules;
using Cpu.Tui;
using Cpu.Tui.Configuration;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class SetupModuleTests
{
    [Fact]
    public void Save_AppliesConfigurationToAllModules()
    {
        var config = TestTuiConfiguration.Create();
        var store = new TuiSettingsStore(Path.Combine(Path.GetTempPath(), $"setup-{Guid.NewGuid():N}.json"));
        var setup = new SetupModule(config, store);
        setup.OnActivate();

        setup.OnKey(Key(ConsoleKey.DownArrow)).Should().BeTrue();
        setup.OnKey(Key(ConsoleKey.DownArrow)).Should().BeTrue();
        setup.OnKey(Key(ConsoleKey.RightArrow)).Should().BeTrue();
        setup.OnKey(Key(ConsoleKey.S)).Should().BeTrue();

        config.Current.PanelSide.Should().Be(PanelSide.Right);
        config.Current.FrameStyle.Should().Be(Rendering.FrameStyle.Unicode);
    }

    [Fact]
    public void PanelSide_Right_PlacesPanelOnRight()
    {
        var config = TestTuiConfiguration.Create(new TuiAppSettings { PanelSide = PanelSide.Right });
        var help = new Cpu.Help.Modules.HelpModule(config);
        var renderer = new FakeTerminalRenderer(120, 30);

        help.OnRender(renderer, 120, 30);

        renderer.GetCell(119, 1).Bg.ConsoleColor.Should().Be(ConsoleColor.DarkBlue);
        renderer.GetCell(0, 1).Bg.ConsoleColor.Should().NotBe(ConsoleColor.DarkBlue);
    }

    private static ConsoleKeyInfo Key(ConsoleKey key) =>
        new('\0', key, false, false, false);
}
