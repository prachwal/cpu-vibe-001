using Cpu.Pet.System;
using Xunit;
using Xunit.Abstractions;

namespace Cpu.Pet.Tests;

public class CheckPetProfiles
{
    private readonly ITestOutputHelper _output;
    public CheckPetProfiles(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("pet-2001-8-b1.json", 40)]
    [InlineData("pet-2001-32-b2.json", 40)]
    [InlineData("pet-4032-b4.json", 40)]
    [InlineData("pet-8032-b4.json", 80)]
    public void Profile_BootsToBasic(string profile, int cols)
    {
        using var machine = PetMachine.Load(profile, cols, 25);
        machine.Reset();
        machine.Run(5_000_000);

        int nonSpace = 0;
        for (int i = 0; i < cols * 25; i++)
        {
            byte b = machine.Board.Cpu.Memory.Read((ushort)(0x8000 + i));
            if (b != 0 && b != 0x20 && b != 0xA0)
                nonSpace++;
        }
        _output.WriteLine($"{profile}: {nonSpace} non-space chars on screen");
        Assert.True(nonSpace > 10, $"{profile} should show BASIC banner (got {nonSpace} chars)");
    }

    [Theory]
    [InlineData("pet-2001-8-b1.json", 40)]
    [InlineData("pet-2001-32-b2.json", 40)]
    [InlineData("pet-4032-b4.json", 40)]
    [InlineData("pet-8032-b4.json", 80)]
    public void Profile_CanTypePrint1(string profile, int cols)
    {
        using var machine = PetMachine.Load(profile, cols, 25);
        machine.Reset();
        machine.Run(5_000_000);

        foreach (char ch in "PRINT 1")
            machine.TypeChar(ch);
        machine.Run(1_000_000);

        bool found = false;
        for (int i = 0; i < cols * 25; i++)
        {
            byte b = machine.Board.Cpu.Memory.Read((ushort)(0x8000 + i));
            if (b == (byte)'1')
            {
                found = true;
                break;
            }
        }
        _output.WriteLine($"{profile}: PRINT 1 → {(found ? "OK" : "FAIL")}");
        Assert.True(found, $"{profile} should display '1' after typing PRINT 1");
    }
}
