using Cpu.DemoMenu.Modules;
using Cpu.Tui;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class DemoPlayerModuleTests
{
    private static readonly ITuiAppConfiguration Config = TestTuiConfiguration.Create();

    private static ConsoleKeyInfo Key(ConsoleKey key) =>
        new('\0', key, false, false, false);

    [Fact]
    public void Playing_F1_PassesThroughForGlobalNavigation()
    {
        var player = new DemoPlayerModule(0, Config);
        player.OnActivate();

        player.OnKey(Key(ConsoleKey.F1)).Should().BeFalse();
    }

    [Fact]
    public void Playing_Escape_IsConsumed()
    {
        var player = new DemoPlayerModule(0, Config);
        player.OnActivate();

        player.OnKey(Key(ConsoleKey.Escape)).Should().BeTrue();
    }

    [Fact]
    public void Playing_P_TogglesPause()
    {
        var player = new DemoPlayerModule(0, Config);
        player.OnActivate();

        player.OnKey(Key(ConsoleKey.P)).Should().BeTrue();
        player.OnKey(Key(ConsoleKey.P)).Should().BeTrue();
    }
}
