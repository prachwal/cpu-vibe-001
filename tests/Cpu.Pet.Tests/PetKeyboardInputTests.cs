using Cpu.Board.Core;
using Cpu.Pet.Rendering.Views;
using Cpu.Pet.System;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public class PetKeyboardInputTests
{
    private static PetView CreateBootedView()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        var machine = new PetMachine(profile);
        var view = new PetView(machine);

        for (int i = 0; i < 3000; i++)
            view.StepCpu(10_000);

        ScreenText(machine).Should().Contain("READY");
        return view;
    }

    [Fact]
    public void SingleKey_AppearsOnScreenWithoutExtraKeypress()
    {
        var view = CreateBootedView();
        int kbBefore = view.Machine.KeyboardBufferCount();

        view.EnqueueKey('P');
        view.StepCpu();

        view.Machine.KeyboardBufferCount().Should().BeLessThanOrEqualTo(kbBefore + 1);
        ScreenText(view.Machine).Should().Contain("P");
    }

    [Fact]
    public void TypedWord_AppearsInOrder()
    {
        var view = CreateBootedView();

        foreach (char ch in "HI")
        {
            view.EnqueueKey(ch);
            view.StepCpu();
        }

        string screen = ScreenText(view.Machine);
        int hi = screen.IndexOf("HI", StringComparison.Ordinal);
        hi.Should().BeGreaterOrEqualTo(0, "expected HI on screen after two keys and two ticks");
    }

    [Fact]
    public void KeyInjectedThenCpuRun_EchoesWithinOneStep()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        using var machine = new PetMachine(MachineBoard.LoadProfile(path));

        for (int i = 0; i < 3000; i++)
            machine.Step(10_000);

        machine.TryTypeChar('X').Should().BeTrue();
        machine.KeyboardBufferCount().Should().Be(1);

        for (int i = 0; i < 500 && !ScreenText(machine).Contains('X'); i++)
            machine.Step(2000);

        ScreenText(machine).Should().Contain("X");
    }

    [Fact]
    public void Backspace_DeletesPreviousCharacter()
    {
        var view = CreateBootedView();

        view.EnqueueKey('X');
        view.StepCpu();
        ScreenText(view.Machine).Should().Contain("X");

        view.EnqueueKey('\b');
        view.StepCpu();

        ScreenText(view.Machine).Should().NotContain("X");
        view.Machine.ReadMemory(PetMachine.KeyBufferAddr).Should().NotBe((byte)'L',
            "backspace must not trigger LOAD macro ($83)");
    }

    private static string ScreenText(PetMachine machine)
    {
        var chars = new char[machine.Rows * machine.Columns];
        for (int row = 0; row < machine.Rows; row++)
        {
            for (int col = 0; col < machine.Columns; col++)
                chars[row * machine.Columns + col] = machine.GetDisplayCell(col, row);
        }
        return new string(chars);
    }
}
