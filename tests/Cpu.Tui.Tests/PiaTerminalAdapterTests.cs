using FluentAssertions;
using Xunit;
using Cpu.Tui.Devices.Pia;

namespace Cpu.Tui.Tests;

public class PiaTerminalAdapterTests
{
    private (PiaDevice pia, ScreenBuffer screen, PiaTerminalAdapter adapter) CreateAdapter()
    {
        var pia = new PiaDevice(0x8000);
        var screen = new ScreenBuffer();
        var adapter = new PiaTerminalAdapter(pia, screen);
        return (pia, screen, adapter);
    }

    [Fact]
    public void Constructor_NullPia_Throws()
    {
        var screen = new ScreenBuffer();
        Action act = () => new PiaTerminalAdapter(null!, screen);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullScreen_Throws()
    {
        var pia = new PiaDevice(0x8000);
        Action act = () => new PiaTerminalAdapter(pia, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteString_DisplaysCharacters()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteString("Hello");

        screen.GetChar(0, 0).Should().Be('H');
        screen.GetChar(1, 0).Should().Be('e');
        screen.GetChar(2, 0).Should().Be('l');
        screen.GetChar(3, 0).Should().Be('l');
        screen.GetChar(4, 0).Should().Be('o');
    }

    [Fact]
    public void WriteString_CursorAdvances()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("ABC");
        adapter.CursorX.Should().Be(3);
        adapter.CursorY.Should().Be(0);
    }

    [Fact]
    public void CarriageReturn_ResetsX()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("ABC\r");
        adapter.CursorX.Should().Be(0);
        adapter.CursorY.Should().Be(0);
    }

    [Fact]
    public void LineFeed_MovesCursorDown()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("ABC\n");
        adapter.CursorX.Should().Be(3);
        adapter.CursorY.Should().Be(1);
    }

    [Fact]
    public void Backspace_MovesCursorLeft()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("ABC\b");
        adapter.CursorX.Should().Be(2);
    }

    [Fact]
    public void Tab_MovesToNextTabStop()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("\t");
        adapter.CursorX.Should().Be(8);
    }

    [Fact]
    public void CR_LF_ResetsX_And_MovesDown()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteString("Line1\r\nLine2");

        adapter.CursorX.Should().Be(5);
        adapter.CursorY.Should().Be(1);
        screen.GetChar(0, 1).Should().Be('L');
        screen.GetChar(4, 1).Should().Be('2');
    }

    [Fact]
    public void Escape_Home_ResetsCursor()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("Hello");
        adapter.WriteEsc("H");

        adapter.CursorX.Should().Be(0);
        adapter.CursorY.Should().Be(0);
    }

    [Fact]
    public void CSI_CursorUp_MovesUp()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("Line1\nLine2");
        adapter.WriteEsc("[A");

        adapter.CursorY.Should().Be(0);
    }

    [Fact]
    public void CSI_CursorDown_MovesDown()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteEsc("[B");

        adapter.CursorY.Should().Be(1);
    }

    [Fact]
    public void CSI_CursorForward_MovesRight()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteEsc("[3C");

        adapter.CursorX.Should().Be(3);
    }

    [Fact]
    public void CSI_CursorBack_MovesLeft()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("ABC");
        adapter.WriteEsc("[2D");

        adapter.CursorX.Should().Be(1);
    }

    [Fact]
    public void CSI_CursorPosition_SetsPosition()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteEsc("[5;10H");

        adapter.CursorY.Should().Be(4);
        adapter.CursorX.Should().Be(9);
    }

    [Fact]
    public void CSI_EraseToEndOfLine_ClearsLine()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteString("ABCDEFG");
        adapter.WriteEsc("[2D");
        adapter.WriteEsc("[K");

        screen.GetChar(3, 0).Should().Be('D');
        screen.GetChar(4, 0).Should().Be('E');
        screen.GetChar(5, 0).Should().Be(' ');
        screen.GetChar(6, 0).Should().Be(' ');
    }

    [Fact]
    public void CSI_EraseDisplay_ClearsScreen()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteString("Hello");
        adapter.WriteEsc("[2J");

        for (int i = 0; i < 5; i++)
            screen.GetChar(i, 0).Should().Be(' ');
    }

    [Fact]
    public void SGR_ForegroundColors_Applied()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteEsc("[31m");
        adapter.WriteString("X");

        screen.GetForeground(0, 0).Should().Be(ConsoleColor.DarkRed);
    }

    [Fact]
    public void SGR_BackgroundColors_Applied()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteEsc("[44m");
        adapter.WriteString("X");

        screen.GetBackground(0, 0).Should().Be(ConsoleColor.DarkBlue);
    }

    [Fact]
    public void SGR_Reset_ClearsColors()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteEsc("[31m");
        adapter.WriteEsc("[0m");
        adapter.WriteString("X");

        screen.GetForeground(0, 0).Should().Be(ConsoleColor.Gray);
        screen.GetBackground(0, 0).Should().Be(ConsoleColor.Black);
    }

    [Fact]
    public void ScrollUp_ClearsTopRow_AfterFull()
    {
        var (_, screen, adapter) = CreateAdapter();

        for (int i = 0; i < ScreenBuffer.Height; i++)
        {
            adapter.WriteString($"R{i:D2}");
            if (i < ScreenBuffer.Height - 1)
                adapter.WriteString("\r\n");
        }

        adapter.WriteString("\r\n");

        screen.GetChar(0, 0).Should().Be('R');
        screen.GetChar(1, 0).Should().Be('0');
        screen.GetChar(2, 0).Should().Be('1');
    }

    [Fact]
    public void CSI_DeleteLines_ShiftsUp()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteString("A\r\nB\r\nC\r\nD");
        adapter.WriteEsc("[1;1H");
        adapter.WriteEsc("[1M");

        screen.GetChar(0, 0).Should().Be('B');
        screen.GetChar(0, 1).Should().Be('C');
    }

    [Fact]
    public void CSI_DeleteChars_ShiftsLeft()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteString("ABCDE");
        adapter.WriteEsc("[1;1H");
        adapter.WriteEsc("[2P");

        screen.GetChar(0, 0).Should().Be('C');
        screen.GetChar(1, 0).Should().Be('D');
        screen.GetChar(2, 0).Should().Be('E');
        screen.GetChar(3, 0).Should().Be(' ');
    }

    [Fact]
    public void CursorVisible_DefaultsTrue()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.CursorVisible.Should().BeTrue();
    }

    [Fact]
    public void PrivateMode_ShowCursor()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteEsc("[?25l");
        adapter.CursorVisible.Should().BeFalse();

        adapter.WriteEsc("[?25h");
        adapter.CursorVisible.Should().BeTrue();
    }

    [Fact]
    public void WriteString_MultipleLines()
    {
        var (_, screen, adapter) = CreateAdapter();
        adapter.WriteString("Line1\r\nLine2\r\nLine3");

        screen.GetChar(0, 0).Should().Be('L');
        screen.GetChar(0, 1).Should().Be('L');
        screen.GetChar(0, 2).Should().Be('L');
    }

    [Fact]
    public void SaveRestoreCursor_PreservesPosition()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("ABC");
        adapter.WriteEsc("[s");
        adapter.WriteString("DEF");
        adapter.WriteEsc("[u");

        adapter.CursorX.Should().Be(3);
    }

    [Fact]
    public void Tab_StopsAt8_Columns()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("\t");
        adapter.CursorX.Should().Be(8);

        adapter.WriteString("\t");
        adapter.CursorX.Should().Be(16);
    }

    [Fact]
    public void BEL_DoesNotCrash()
    {
        var (_, _, adapter) = CreateAdapter();
        Action act = () => adapter.WriteString("\x07");
        act.Should().NotThrow();
    }

    [Fact]
    public void CursorHome_ResetsToTopLeft()
    {
        var (_, _, adapter) = CreateAdapter();
        adapter.WriteString("ABCDEF");
        adapter.WriteEsc("[H");

        adapter.CursorX.Should().Be(0);
        adapter.CursorY.Should().Be(0);
    }
}
