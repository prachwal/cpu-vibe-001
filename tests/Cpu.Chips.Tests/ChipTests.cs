using Cpu.Chips.Crtc6545;
using Cpu.Chips.Via6522;
using FluentAssertions;
using Xunit;

namespace Cpu.Chips.Tests;

public class Crtc6545Tests
{
    [Fact]
    public void InitPet40Column_SetsDisplayWidth()
    {
        var crtc = new CRTC6545Chip(0xE880);
        crtc.SetRegister(CRTC6545Constants.R0_HORIZONTAL_TOTAL, 63);
        crtc.SetRegister(CRTC6545Constants.R1_HORIZONTAL_DISPLAY, 40);
        crtc.SetRegister(CRTC6545Constants.R6_VERTICAL_DISPLAY, 25);
        crtc.UpdateDisplayStart();

        crtc.R1.Should().Be(40);
        crtc.R6.Should().Be(25);
    }

    [Fact]
    public void Update_ProducesVSyncPulses()
    {
        var crtc = new CRTC6545Chip(0xE880);
        crtc.SetRegister(CRTC6545Constants.R0_HORIZONTAL_TOTAL, 10);
        crtc.SetRegister(CRTC6545Constants.R1_HORIZONTAL_DISPLAY, 8);
        crtc.SetRegister(CRTC6545Constants.R4_VERTICAL_TOTAL, 5);
        crtc.SetRegister(CRTC6545Constants.R6_VERTICAL_DISPLAY, 4);
        crtc.UpdateDisplayStart();

        bool sawVSync = false;
        for (int i = 0; i < 500 && !sawVSync; i++)
        {
            crtc.Update();
            if (crtc.VSync)
                sawVSync = true;
        }

        sawVSync.Should().BeTrue();
    }
}

public class Via6522Tests
{
    [Fact]
    public void Reset_ClearsInterrupt()
    {
        var via = new VIA6522Chip(0xE840);
        via.Reset();
        via.HasInterrupt.Should().BeFalse();
    }

    [Fact]
    public void IerWrite_EnablesCa1Interrupt()
    {
        var via = new VIA6522Chip(0xE840);
        via.WriteByte(0xE84E, 0x82);
        via.IER.Should().Be(0x02);
    }
}
