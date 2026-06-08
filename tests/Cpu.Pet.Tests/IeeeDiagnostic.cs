using Cpu.Board.Core;
using Cpu.Pet.System;
using Xunit;
using FluentAssertions;

namespace Cpu.Pet.Tests;

public sealed class IeeeDiagTests
{
    private static string D => Path.Combine(
        AppContext.BaseDirectory, "../../../../../src/Cpu.Pet/roms/pet-test-disks");

    [Fact]
    public void TraceLoad()
    {
        var profile = MachineBoard.LoadProfile(
            Path.Combine(AppContext.BaseDirectory, "profiles", "pet-2001-32.json"));
        using var m = new PetMachine(profile);
        m.MountDisk(Path.Combine(D, "games-1.d64"));
        m.Reset();

        for (int i = 0; i < 600; i++) m.Step(10_000);

        foreach (char ch in "LOAD\"$\",8\r") m.EnqueueChar(ch);

        int dioWrites = 0;
        var captured = new global::System.Collections.Generic.List<byte>();
        
        for (int i = 0; i < 8000; i++)
        {
            m.ProcessPendingInput();
            
            byte lastDio = m.IeeeBus.LastDio;
            m.Step(10_000);
            
            if (m.IeeeBus.LastDio != lastDio && m.IeeeBus.LastDio != 0 && m.IeeeBus.LastDio != 0xFF)
            {
                if (dioWrites < 10)
                    captured.Add(m.IeeeBus.LastDio);
                dioWrites++;
            }
        }

        global::System.Console.Error.WriteLine($"DIO writes: {dioWrites}");
        global::System.Console.Error.WriteLine($"DIO writes: {dioWrites}");
        global::System.Console.Error.WriteLine($"Bytes: {string.Join(",", captured.ConvertAll(b => $"${b:X2}"))}");

        var sb = new global::System.Text.StringBuilder();
        for (int r = 0; r < 25; r++)
        {
            var line = new char[40];
            for (int c = 0; c < 40; c++)
                line[c] = m.GetDisplayCell(c, r);
            string t = new string(line).TrimEnd();
            if (t.Length > 0) sb.AppendLine($"  [{r}] {t}");
        }
        global::System.Console.Error.WriteLine(sb.ToString());
    }
}
