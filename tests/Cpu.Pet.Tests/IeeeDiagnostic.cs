using Cpu.Board.Core;
using Cpu.Pet.System;
using Xunit;
using FluentAssertions;

namespace Cpu.Pet.Tests;

public sealed class IeeeFullLoadTests
{
    private static string TestDisksDir =>
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    [Fact]
    public void CheckVectorsAfterInit()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        using var machine = new PetMachine(profile);
        machine.MountDisk(Path.Combine(TestDisksDir, "games-1.d64"));
        machine.Reset();

        // Check vectors RIGHT AFTER reset
        var sb = new global::System.Text.StringBuilder();
        ushort[] addrs = [0x033C, 0x033E, 0x0340, 0x0342, 0x0344, 0x0346, 0x202E, 0x2030];
        string[] names = ["IACPTR","ICIOUT","IUNTLK","IUNLSN","ILISTN","ITALK","LISTEN","TALK"];
        for (int i = 0; i < addrs.Length; i++)
        {
            byte lo = machine.ReadMemory(addrs[i]);
            byte hi = machine.ReadMemory((ushort)(addrs[i] + 1));
            sb.AppendLine($"  ${addrs[i]:X4} ({names[i]}): ${hi:X2}{lo:X2}");
        }
        global::System.Console.Error.WriteLine(sb.ToString());

        // Boot and type LOAD
        for (int i = 0; i < 600; i++)
            machine.Step(10_000);

        machine.ReinitIeeeVectors();

        string cmd = "LOAD\"$\",8\r";
        foreach (char ch in cmd)
            machine.EnqueueChar(ch);

        for (int i = 0; i < 6000; i++)
        {
            machine.ProcessPendingInput();
            machine.Step(10_000);
        }

        // Show screen
        sb.Clear();
        for (int row = 0; row < machine.Rows; row++)
        {
            var line = new char[machine.Columns];
            for (int col = 0; col < machine.Columns; col++)
                line[col] = machine.GetDisplayCell(col, row);
            string text = new string(line).TrimEnd();
            if (text.Length > 0)
                sb.AppendLine($"  [{row}] {text}");
        }
        global::System.Console.Error.WriteLine(sb.ToString());
    }
}
