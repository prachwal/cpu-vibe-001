using Cpu.Screen.Modules;
using Cpu.Screen.Rendering.Views;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Layout;
using Cpu.Tui.Rendering;
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

    [Fact]
    public void ScreenView_UsesScreenModeForFrameSize()
    {
        var screen = new ScreenBuffer();
        var echo = new EchoTerminal(screen);
        var view = new ScreenView(screen, echo) { ScreenMode = ScreenMode.Rows24Cols40 };

        var renderer = new FakeTerminalRenderer(90, 30);
        var area = new TermRect(0, 0, 90, 29);
        view.Render(renderer, area, new PresentationSession(renderer, area));

        MeasureFrameWidth(renderer).Should().Be(42);
    }

    [Fact]
    public void F5_SetsScreenMode24x40_InModuleRender()
    {
        var module = Create(out _);
        module.OnActivate();
        module.OnKey(Key(ConsoleKey.F5)).Should().BeTrue();

        var renderer = new FakeTerminalRenderer(120, 30);
        module.OnRender(renderer, 120, 30);

        MeasureFrameWidth(renderer).Should().Be(42);
    }

    private static int MeasureFrameWidth(FakeTerminalRenderer r)
    {
        for (int y = 0; y < r.Height; y++)
        {
            for (int x = 0; x < r.Width; x++)
            {
                if (r.GetCell(x, y).Ch is not ('+' or '┌'))
                    continue;
                int w = 0;
                for (int c = x; c < r.Width; c++)
                {
                    char ch = r.GetCell(c, y).Ch;
                    if (ch is '+' or '-' or '┌' or '─' or '┐')
                        w++;
                    else
                        break;
                }
                return w;
            }
        }
        return 0;
    }
}
