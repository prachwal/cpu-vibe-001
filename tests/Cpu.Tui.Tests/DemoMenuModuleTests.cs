using Cpu.DemoMenu.Modules;
using Cpu.Tui;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class DemoMenuModuleTests
{
    private static readonly ITuiAppConfiguration Config = TestTuiConfiguration.Create();

    private static ConsoleKeyInfo Key(ConsoleKey key) =>
        new('\0', key, false, false, false);

    [Fact]
    public void PlayerOpen_F1_BubblesToMainMenu()
    {
        var help = new TestNavModule(Config, "Help");
        var demoMenu = new DemoMenuModule(Config);
        var menu = new MainMenuModule([demoMenu, help]);

        menu.OnKey(Key(ConsoleKey.F4)).Should().BeTrue();
        demoMenu.OnKey(Key(ConsoleKey.Enter)).Should().BeTrue();
        demoMenu.Child.Should().NotBeNull();

        menu.OnKey(Key(ConsoleKey.F1)).Should().BeTrue();
        menu.Child.Should().Be(help);
    }

    [Fact]
    public void PlayerPlaying_Escape_DoesNotClosePlayer()
    {
        var demoMenu = new DemoMenuModule(Config);
        demoMenu.OnKey(Key(ConsoleKey.Enter)).Should().BeTrue();
        demoMenu.Child.Should().NotBeNull();

        demoMenu.OnKey(Key(ConsoleKey.Escape)).Should().BeTrue();
        demoMenu.Child.Should().NotBeNull();
    }

    private sealed class TestNavModule(ITuiAppConfiguration config, string name) : ModuleBase(config)
    {
        public override string Name => name;
        protected override bool OnKeyCore(ConsoleKeyInfo key) => false;
        protected override void RenderContent(ITerminalRenderer r, int x, int y, int w, int h) { }
        protected override void RenderPanelInfo(ITerminalRenderer r, int w, ref int y) { }
    }
}
