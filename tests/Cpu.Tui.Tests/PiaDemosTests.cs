using FluentAssertions;
using Xunit;
using Cpu.Tui.Devices.Pia;

namespace Cpu.Tui.Tests;

public class PiaDemosTests
{
    [Fact]
    public void Names_HasFiveEntries()
    {
        PiaDemos.Names.Should().HaveCount(5);
    }

    [Fact]
    public void GetText_ReturnsNonEmpty()
    {
        for (int i = 0; i < PiaDemos.Names.Length; i++)
        {
            string text = PiaDemos.GetText(i);
            text.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GetText_LoremIpsum_ContainsLorem()
    {
        PiaDemos.GetText(0).Should().Contain("Lorem ipsum");
    }

    [Fact]
    public void GetText_BoxDrawing_ContainsBoxChars()
    {
        PiaDemos.GetText(1).Should().Contain("+--");
    }

    [Fact]
    public void GetText_ColorBars_ContainsEsc()
    {
        PiaDemos.GetText(2).Should().Contain("\x1B[");
    }

    [Fact]
    public void Run_SendsDataToTerminal()
    {
        var pia = new PiaDevice(0x8800);
        var screen = new ScreenBuffer();
        var adapter = new PiaTerminalAdapter(pia, screen);

        PiaDemos.Run(pia, 0);

        screen.GetChar(0, 0).Should().NotBe('\0');
    }

    [Fact]
    public void Run_AllDemos_DontCrash()
    {
        var pia = new PiaDevice(0x8800);
        var screen = new ScreenBuffer();
        var adapter = new PiaTerminalAdapter(pia, screen);

        Action act = () =>
        {
            for (int i = 0; i < PiaDemos.Names.Length; i++)
            {
                pia.Reset();
                screen.Reset();
                PiaDemos.Run(pia, i);
            }
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Run_BoxDrawing_FillsScreen()
    {
        var pia = new PiaDevice(0x8800);
        var screen = new ScreenBuffer();
        var adapter = new PiaTerminalAdapter(pia, screen);

        PiaDemos.Run(pia, 1);

        screen.GetChar(0, 0).Should().NotBe('\0');
        screen.GetChar(10, 0).Should().NotBe('\0');
    }

    [Fact]
    public void Run_ScrollDemo_MultipleLines()
    {
        var pia = new PiaDevice(0x8800);
        var screen = new ScreenBuffer();
        var adapter = new PiaTerminalAdapter(pia, screen);

        PiaDemos.Run(pia, 4);

        screen.GetChar(0, 0).Should().NotBe('\0');
    }
}
