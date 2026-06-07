using Cpu.Module;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class ModuleChainTests
{
    private sealed class TestModule : IAppModule
    {
        public string Name { get; }
        public IAppModule? Parent { get; set; }
        public IAppModule? Child { get; private set; }
        public bool IsActive { get; private set; }
        public int ActivateCount { get; private set; }
        public int DeactivateCount { get; private set; }
        public int KeyCount { get; private set; }
        public int MouseCount { get; private set; }

        public TestModule(string name) { Name = name; }

        public void SetChild(IAppModule? child) => Child = child;
        public void OnActivate() { IsActive = true; ActivateCount++; }
        public void OnDeactivate() { IsActive = false; DeactivateCount++; }
        public bool OnKey(ConsoleKeyInfo key) { KeyCount++; return key.Key == ConsoleKey.A; }
        public bool OnMouse(MouseEvent e) { MouseCount++; return true; }
        public bool OnTick() => false;
        public void OnRender(ITerminalRenderer r, int tw, int th) { }
    }

    [Fact]
    public void MainMenu_HighlightsSelectedItem()
    {
        var items = new[] { new TestModule("A"), new TestModule("B"), new TestModule("C") };
        var menu = new MainMenuModule(items);

        using var ms = new MemoryStream();
        using var r = new AnsiTerminalRenderer(ms);
        r.Resize(50, 20);

        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        // selected should be index 2 ("C")

        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        menu.Child.Should().Be(items[2]);
        items[2].ActivateCount.Should().Be(1);
    }

    [Fact]
    public void Escape_ClosesChildAndReturnsToParent()
    {
        var child = new TestModule("Child");
        var menu = new MainMenuModule([new TestModule("A"), child, new TestModule("C")]);

        // Select Child and open
        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        menu.Child.Should().Be(child);
        child.IsActive.Should().BeTrue();

        // Esc should close child
        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false));
        menu.Child.Should().BeNull();
        child.IsActive.Should().BeFalse();
        child.DeactivateCount.Should().Be(1);
    }

    [Fact]
    public void ChildReceivesKeysBeforeParent()
    {
        var child = new TestModule("Child");
        var menu = new MainMenuModule([new TestModule("A"), child]);
        menu.SetChild(child);
        child.Parent = menu;

        // Press 'A' — child handles it (returns true)
        menu.OnKey(new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false))
            .Should().BeTrue();
        child.KeyCount.Should().Be(1);
    }

    [Fact]
    public void ModuleManager_DispatchesToActiveLeaf()
    {
        var leaf = new TestModule("Leaf");
        var menu = new MainMenuModule([leaf]);
        var mgr = new ModuleManager(menu);

        menu.SetChild(leaf);
        leaf.Parent = menu;
        mgr.UpdateActive();

        mgr.OnKey(new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false))
            .Should().BeTrue();
        leaf.KeyCount.Should().Be(1);
    }

    [Fact]
    public void ModuleManager_DispatchesMouse()
    {
        var leaf = new TestModule("Leaf");
        var menu = new MainMenuModule([leaf]);
        var mgr = new ModuleManager(menu);

        menu.SetChild(leaf);
        leaf.Parent = menu;
        mgr.UpdateActive();

        mgr.OnMouse(new MouseEvent(10, 20, 0, false)).Should().BeTrue();
        leaf.MouseCount.Should().Be(1);
    }

    [Fact]
    public void NavigatingMenu_DoesNotActivateModule()
    {
        var m0 = new TestModule("A");
        var m1 = new TestModule("B");
        var menu = new MainMenuModule([m0, m1]);

        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        menu.Child.Should().BeNull();
        m0.ActivateCount.Should().Be(0);
    }

    [Fact]
    public void FKey_OpensMatchingModule()
    {
        var help = new TestModule("Help");
        var menu = new MainMenuModule([new TestModule("A"), help]);

        menu.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.F1, false, false, false));
        menu.Child.Should().Be(help);
        help.ActivateCount.Should().Be(1);
    }
}
