using Cpu.Apple1.Modules;
using Cpu.Canvas.Modules;
using Cpu.DemoMenu.Modules;
using Cpu.Help.Modules;
using Cpu.Image.Modules;
using Cpu.Module;
using Cpu.Screen.Modules;
using Cpu.Tui;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class ModuleKeyTests
{
    private static ConsoleKeyInfo K(ConsoleKey k, char ch = '\0')
        => new(ch, k, false, false, false);

    [Fact] public void Help_NoKeysHandled()
    {
        var m = new HelpModule();
        m.OnKey(K(ConsoleKey.A)).Should().BeFalse();
        m.OnKey(K(ConsoleKey.F1)).Should().BeFalse();
        m.OnKey(K(ConsoleKey.Escape)).Should().BeFalse();
    }

    [Fact] public void Screen_HandlesOwnKeysOnly()
    {
        var s = new ScreenBuffer();
        var screen = new ScreenModule(s, new EchoTerminal(s));
        screen.OnKey(K(ConsoleKey.F2)).Should().BeTrue();
        screen.OnKey(K(ConsoleKey.F3)).Should().BeTrue();
        screen.OnKey(K(ConsoleKey.F6)).Should().BeTrue();
        screen.OnKey(K(ConsoleKey.A)).Should().BeFalse();
        screen.OnKey(K(ConsoleKey.Escape)).Should().BeFalse();
    }

    [Fact] public void Image_HandlesOwnKeysOnly()
    {
        var img = new ImageModule();
        img.OnKey(K(ConsoleKey.LeftArrow)).Should().BeTrue();
        img.OnKey(K(ConsoleKey.RightArrow)).Should().BeTrue();
        img.OnKey(K(ConsoleKey.UpArrow)).Should().BeTrue();
        img.OnKey(K(ConsoleKey.DownArrow)).Should().BeTrue();
        img.OnKey(K(ConsoleKey.F7)).Should().BeTrue();
        img.OnKey(K(ConsoleKey.F8)).Should().BeTrue();
        img.OnKey(K(ConsoleKey.A)).Should().BeFalse();
        img.OnKey(K(ConsoleKey.Escape)).Should().BeFalse();
    }

    [Fact] public void Canvas_HandlesOwnKeysOnly()
    {
        var cv = new CanvasModule();
        cv.OnKey(K(ConsoleKey.LeftArrow)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.RightArrow)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.F10)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.UpArrow)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.DownArrow)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.W)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.A)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.S)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.D)).Should().BeTrue();
        cv.OnKey(K(ConsoleKey.Escape)).Should().BeFalse();
        cv.OnKey(K(ConsoleKey.F1)).Should().BeFalse();
    }

    [Fact] public void Apple1_HandlesEmulationKeysNotNavigation()
    {
        var a1 = new Apple1Module();
        a1.OnKey(K(ConsoleKey.UpArrow)).Should().BeTrue();
        a1.OnKey(K(ConsoleKey.DownArrow)).Should().BeTrue();
        a1.OnKey(K(ConsoleKey.F10)).Should().BeTrue();
        a1.OnKey(K(ConsoleKey.Enter)).Should().BeTrue();
        a1.OnKey(K(ConsoleKey.Backspace)).Should().BeTrue();
        a1.OnKey(K(ConsoleKey.A, 'A')).Should().BeTrue();
        // Navigation keys pass through to parent
        a1.OnKey(K(ConsoleKey.Escape)).Should().BeFalse();
        a1.OnKey(K(ConsoleKey.F1)).Should().BeFalse();
        a1.OnKey(K(ConsoleKey.F4)).Should().BeFalse();
        a1.OnKey(K(ConsoleKey.F7)).Should().BeFalse();
        a1.OnKey(K(ConsoleKey.F8)).Should().BeFalse();
        a1.OnKey(K(ConsoleKey.F9)).Should().BeFalse();
    }

    [Fact] public void DemoMenu_HandlesMenuKeys()
    {
        var dm = new DemoMenuModule(new ErrorCollector());
        dm.OnKey(K(ConsoleKey.UpArrow)).Should().BeTrue();
        dm.OnKey(K(ConsoleKey.DownArrow)).Should().BeTrue();
        dm.OnKey(K(ConsoleKey.Enter)).Should().BeTrue();
        // Navigation keys like Esc are handled by parent chain, not by DemoMenu
    }

    [Fact] public void DemoPlayer_Playing_ConsumesAllKeys()
    {
        var player = new DemoPlayerModule(0);
        player.OnActivate();
        player.OnKey(K(ConsoleKey.P)).Should().BeTrue();
        player.OnKey(K(ConsoleKey.Add)).Should().BeTrue();
        player.OnKey(K(ConsoleKey.Subtract)).Should().BeTrue();
        player.OnKey(K(ConsoleKey.A, 'A')).Should().BeTrue();
        // While playing, ALL keys are consumed (including Esc)
        player.OnKey(K(ConsoleKey.Escape)).Should().BeTrue("Esc consumed during playback");
    }

    [Fact] public void DemoPlayer_Ended_OnlyEscPassesToParent()
    {
        var player = new DemoPlayerModule(0);
        player.OnActivate();
        // Speed up to 10x to finish faster
        for (int i = 0; i < 10; i++) player.OnKey(K(ConsoleKey.Add));
        for (int i = 0; i < 2000 && player.OnTick(); i++) { }
        player.OnKey(K(ConsoleKey.Escape)).Should().BeFalse("ended passes Esc to parent");
        player.OnKey(K(ConsoleKey.A, 'A')).Should().BeTrue("ended consumes other keys");
    }

    [Fact] public void MainMenu_NoChild_HandlesMenuKeys()
    {
        var items = CreateModules();
        var m = new MainMenuModule(items);
        m.OnKey(K(ConsoleKey.UpArrow)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.DownArrow)).Should().BeTrue();

        // F-keys work before child is opened
        m.OnKey(K(ConsoleKey.F1)).Should().BeTrue();
        m.Child.Should().NotBeNull("F1 opens a child");
        // Close child before continuing
        m.OnKey(K(ConsoleKey.Escape)).Should().BeTrue();
        m.Child.Should().BeNull();

        m.OnKey(K(ConsoleKey.F4)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.Escape)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.F7)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.Escape)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.F8)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.Escape)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.F9)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.Escape)).Should().BeTrue();

        m.OnKey(K(ConsoleKey.F12)).Should().BeTrue();
        m.OnKey(K(ConsoleKey.A)).Should().BeFalse();
    }

    [Fact] public void MainMenu_WithChild_FallbacksWork()
    {
        var items = CreateModules();
        var m = new MainMenuModule(items);
        var child = items.OfType<HelpModule>().First();
        m.SetChild(child);

        m.OnKey(K(ConsoleKey.A)).Should().BeFalse("Help doesn't handle keys");
        m.OnKey(K(ConsoleKey.F8)).Should().BeTrue("MainMenu fallback switches to Apple1");
    }

    [Fact] public void ModuleManager_ChainDispatch()
    {
        var items = CreateModules();
        var root = new MainMenuModule(items);
        var mgr = new ModuleManager(root);

        root.OnKey(K(ConsoleKey.Enter));
        root.OnKey(K(ConsoleKey.Enter));
        mgr.UpdateActive();
        mgr.Active.Should().NotBe(root, "active should be a leaf");

        mgr.OnKey(K(ConsoleKey.F8)).Should().BeTrue("switch to Apple1");

        mgr.OnKey(K(ConsoleKey.F1)).Should().BeTrue("F1 switches to Help");
        mgr.OnKey(K(ConsoleKey.Escape)).Should().BeTrue("Esc closes Help");
        // After closing, we're back at menu or in another module
    }

    private static IAppModule[] CreateModules() =>
    [
        new HelpModule(),
        new ScreenModule(new ScreenBuffer(), new EchoTerminal(new ScreenBuffer())),
        new DemoMenuModule(new ErrorCollector()),
        new ImageModule(),
        new CanvasModule(),
        new Apple1Module()
    ];
}
