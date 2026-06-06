using FluentAssertions;
using Xunit;
using Cpu.Tui.Devices.Pia;

namespace Cpu.Tui.Tests;

public class EchoTerminalTests
{
    private (ScreenBuffer screen, EchoTerminal echo) Create()
    {
        var screen = new ScreenBuffer();
        var echo = new EchoTerminal(screen);
        return (screen, echo);
    }

    [Fact]
    public void Constructor_NullScreen_Throws()
    {
        Action act = () => new EchoTerminal(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void InitialCursor_IsTopLeft()
    {
        var (_, echo) = Create();
        echo.CursorX.Should().Be(0);
        echo.CursorY.Should().Be(0);
    }

    [Fact]
    public void InitialCursor_IsVisible()
    {
        var (_, echo) = Create();
        echo.CursorVisible.Should().BeTrue();
    }

    [Fact]
    public void PrintableChar_WritesToScreen()
    {
        var (screen, echo) = Create();
        echo.ProcessKey(ConsoleKey.A, 'a');

        screen.GetChar(0, 0).Should().Be('a');
        echo.CursorX.Should().Be(1);
    }

    [Fact]
    public void PrintableChar_AdvancesCursor()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.A, 'a');
        echo.ProcessKey(ConsoleKey.B, 'b');
        echo.ProcessKey(ConsoleKey.C, 'c');

        echo.CursorX.Should().Be(3);
    }

    [Fact]
    public void Enter_MovesToNextLine()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.Enter, '\r');

        echo.CursorX.Should().Be(0);
        echo.CursorY.Should().Be(1);
    }

    [Fact]
    public void Enter_AtBottom_StaysAtBottom()
    {
        var (_, echo) = Create();
        for (int i = 0; i < EchoTerminal.Rows; i++)
            echo.ProcessKey(ConsoleKey.Enter, '\r');

        echo.CursorY.Should().Be(EchoTerminal.Rows - 1);
    }

    [Fact]
    public void Backspace_DecrementsCursor()
    {
        var (screen, echo) = Create();
        echo.ProcessKey(ConsoleKey.A, 'a');
        echo.ProcessKey(ConsoleKey.B, 'b');
        echo.ProcessKey(ConsoleKey.Backspace, '\b');

        echo.CursorX.Should().Be(1);
        screen.GetChar(1, 0).Should().Be(' ');
    }

    [Fact]
    public void Backspace_AtStart_DoesNothing()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.Backspace, '\b');

        echo.CursorX.Should().Be(0);
    }

    [Fact]
    public void LeftArrow_MovesLeft()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.A, 'a');
        echo.ProcessKey(ConsoleKey.LeftArrow, '\0');

        echo.CursorX.Should().Be(0);
    }

    [Fact]
    public void LeftArrow_AtStart_DoesNothing()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.LeftArrow, '\0');

        echo.CursorX.Should().Be(0);
    }

    [Fact]
    public void RightArrow_MovesRight()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.RightArrow, '\0');

        echo.CursorX.Should().Be(1);
    }

    [Fact]
    public void RightArrow_AtEnd_DoesNothing()
    {
        var (_, echo) = Create();
        for (int i = 0; i < EchoTerminal.Cols; i++)
            echo.ProcessKey(ConsoleKey.RightArrow, '\0');

        echo.CursorX.Should().Be(EchoTerminal.Cols - 1);
    }

    [Fact]
    public void UpArrow_MovesUp()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.Enter, '\r');
        echo.ProcessKey(ConsoleKey.UpArrow, '\0');

        echo.CursorY.Should().Be(0);
    }

    [Fact]
    public void UpArrow_AtTop_DoesNothing()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.UpArrow, '\0');

        echo.CursorY.Should().Be(0);
    }

    [Fact]
    public void DownArrow_MovesDown()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.DownArrow, '\0');

        echo.CursorY.Should().Be(1);
    }

    [Fact]
    public void DownArrow_AtBottom_DoesNothing()
    {
        var (_, echo) = Create();
        for (int i = 0; i < EchoTerminal.Rows; i++)
            echo.ProcessKey(ConsoleKey.DownArrow, '\0');

        echo.CursorY.Should().Be(EchoTerminal.Rows - 1);
    }

    [Fact]
    public void Home_ResetsToTopLeft()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.A, 'a');
        echo.ProcessKey(ConsoleKey.Enter, '\r');
        echo.ProcessKey(ConsoleKey.Home, '\0');

        echo.CursorX.Should().Be(0);
        echo.CursorY.Should().Be(0);
    }

    [Fact]
    public void End_MovesToBottomLeft()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.End, '\0');

        echo.CursorX.Should().Be(0);
        echo.CursorY.Should().Be(EchoTerminal.Rows - 1);
    }

    [Fact]
    public void PageUp_MovesToTop()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.DownArrow, '\0');
        echo.ProcessKey(ConsoleKey.DownArrow, '\0');
        echo.ProcessKey(ConsoleKey.PageUp, '\0');

        echo.CursorY.Should().Be(0);
    }

    [Fact]
    public void PageDown_MovesToBottom()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.PageDown, '\0');

        echo.CursorY.Should().Be(EchoTerminal.Rows - 1);
    }

    [Fact]
    public void WrapLine_AtEnd()
    {
        var (_, echo) = Create();
        for (int i = 0; i < EchoTerminal.Cols; i++)
            echo.ProcessKey(ConsoleKey.A, 'a');

        echo.CursorX.Should().Be(0);
        echo.CursorY.Should().Be(1);
    }

    [Fact]
    public void WrapLine_StopsAtLastRow()
    {
        var (_, echo) = Create();
        for (int i = 0; i < EchoTerminal.Cols * EchoTerminal.Rows; i++)
            echo.ProcessKey(ConsoleKey.A, 'a');

        echo.CursorY.Should().Be(EchoTerminal.Rows - 1);
    }

    [Fact]
    public void TickBlink_TogglesVisibility()
    {
        var (_, echo) = Create();
        echo.CursorVisible.Should().BeTrue();

        for (int i = 0; i < 25; i++)
            echo.TickBlink();

        echo.CursorVisible.Should().BeFalse();

        for (int i = 0; i < 25; i++)
            echo.TickBlink();

        echo.CursorVisible.Should().BeTrue();
    }

    [Fact]
    public void Reset_ClearsScreen()
    {
        var (screen, echo) = Create();
        echo.ProcessKey(ConsoleKey.A, 'a');
        echo.ProcessKey(ConsoleKey.Enter, '\r');

        echo.Reset();

        echo.CursorX.Should().Be(0);
        echo.CursorY.Should().Be(0);
        screen.GetChar(0, 0).Should().Be(' ');
    }

    [Fact]
    public void NonPrintableChar_Ignored()
    {
        var (_, echo) = Create();
        echo.ProcessKey(ConsoleKey.Tab, '\t');

        echo.CursorX.Should().Be(0);
    }

    [Fact]
    public void TypeMultipleWords()
    {
        var (screen, echo) = Create();
        foreach (char c in "Hello World")
            echo.ProcessKey(c == ' ' ? ConsoleKey.Spacebar : ConsoleKey.A, c);

        screen.GetChar(0, 0).Should().Be('H');
        screen.GetChar(4, 0).Should().Be('o');
        screen.GetChar(5, 0).Should().Be(' ');
        screen.GetChar(6, 0).Should().Be('W');
        screen.GetChar(10, 0).Should().Be('d');
        echo.CursorX.Should().Be(11);
    }
}
