namespace Cpu.Chips.Via6522;

public static class VIA6522Constants
{
    public const int REGISTER_COUNT = 16;

    public const int ORB_OFFSET = 0x00;
    public const int ORA_OFFSET = 0x01;
    public const int DDRB_OFFSET = 0x02;
    public const int DDRA_OFFSET = 0x03;
    public const int T1CL_OFFSET = 0x04;
    public const int T1CH_OFFSET = 0x05;
    public const int T1LL_OFFSET = 0x06;
    public const int T1LH_OFFSET = 0x07;
    public const int T2CL_OFFSET = 0x08;
    public const int T2CH_OFFSET = 0x09;
    public const int SR_OFFSET = 0x0A;
    public const int ACR_OFFSET = 0x0B;
    public const int PCR_OFFSET = 0x0C;
    public const int IFR_OFFSET = 0x0D;
    public const int IER_OFFSET = 0x0E;
    public const int ORA_NOHANDSHAKE_OFFSET = 0x0F;

    public const byte IFR_TIMER1_BIT = 6;
    public const byte IFR_TIMER2_BIT = 5;
    public const byte IFR_CB1_BIT = 4;
    public const byte IFR_CB2_BIT = 3;
    public const byte IFR_SR_BIT = 2;
    public const byte IFR_CA1_BIT = 1;
    public const byte IFR_CA2_BIT = 0;
    public const byte IFR_IRQ_BIT = 7;

    public const byte IFR_TIMER1_MASK = 1 << IFR_TIMER1_BIT;
    public const byte IFR_TIMER2_MASK = 1 << IFR_TIMER2_BIT;
    public const byte IFR_CB1_MASK = 1 << IFR_CB1_BIT;
    public const byte IFR_CB2_MASK = 1 << IFR_CB2_BIT;
    public const byte IFR_SR_MASK = 1 << IFR_SR_BIT;
    public const byte IFR_CA1_MASK = 1 << IFR_CA1_BIT;
    public const byte IFR_CA2_MASK = 1 << IFR_CA2_BIT;
    public const byte IFR_IRQ_MASK = 1 << IFR_IRQ_BIT;

    public const byte IER_SET_CLEAR_BIT = 7;
    public const byte IER_SET_CLEAR_MASK = 1 << IER_SET_CLEAR_BIT;

    public const byte ACR_PA_LATCH_BIT = 0;
    public const byte ACR_PB_LATCH_BIT = 1;
    public const byte ACR_SR_SHIFT_DIR_BIT = 2;
    public const byte ACR_SR_CLOCK_BIT3 = 3;
    public const byte ACR_SR_CLOCK_BIT4 = 4;
    public const byte ACR_T2_CONTROL_BIT = 5;
    public const byte ACR_T1_CONTROL_BIT = 6;
    public const byte ACR_T1_OUTPUT_BIT = 7;

    public const byte ACR_PA_LATCH_MASK = 1 << ACR_PA_LATCH_BIT;
    public const byte ACR_PB_LATCH_MASK = 1 << ACR_PB_LATCH_BIT;
    public const byte ACR_SR_MODE_MASK = 0x1C;
    public const byte ACR_T2_CONTROL_MASK = 1 << ACR_T2_CONTROL_BIT;
    public const byte ACR_T1_CONTROL_MASK = 1 << ACR_T1_CONTROL_BIT;
    public const byte ACR_T1_OUTPUT_MASK = 1 << ACR_T1_OUTPUT_BIT;

    public const byte PCR_CA1_EDGE_BIT = 0;
    public const byte PCR_CA2_CONTROL_MASK = 0x0E;
    public const byte PCR_CB1_EDGE_BIT = 4;
    public const byte PCR_CB2_CONTROL_MASK = 0xE0;

    public const byte PCR_CA1_EDGE_MASK = 1 << PCR_CA1_EDGE_BIT;
    public const byte PCR_CB1_EDGE_MASK = 1 << PCR_CB1_EDGE_BIT;

    public const byte T1_ONE_SHOT = 0;
    public const byte T1_FREE_RUN = 1;

    public const byte T2_ONE_SHOT = 0;
    public const byte T2_PULSE_COUNT = 1;

    public const byte SR_DISABLED = 0x00;
    public const byte SR_IN_T2 = 0x04;
    public const byte SR_IN_PHI2 = 0x08;
    public const byte SR_IN_CB1 = 0x0C;
    public const byte SR_OUT_FREE = 0x10;
    public const byte SR_OUT_T2 = 0x14;
    public const byte SR_OUT_PHI2 = 0x18;
    public const byte SR_OUT_CB1 = 0x1C;

    public const byte CB2_INPUT_NEG = 0x00;
    public const byte CB2_IND_INT_NEG = 0x20;
    public const byte CB2_INPUT_POS = 0x40;
    public const byte CB2_IND_INT_POS = 0x60;
    public const byte CB2_HANDSHAKE = 0x80;
    public const byte CB2_PULSE = 0xA0;
    public const byte CB2_LOW = 0xC0;
    public const byte CB2_HIGH = 0xE0;

    public const byte CA2_INPUT_NEG = 0x00;
    public const byte CA2_IND_INT_NEG = 0x02;
    public const byte CA2_INPUT_POS = 0x04;
    public const byte CA2_IND_INT_POS = 0x06;
    public const byte CA2_HANDSHAKE = 0x08;
    public const byte CA2_PULSE = 0x0A;
    public const byte CA2_LOW = 0x0C;
    public const byte CA2_HIGH = 0x0E;
}
