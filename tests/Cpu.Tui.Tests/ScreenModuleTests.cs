using Cpu.Screen.Modules;
using Cpu.Tui.Devices.Pia;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class ScreenModuleTests
{
    private static ScreenModule Create(out ScreenBuffer screen)
    {
        screen = new ScreenBuffer();
        var echo = new EchoTerminal(screen);
        return new ScreenModule(screen, echo);
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0') =>
        new(ch, key, false, false, false);

    [Fact]
    public void EchoMode_F6ThenPrintable_WritesToScreen()
    {
        var module = Create(out var screen);
        module.OnActivate();

        module.OnKey(Key(ConsoleKey.F6)).Should().BeTrue();
        module.OnKey(Key(ConsoleKey.H, 'h')).Should().BeTrue();
        module.OnKey(Key(ConsoleKey.I, 'i')).Should().BeTrue();

        screen.GetChar(0, 0).Should().Be('h');
        screen.GetChar(1, 0).Should().Be('i');
    }

    [Fact]
    public void EchoMode_Off_IgnoresPrintable()
    {
        var module = Create(out var screen);
        module.OnActivate();

        module.OnKey(Key(ConsoleKey.H, 'h')).Should().BeFalse();
        screen.GetChar(0, 0).Should().Be(' ');
    }
}
