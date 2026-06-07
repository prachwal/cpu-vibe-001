using Cpu.Pet.Devices;
using FluentAssertions;
using Xunit;

namespace Cpu.Pet.Tests;

public class PetSciiTests
{
    [Theory]
    [InlineData(0x01, 'A')]
    [InlineData(0x1A, 'Z')]
    [InlineData(0x20, ' ')]
    [InlineData(0x30, '0')]
    [InlineData(0x41, 'a')]
    [InlineData(0x5A, 'z')]
    [InlineData(0x40, '@')]
    public void ToDisplayChar_MapsScreenCodes(byte code, char expected)
    {
        PetScii.ToDisplayChar(code).Should().Be(expected);
    }

    [Theory]
    [InlineData(0x41, 0x01)]
    [InlineData(0x5A, 0x1A)]
    public void NormalizeVideoRam_ConvertsTypedAsciiToScreenCode(byte input, byte expected)
    {
        PetScii.NormalizeVideoRam(input).Should().Be(expected);
    }

    [Theory]
    [InlineData('A', 0x41)]
    [InlineData('a', 0x41)]
    [InlineData('0', 0x30)]
    [InlineData('\r', 0x0D)]
    [InlineData('\b', 0x83)]
    public void HostCharToKeyboardCode_MapsHostInput(char ch, byte expected)
    {
        PetScii.HostCharToKeyboardCode(ch).Should().Be(expected);
    }
}
