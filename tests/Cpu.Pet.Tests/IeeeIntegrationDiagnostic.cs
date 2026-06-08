using Cpu.Board.Core;
using Cpu.Pet.System;
using Xunit;
using FluentAssertions;

namespace Cpu.Pet.Tests;

public sealed class IeeeIntegrationDiagnostic
{
    private static string D => Path.Combine(
        AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    [Fact]
    public void Boot4032AndLoadDir()
    {
        var profile = MachineBoard.LoadProfile(
            Path.Combine(AppContext.BaseDirectory, "profiles", "pet-4032-b4.json"));
        using var machine = new PetMachine(profile, 40, 25);
        machine.MountDisk(Path.Combine(D, "games-1.d64"));
        machine.Reset();

        for (int b = 0; b < 600; b++) machine.Step(10_000);

        // Re-inicjalizuj wektory IEEE po boot BASIC 4 (który je czyści)
        machine.ReinitIeeeVectors();

        // Verify vectors
        global::System.Console.Error.WriteLine($"POST-BOOT vectors:");
        ushort[] va = [0x033C, 0x033E, 0x0340, 0x0342, 0x0344, 0x0346];
        string[] vn = ["ACPTR","CIOUT","UNTLK","UNLSN","LISTEN","TALK"];
        for (int i = 0; i < va.Length; i++)
        {
            byte lo = machine.ReadMemory(va[i]);
            byte hi = machine.ReadMemory((ushort)(va[i] + 1));
            global::System.Console.Error.WriteLine($"  ${va[i]:X4} ({vn[i]}): ${hi:X2}{lo:X2}");
        }
        // Check trampolines
        for (ushort a = 0x301; a <= 0x312; a++)
            global::System.Console.Error.Write($" ${machine.ReadMemory(a):02X}");
        global::System.Console.Error.WriteLine();

        machine.IeeeBus.SetLogFile("/tmp/ieee-trace.txt");
        foreach (char ch in "LOAD\"$\",8\r") machine.EnqueueChar(ch);

        int dioCh = 0; byte lastDio = 0;
        for (int b = 0; b < 2000; b++)
        {
            machine.ProcessPendingInput();
            machine.Step(10_000);
            if (machine.IeeeBus.LastDio != lastDio && machine.IeeeBus.LastDio != 0 && machine.IeeeBus.LastDio != 0xFF)
            {
                lastDio = machine.IeeeBus.LastDio;
                dioCh++;
                if (dioCh <= 15)
                    global::System.Console.Error.WriteLine($"  DIO#{dioCh} = ${lastDio:X2}");
            }
        }

        var screen = string.Concat(
            Enumerable.Range(0, 25 * 40)
                .Select(i => machine.GetDisplayCell(i % 40, i / 40)));
        global::System.Console.Error.WriteLine($"\nTOTAL DIO: {dioCh}\nScreen:\n{screen}");
    }

    [Fact]
    public void CheckVectorsAndDio()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json");
        var profile = MachineBoard.LoadProfile(path);
        using var machine = new PetMachine(profile);
        machine.MountDisk(Path.Combine(D, "games-1.d64"));
        machine.Reset();

        var hex = new global::System.Text.StringBuilder();
        for (ushort a = 0xFFA0; a <= 0xFFBF; a++)
            hex.Append($" {machine.ReadMemory(a):X2}");
        global::System.Console.Error.WriteLine($"VECTORS:{hex}");

        for (int b = 0; b < 600; b++) machine.Step(10_000);
        hex.Clear();
        for (ushort a = 0xFFA0; a <= 0xFFBF; a++)
            hex.Append($" {machine.ReadMemory(a):X2}");
        global::System.Console.Error.WriteLine($"VECTORS after boot:{hex}");

        foreach (char ch in "LOAD\"$\",8\r") machine.EnqueueChar(ch);

        int dioCh = 0; byte lastDio = 0;
        for (int b = 0; b < 8000; b++)
        {
            machine.ProcessPendingInput();
            machine.Step(10_000);
            if (machine.IeeeBus.LastDio != lastDio && machine.IeeeBus.LastDio != 0 && machine.IeeeBus.LastDio != 0xFF)
            {
                lastDio = machine.IeeeBus.LastDio;
                dioCh++;
                global::System.Console.Error.WriteLine($"  DIO#{dioCh} = ${lastDio:X2}");
            }
        }
        var screen = new string(Enumerable.Range(0, 25 * 40)
            .Select(i => machine.GetDisplayCell(i % 40, i / 40)).ToArray());
        global::System.Console.Error.WriteLine($"TOTAL DIO: {dioCh}\nScreen:\n{screen}");
    }
}
