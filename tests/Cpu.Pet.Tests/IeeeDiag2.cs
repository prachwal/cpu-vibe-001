using Cpu.Board.Core;
using Cpu.Pet.System;
using Xunit;
using FluentAssertions;

namespace Cpu.Pet.Tests;

public sealed class IeeeDiag2
{
    private static string D => Path.Combine(
        AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    [Fact]
    public void Boot4032LoadDir()
    {
        var profile = MachineBoard.LoadProfile(
            Path.Combine(AppContext.BaseDirectory, "profiles", "pet-4032-b4.json"));
        using var m = new PetMachine(profile, 40, 25);
        m.MountDisk(Path.Combine(D, "games-1.d64"));
        m.Reset();

        for (int b = 0; b < 600; b++) m.Step(10_000);
        m.ReinitIeeeVectors();

        var sb = new global::System.Text.StringBuilder();
        ushort[] va = [0x033C, 0x033E, 0x0340, 0x0342, 0x0344, 0x0346];
        string[] vn = ["ACPTR","CIOUT","UNTLK","UNLSN","LISTEN","TALK"];
        for (int i = 0; i < 6; i++)
        {
            byte lo = m.ReadMemory(va[i]);
            byte hi = m.ReadMemory((ushort)(va[i]+1));
            sb.AppendLine($"  ${va[i]:X4} ({vn[i]}): ${hi:X2}{lo:X2}");
        }
        // Check trampoline
        sb.Append("  $0401: ");
        sb.AppendLine($"{m.ReadMemory(0x0401):X2} {m.ReadMemory(0x0402):X2} {m.ReadMemory(0x0403):X2}");
        global::System.Console.Error.WriteLine(sb.ToString());

        foreach (char ch in "LOAD\"$\",8\r") m.EnqueueChar(ch);

        int dc = 0; byte ld = 0;
        for (int b = 0; b < 10000; b++)
        {
            m.ProcessPendingInput();
            m.Step(10_000);
            if (m.IeeeBus.LastDio != ld && m.IeeeBus.LastDio != 0 && m.IeeeBus.LastDio != 0xFF)
            {
                ld = m.IeeeBus.LastDio; dc++;
                if (dc <= 20) global::System.Console.Error.WriteLine($"  DIO#{dc} = ${ld:X2}");
            }
        }

        var scr = string.Concat(Enumerable.Range(0, 25*40)
            .Select(i => m.GetDisplayCell(i%40, i/40)));
        global::System.Console.Error.WriteLine($"TOTAL DIO: {dc}\nScreen:\n{scr}");
    }
}
