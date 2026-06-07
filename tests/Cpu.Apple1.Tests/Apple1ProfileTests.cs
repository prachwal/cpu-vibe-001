using Cpu.Apple1.Adapters;
using Cpu.Apple1.Rendering.Views;
using Cpu.Board.Core;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Apple1.Tests;

public class Apple1ProfileTests
{
    [Theory]
    [InlineData("apple-1.json", "Apple 1")]
    [InlineData("apple-1-basic.json", "Apple 1 BASIC")]
    public void Profile_LoadsAndCreatesBoard(string profileFile, string _)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", profileFile);
        var profile = MachineBoard.LoadProfile(path);
        using var board = new MachineBoard(profile);
        board.Cpu.Should().NotBeNull();
        board.Bus.Should().NotBeNull();
        board.Devices.Count.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void BasicProfile_Print1ShowsDigit()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "apple-1-basic.json");
        var profile = MachineBoard.LoadProfile(path);
        using var board = new MachineBoard(profile);
        var pia = new PiaDevice(profile.Pia!.BaseAddress);
        board.AttachDevice(pia);

        var display = new Apple1DisplayAdapter(40, 24);
        var keyboard = new Apple1KeyboardAdapter();
        var view = new Apple1View(board, pia, display, keyboard, "BASIC");
        using var stream = new MemoryStream();
        using var renderer = new AnsiTerminalRenderer(stream);

        renderer.Resize(100, 30);
        view.Activate(renderer, new TermRect(0, 0, 100, 29));

        view.EnqueueText("PRINT 1\r");

        for (int i = 0; i < 500; i++)
            view.StepCpu(5000);

        var sb = new System.Text.StringBuilder();
        for (int row = 0; row < 24; row++)
        {
            for (int col = 0; col < 40; col++)
                sb.Append(display.GetChar(col, row));
            sb.AppendLine();
        }
        var screen = sb.ToString();
        Assert.Contains("1", screen);
    }

    [Fact]
    public void WozMonitor_ReachesPrompt()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "apple-1.json");
        var profile = MachineBoard.LoadProfile(path);
        using var board = new MachineBoard(profile);
        var pia = new PiaDevice(profile.Pia!.BaseAddress);
        board.AttachDevice(pia);

        var display = new Apple1DisplayAdapter(40, 24);
        var keyboard = new Apple1KeyboardAdapter();
        var view = new Apple1View(board, pia, display, keyboard, "Woz Monitor");
        using var stream = new MemoryStream();
        using var renderer = new AnsiTerminalRenderer(stream);

        renderer.Resize(100, 30);
        view.Activate(renderer, new TermRect(0, 0, 100, 29));

        for (int i = 0; i < 200; i++)
            view.StepCpu(5000);

        var sb = new System.Text.StringBuilder();
        for (int row = 0; row < 24; row++)
        {
            for (int col = 0; col < 40; col++)
                sb.Append(display.GetChar(col, row));
            sb.AppendLine();
        }
        var screen = sb.ToString();
        Assert.NotEmpty(screen.Trim());
    }
}
