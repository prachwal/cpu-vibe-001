using Cpu.Module;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class MainMenuMouseTests
{
    private sealed class StubModule(string name) : IAppModule
    {
        public string Name => name;
        public IAppModule? Parent { get; set; }
        public IAppModule? Child { get; private set; }
        public bool IsActive { get; private set; }

        public void SetChild(IAppModule? child) => Child = child;
        public void OnActivate() => IsActive = true;
        public void OnDeactivate() => IsActive = false;
        public bool OnKey(ConsoleKeyInfo key) => false;
        public bool OnMouse(MouseEvent e) => false;
        public bool OnTick() => false;
        public void OnRender(ITerminalRenderer r, int tw, int th) { }
    }

    [Fact]
    public void Motion_DoesNotOpenModule()
    {
        var a = new StubModule("A");
        var b = new StubModule("B");
        var menu = new MainMenuModule([a, b]);
        menu.OnActivate();
        menu.OnRender(new FakeTerminalRenderer(80, 30), 80, 30);

        menu.OnMouse(new MouseEvent(5, MenuStartY(30), 0, false, true)).Should().BeFalse();
        menu.Child.Should().BeNull();
    }

    [Fact]
    public void Click_OpensHitModule()
    {
        var a = new StubModule("A");
        var b = new StubModule("B");
        var menu = new MainMenuModule([a, b]);
        menu.OnActivate();
        menu.OnRender(new FakeTerminalRenderer(80, 30), 80, 30);

        int row = MenuStartY(30) + 1;
        menu.OnMouse(new MouseEvent(5, row, 0, false)).Should().BeTrue();
        menu.Child.Should().Be(b);
        b.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Click_WithChildOpen_IsIgnored()
    {
        var a = new StubModule("A");
        var b = new StubModule("B");
        var menu = new MainMenuModule([a, b]);
        menu.OnActivate();
        menu.OnRender(new FakeTerminalRenderer(80, 30), 80, 30);
        menu.OnMouse(new MouseEvent(5, MenuStartY(30), 0, false));

        menu.OnMouse(new MouseEvent(5, MenuStartY(30) + 1, 0, false)).Should().BeFalse();
        menu.Child.Should().Be(a);
    }

    private static int MenuStartY(int h) => Math.Max(2, (h - 2) / 2);
}
