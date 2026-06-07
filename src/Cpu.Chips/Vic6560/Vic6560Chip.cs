namespace Cpu.Chips.Vic6560;

public sealed class Vic6560Chip
{
    private readonly byte[] _reg = new byte[Vic6560Constants.RegisterCount];
    private int _rasterCounter;
    private readonly double[] _phase = new double[4];
    private ushort _noiseLfsr = 0xFFFF;
    private bool _palMode;

    public Vic6560Chip(ushort baseAddress = 0x9000)
    {
        BaseAddress = baseAddress;
        Reset();
    }

    public ushort BaseAddress { get; }

    public byte this[int reg] => _reg[reg & 0x0F];

    public int Raster => ((_reg[0x03] & 0x80) << 1) | _reg[0x04];
    public int ScreenX => _reg[0x00] & 0x7F;
    public int ScreenY => _reg[0x01];
    public int Columns => _reg[0x02] & 0x7F;
    public int Rows => (_reg[0x03] >> 1) & 0x3F;
    public bool DoubleHeightChars => (_reg[0x03] & 0x01) != 0;
    public int ScreenMatrixBase => ((_reg[0x05] & 0xF0) << 6) | ((_reg[0x02] & 0x80) << 2);
    public int CharMatrixBase => (_reg[0x05] & 0x0F) << 10;
    public int ScreenAddr => Vic6560Constants.ToCpuAddress(ScreenMatrixBase);
    public int CharAddr => Vic6560Constants.ToCpuAddress(CharMatrixBase);
    public byte AuxColor => (byte)((_reg[0x0E] >> 4) & 0x0F);
    public byte Volume => (byte)(_reg[0x0E] & 0x0F);
    public byte ScreenColor => (byte)((_reg[0x0F] >> 4) & 0x0F);
    public bool ReverseMode => (_reg[0x0F] & 0x08) != 0;
    public byte BorderColor => (byte)(_reg[0x0F] & 0x07);
    public bool Osc1Enabled => (_reg[0x0A] & 0x80) != 0;
    public byte Osc1FreqRaw => (byte)(_reg[0x0A] & 0x7F);
    public double Osc1Frequency => CalcFreq(Osc1FreqRaw, 256);
    public bool Osc2Enabled => (_reg[0x0B] & 0x80) != 0;
    public byte Osc2FreqRaw => (byte)(_reg[0x0B] & 0x7F);
    public double Osc2Frequency => CalcFreq(Osc2FreqRaw, 128);
    public bool Osc3Enabled => (_reg[0x0C] & 0x80) != 0;
    public byte Osc3FreqRaw => (byte)(_reg[0x0C] & 0x7F);
    public double Osc3Frequency => CalcFreq(Osc3FreqRaw, 64);
    public bool NoiseEnabled => (_reg[0x0D] & 0x80) != 0;
    public byte NoiseFreqRaw => (byte)(_reg[0x0D] & 0x7F);
    public double NoiseFrequency => CalcFreq(NoiseFreqRaw, 32);
    public bool InterlaceMode => (_reg[0x00] & 0x80) != 0;
    public bool PalMode => _palMode;
    public int TotalScanlines => _palMode ? Vic6560Constants.TotalScanlinesPal : Vic6560Constants.TotalScanlinesNtsc;
    public int CyclesPerLine => _palMode ? Vic6560Constants.CyclesPerLinePal : Vic6560Constants.CyclesPerLineNtsc;

    public byte LightPenX
    {
        set => _reg[0x06] = value;
    }

    public byte LightPenY
    {
        set => _reg[0x07] = value;
    }

    public byte PaddleX
    {
        set => _reg[0x08] = value;
    }

    public byte PaddleY
    {
        set => _reg[0x09] = value;
    }

    public bool HasInterrupt => false;

    public bool AcknowledgeInterrupt() => false;

    public void SetPalMode(bool pal) => _palMode = pal;

    public byte ReadByte(ushort address)
    {
        return _reg[(address - BaseAddress) & 0x0F];
    }

    public void WriteByte(ushort address, byte value)
    {
        _reg[(address - BaseAddress) & 0x0F] = value;
    }

    public void Reset()
    {
        Array.Clear(_reg, 0, _reg.Length);
        _rasterCounter = 0;
        _noiseLfsr = 0xFFFF;
        for (int i = 0; i < 4; i++)
            _phase[i] = 0;
    }

    public void Update()
    {
        _rasterCounter++;
        if (_rasterCounter >= TotalScanlines)
            _rasterCounter = 0;
        _reg[0x04] = (byte)(_rasterCounter & 0xFF);
        if ((_rasterCounter & 0x100) != 0)
            _reg[0x03] |= 0x80;
        else
            _reg[0x03] &= 0x7F;
    }

    public void AdvanceOscillators(double dt)
    {
        if (Osc1Enabled)
            AdvancePhase(0, Osc1Frequency, dt);
        if (Osc2Enabled)
            AdvancePhase(1, Osc2Frequency, dt);
        if (Osc3Enabled)
            AdvancePhase(2, Osc3Frequency, dt);
        if (NoiseEnabled)
        {
            _phase[3] += NoiseFrequency * dt;
            while (_phase[3] >= 1.0)
            {
                _phase[3] -= 1.0;
                ClockNoise();
            }
        }
    }

    public float GetAudioSample()
    {
        float sample = 0;
        int count = 0;
        if (Osc1Enabled) { sample += GetSquare(0); count++; }
        if (Osc2Enabled) { sample += GetSquare(1); count++; }
        if (Osc3Enabled) { sample += GetSquare(2); count++; }
        if (NoiseEnabled) { sample += GetNoiseSample(); count++; }
        if (count == 0)
            return 0;
        return sample / count * Volume / 15.0f;
    }

    private void AdvancePhase(int idx, double freq, double dt)
    {
        _phase[idx] += freq * dt;
        if (_phase[idx] >= 1.0)
            _phase[idx] -= Math.Floor(_phase[idx]);
    }

    private float GetSquare(int idx) => _phase[idx] < 0.5 ? 1.0f : -1.0f;

    private float GetNoiseSample() => (_noiseLfsr & 1) == 0 ? 1.0f : -1.0f;

    private void ClockNoise()
    {
        ushort bit = (ushort)(((_noiseLfsr >> 14) ^ (_noiseLfsr >> 13)) & 1);
        _noiseLfsr = (ushort)((_noiseLfsr << 1) | bit);
    }

    private double CalcFreq(byte raw, int divider)
    {
        double phi2 = _palMode ? Vic6560Constants.Phi2Pal : Vic6560Constants.Phi2Ntsc;
        return phi2 / divider / (255 - raw + 1);
    }
}
