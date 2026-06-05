namespace CpuVibe.Instructions;

/// <summary>
/// Lookup table: opcode (byte) → handler instrukcji.
/// 256 wpisów, stały czas dekodowania O(1).
/// </summary>
public class InstructionTable
{
    public InstructionHandler[] Handlers = new InstructionHandler[256];

    public InstructionTable()
    {
        for (int i = 0; i < 256; i++)
            Handlers[i] = Nop.Nop.Execute;

        InitBase6502();
    }

    private InstructionTable(InstructionTable other)
    {
        Array.Copy(other.Handlers, Handlers, 256);
    }

    public static InstructionTable Create(CpuVariant variant)
    {
        var table = new InstructionTable();
        switch (variant)
        {
            case CpuVariant.Wdc65C02:
            case CpuVariant.Wdc65C816:
                table.AddWdc65C02Extensions();
                break;
        }
        return table;
    }

    private void InitBase6502()
    {
        Handlers[0x69] = AdcImmediate.Execute;
        Handlers[0x65] = AdcZeroPage.Execute;
        Handlers[0x75] = AdcZeroPageX.Execute;
        Handlers[0x6D] = AdcAbsolute.Execute;
        Handlers[0x7D] = AdcAbsoluteX.Execute;
        Handlers[0x79] = AdcAbsoluteY.Execute;
        Handlers[0x61] = AdcIndirectX.Execute;
        Handlers[0x71] = AdcIndirectY.Execute;

        Handlers[0x29] = AndImmediate.Execute;
        Handlers[0x25] = AndZeroPage.Execute;
        Handlers[0x35] = AndZeroPageX.Execute;
        Handlers[0x2D] = AndAbsolute.Execute;
        Handlers[0x3D] = AndAbsoluteX.Execute;
        Handlers[0x39] = AndAbsoluteY.Execute;
        Handlers[0x21] = AndIndirectX.Execute;
        Handlers[0x31] = AndIndirectY.Execute;

        Handlers[0x0A] = AslAccumulator.Execute;
        Handlers[0x06] = AslZeroPage.Execute;
        Handlers[0x16] = AslZeroPageX.Execute;
        Handlers[0x0E] = AslAbsolute.Execute;
        Handlers[0x1E] = AslAbsoluteX.Execute;

        Handlers[0x90] = BccRelative.Execute;
        Handlers[0xB0] = BcsRelative.Execute;
        Handlers[0xF0] = BeqRelative.Execute;
        Handlers[0x24] = BitZeroPage.Execute;
        Handlers[0x2C] = BitAbsolute.Execute;
        Handlers[0x30] = BmiRelative.Execute;
        Handlers[0xD0] = BneRelative.Execute;
        Handlers[0x10] = BplRelative.Execute;
        Handlers[0x00] = BrkImplied.Execute;
        Handlers[0x50] = BvcRelative.Execute;
        Handlers[0x70] = BvsRelative.Execute;

        Handlers[0x18] = ClcImplied.Execute;
        Handlers[0xD8] = CldImplied.Execute;
        Handlers[0x58] = CliImplied.Execute;
        Handlers[0xB8] = ClvImplied.Execute;

        Handlers[0xC9] = CmpImmediate.Execute;
        Handlers[0xC5] = CmpZeroPage.Execute;
        Handlers[0xD5] = CmpZeroPageX.Execute;
        Handlers[0xCD] = CmpAbsolute.Execute;
        Handlers[0xDD] = CmpAbsoluteX.Execute;
        Handlers[0xD9] = CmpAbsoluteY.Execute;
        Handlers[0xC1] = CmpIndirectX.Execute;
        Handlers[0xD1] = CmpIndirectY.Execute;

        Handlers[0xE0] = CpxImmediate.Execute;
        Handlers[0xE4] = CpxZeroPage.Execute;
        Handlers[0xEC] = CpxAbsolute.Execute;

        Handlers[0xC0] = CpyImmediate.Execute;
        Handlers[0xC4] = CpyZeroPage.Execute;
        Handlers[0xCC] = CpyAbsolute.Execute;

        Handlers[0xC6] = DecZeroPage.Execute;
        Handlers[0xD6] = DecZeroPageX.Execute;
        Handlers[0xCE] = DecAbsolute.Execute;
        Handlers[0xDE] = DecAbsoluteX.Execute;
        Handlers[0xCA] = DexImplied.Execute;
        Handlers[0x88] = DeyImplied.Execute;

        Handlers[0x49] = EorImmediate.Execute;
        Handlers[0x45] = EorZeroPage.Execute;
        Handlers[0x55] = EorZeroPageX.Execute;
        Handlers[0x4D] = EorAbsolute.Execute;
        Handlers[0x5D] = EorAbsoluteX.Execute;
        Handlers[0x59] = EorAbsoluteY.Execute;
        Handlers[0x41] = EorIndirectX.Execute;
        Handlers[0x51] = EorIndirectY.Execute;

        Handlers[0xE6] = IncZeroPage.Execute;
        Handlers[0xF6] = IncZeroPageX.Execute;
        Handlers[0xEE] = IncAbsolute.Execute;
        Handlers[0xFE] = IncAbsoluteX.Execute;
        Handlers[0xE8] = InxImplied.Execute;
        Handlers[0xC8] = InyImplied.Execute;

        Handlers[0x4C] = JmpAbsolute.Execute;
        Handlers[0x6C] = JmpIndirect.Execute;
        Handlers[0x20] = JsrAbsolute.Execute;

        Handlers[0xA9] = LdaImmediate.Execute;
        Handlers[0xA5] = LdaZeroPage.Execute;
        Handlers[0xB5] = LdaZeroPageX.Execute;
        Handlers[0xAD] = LdaAbsolute.Execute;
        Handlers[0xBD] = LdaAbsoluteX.Execute;
        Handlers[0xB9] = LdaAbsoluteY.Execute;
        Handlers[0xA1] = LdaIndirectX.Execute;
        Handlers[0xB1] = LdaIndirectY.Execute;

        Handlers[0xA2] = LdxImmediate.Execute;
        Handlers[0xA6] = LdxZeroPage.Execute;
        Handlers[0xB6] = LdxZeroPageY.Execute;
        Handlers[0xAE] = LdxAbsolute.Execute;
        Handlers[0xBE] = LdxAbsoluteY.Execute;

        Handlers[0xA0] = LdyImmediate.Execute;
        Handlers[0xA4] = LdyZeroPage.Execute;
        Handlers[0xB4] = LdyZeroPageX.Execute;
        Handlers[0xAC] = LdyAbsolute.Execute;
        Handlers[0xBC] = LdyAbsoluteX.Execute;

        Handlers[0x4A] = LsrAccumulator.Execute;
        Handlers[0x46] = LsrZeroPage.Execute;
        Handlers[0x56] = LsrZeroPageX.Execute;
        Handlers[0x4E] = LsrAbsolute.Execute;
        Handlers[0x5E] = LsrAbsoluteX.Execute;

        Handlers[0xEA] = Nop.Nop.Execute;

        Handlers[0x09] = OraImmediate.Execute;
        Handlers[0x05] = OraZeroPage.Execute;
        Handlers[0x15] = OraZeroPageX.Execute;
        Handlers[0x0D] = OraAbsolute.Execute;
        Handlers[0x1D] = OraAbsoluteX.Execute;
        Handlers[0x19] = OraAbsoluteY.Execute;
        Handlers[0x01] = OraIndirectX.Execute;
        Handlers[0x11] = OraIndirectY.Execute;

        Handlers[0x48] = PhaImplied.Execute;
        Handlers[0x08] = PhpImplied.Execute;
        Handlers[0x68] = PlaImplied.Execute;
        Handlers[0x28] = PlpImplied.Execute;

        Handlers[0x2A] = RolAccumulator.Execute;
        Handlers[0x26] = RolZeroPage.Execute;
        Handlers[0x36] = RolZeroPageX.Execute;
        Handlers[0x2E] = RolAbsolute.Execute;
        Handlers[0x3E] = RolAbsoluteX.Execute;

        Handlers[0x6A] = RorAccumulator.Execute;
        Handlers[0x66] = RorZeroPage.Execute;
        Handlers[0x76] = RorZeroPageX.Execute;
        Handlers[0x6E] = RorAbsolute.Execute;
        Handlers[0x7E] = RorAbsoluteX.Execute;

        Handlers[0x40] = RtiImplied.Execute;
        Handlers[0x60] = RtsImplied.Execute;

        Handlers[0xE9] = SbcImmediate.Execute;
        Handlers[0xE5] = SbcZeroPage.Execute;
        Handlers[0xF5] = SbcZeroPageX.Execute;
        Handlers[0xED] = SbcAbsolute.Execute;
        Handlers[0xFD] = SbcAbsoluteX.Execute;
        Handlers[0xF9] = SbcAbsoluteY.Execute;
        Handlers[0xE1] = SbcIndirectX.Execute;
        Handlers[0xF1] = SbcIndirectY.Execute;

        Handlers[0x38] = SecImplied.Execute;
        Handlers[0xF8] = SedImplied.Execute;
        Handlers[0x78] = SeiImplied.Execute;

        Handlers[0x85] = StaZeroPage.Execute;
        Handlers[0x95] = StaZeroPageX.Execute;
        Handlers[0x8D] = StaAbsolute.Execute;
        Handlers[0x9D] = StaAbsoluteX.Execute;
        Handlers[0x99] = StaAbsoluteY.Execute;
        Handlers[0x81] = StaIndirectX.Execute;
        Handlers[0x91] = StaIndirectY.Execute;

        Handlers[0x86] = StxZeroPage.Execute;
        Handlers[0x96] = StxZeroPageY.Execute;
        Handlers[0x8E] = StxAbsolute.Execute;

        Handlers[0x84] = StyZeroPage.Execute;
        Handlers[0x94] = StyZeroPageX.Execute;
        Handlers[0x8C] = StyAbsolute.Execute;

        Handlers[0xAA] = TaxImplied.Execute;
        Handlers[0xA8] = TayImplied.Execute;
        Handlers[0xBA] = TsxImplied.Execute;
        Handlers[0x8A] = TxaImplied.Execute;
        Handlers[0x9A] = TxsImplied.Execute;
        Handlers[0x98] = TyaImplied.Execute;
    }

    private void AddWdc65C02Extensions()
    {
        Handlers[0x80] = Bra.BraRelative.Execute;

        Handlers[0xDA] = Phx.PhxImplied.Execute;
        Handlers[0xFA] = Plx.PlxImplied.Execute;
        Handlers[0x5A] = Phy.PhyImplied.Execute;
        Handlers[0x7A] = Ply.PlyImplied.Execute;

        Handlers[0x64] = Stz.StzZeroPage.Execute;
        Handlers[0x74] = Stz.StzZeroPageX.Execute;
        Handlers[0x9C] = Stz.StzAbsolute.Execute;
        Handlers[0x9E] = Stz.StzAbsoluteX.Execute;

        Handlers[0x14] = Trb.TrbZeroPage.Execute;
        Handlers[0x1C] = Trb.TrbAbsolute.Execute;
        Handlers[0x04] = Tsb.TsbZeroPage.Execute;
        Handlers[0x0C] = Tsb.TsbAbsolute.Execute;

        Handlers[0x89] = Bit.BitImmediate.Execute;
        Handlers[0x34] = Bit.BitZeroPageX.Execute;
        Handlers[0x3C] = Bit.BitAbsoluteX.Execute;

        Handlers[0x1A] = Inc.IncAccumulator.Execute;
        Handlers[0x3A] = Dec.DecAccumulator.Execute;

        Handlers[0x5C] = Nop.Nop.Execute;
        Handlers[0x7C] = Jmp.JmpIndirectX.Execute;
        Handlers[0x7E] = Stz.StzAbsoluteX.Execute;
    }
}
