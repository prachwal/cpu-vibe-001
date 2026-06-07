using Cpu.Board.Core;
using Cpu.Chips.Crtc6545;
using Cpu.Chips.Via6522;
using Cpu.Pet.Devices;
using CpuBase;
using Mos6502.Core;

namespace Cpu.Pet.System;

public sealed class PetMachine : IDisposable
{
    public const ushort VideoRamBase = 0x8000;
    public const ushort KeyCountAddr = 0x009E;
    public const ushort KeyBufferAddr = 0x026F;
    public const int MaxKeyBuffer = 10;
    public const ushort CursorColAddr = 0x00C6;
    public const ushort ScreenPtrLo = 0x00C4;
    public const ushort ScreenPtrHi = 0x00C5;

    private readonly MachineBoard _board;
    private readonly PetPia6520 _pia;
    private readonly Via6522Device _via;
    private readonly Crtc6545Device _crtc;
    private readonly PetKeyboardMatrix _keyboard;
    private bool _previousDisplayEnable;

    public MachineBoard Board => _board;
    public PetPia6520 Pia => _pia;
    public PetKeyboardMatrix Keyboard => _keyboard;
    public int Columns { get; }
    public int Rows { get; }

    public PetMachine(MachineProfile profile, int columns = 40, int rows = 25)
    {
        Columns = columns;
        Rows = rows;
        _keyboard = new PetKeyboardMatrix();
        var binding = new PetKeyboardPiaBinding(_keyboard);

        _board = new MachineBoard(profile);
        _pia = new PetPia6520(0xE810, binding);
        _via = new Via6522Device(0xE840);
        _crtc = new Crtc6545Device(0xE880);

        _board.AttachDevice(_pia);
        _board.AttachDevice(_via);
        _board.AttachDevice(_crtc);

        InitPetCrtc(_crtc.Chip, columns);
        _previousDisplayEnable = _crtc.Chip.DisplayEnable;
        Reset();
    }

    public static PetMachine Load(string profileFile, int columns = 40, int rows = 25)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "profiles", profileFile);
        var profile = MachineBoard.LoadProfile(path);
        return new PetMachine(profile, columns, rows);
    }

    public void Reset()
    {
        _keyboard.ReleaseAll();
        _board.Reset();
        InitPetCrtc(_crtc.Chip, Columns);
        _previousDisplayEnable = _crtc.Chip.DisplayEnable;
    }

    public void Step(long cycles = 1)
    {
        for (long i = 0; i < cycles; i++)
        {
            TickDevices(1);
            UpdateIrq();
            _board.Step();
        }
    }

    public void Run(long cycles) => Step(cycles);

    public byte ReadMemory(ushort address) => _board.Cpu.Memory.Read(address);

    public void WriteMemory(ushort address, byte value) => _board.Cpu.Memory.Write(address, value);

    public void TypeChar(char ch)
    {
        byte code = PetScii.HostCharToKeyboardCode(ch);
        if (code == 0)
            return;

        int count = ReadMemory(KeyCountAddr);
        if (count >= MaxKeyBuffer)
            return;

        WriteMemory((ushort)(KeyBufferAddr + count), code);
        WriteMemory(KeyCountAddr, (byte)(count + 1));
    }

    public void PressKey(char ch)
    {
        if (!PetKeyMapper.TryMapCharacter(ch, out int row, out int col))
            return;
        _keyboard.PressKey(row, col);
    }

    public void ReleaseAllKeys() => _keyboard.ReleaseAll();

    public char GetDisplayCell(int col, int row)
    {
        if ((uint)col >= (uint)Columns || (uint)row >= (uint)Rows)
            return ' ';

        ushort addr = (ushort)(VideoRamBase + row * Columns + col);
        byte code = PetScii.NormalizeVideoRam(ReadMemory(addr));
        return PetScii.ToDisplayChar(code);
    }

    public (int Col, int Row) GetCursorPosition()
    {
        int col = ReadMemory(CursorColAddr);
        if (col >= Columns)
            col = 0;

        uint ptr = (uint)(ReadMemory(ScreenPtrLo) | (ReadMemory(ScreenPtrHi) << 8));
        int row = 0;
        if (ptr >= VideoRamBase && ptr < VideoRamBase + (uint)(Rows * Columns))
            row = (int)((ptr - VideoRamBase) / Columns);

        return (col, row);
    }

    private void TickDevices(long cycles)
    {
        for (long i = 0; i < cycles; i++)
        {
            _crtc.Chip.Update();
            _via.Chip.CA1 = _crtc.Chip.VSync;
            _via.Chip.Update();

            byte portB = _via.Chip.PortBExternalInput;
            if (_crtc.Chip.DisplayEnable)
                portB = (byte)(portB | 0x20);
            else
                portB = (byte)(portB & ~0x20);
            _via.Chip.PortBExternalInput = portB;

            bool currentDe = _crtc.Chip.DisplayEnable;
            if (_previousDisplayEnable && !currentDe)
                _pia.SignalCB1(false);
            else if (!_previousDisplayEnable && currentDe)
                _pia.SignalCB1(true);
            _previousDisplayEnable = currentDe;
        }
    }

    private void UpdateIrq()
    {
        _board.Cpu.IrqAsserted = _pia.IrqPending || _via.HasInterrupt;
    }

    private static void InitPetCrtc(CRTC6545Chip crtc, int columns)
    {
        if (columns == 80)
        {
            crtc.SetRegister(CRTC6545Constants.R0_HORIZONTAL_TOTAL, 100);
            crtc.SetRegister(CRTC6545Constants.R1_HORIZONTAL_DISPLAY, 80);
            crtc.SetRegister(CRTC6545Constants.R2_HORIZONTAL_SYNC_POS, 86);
            crtc.SetRegister(CRTC6545Constants.R3_SYNC_WIDTHS, 0x1E);
        }
        else
        {
            crtc.SetRegister(CRTC6545Constants.R0_HORIZONTAL_TOTAL, 63);
            crtc.SetRegister(CRTC6545Constants.R1_HORIZONTAL_DISPLAY, 40);
            crtc.SetRegister(CRTC6545Constants.R2_HORIZONTAL_SYNC_POS, 52);
            crtc.SetRegister(CRTC6545Constants.R3_SYNC_WIDTHS, 0x0C);
        }

        crtc.SetRegister(CRTC6545Constants.R4_VERTICAL_TOTAL, 31);
        crtc.SetRegister(CRTC6545Constants.R5_VERTICAL_ADJUST, columns == 80 ? (byte)4 : (byte)1);
        crtc.SetRegister(CRTC6545Constants.R6_VERTICAL_DISPLAY, 25);
        crtc.SetRegister(CRTC6545Constants.R7_VERTICAL_SYNC_POS, 28);
        crtc.SetRegister(CRTC6545Constants.R9_SCAN_LINES, 7);
        crtc.SetRegister(CRTC6545Constants.R10_CURSOR_START, 0x00);
        crtc.SetRegister(CRTC6545Constants.R11_CURSOR_END, 0x07);
        crtc.SetRegister(CRTC6545Constants.R12_DISPLAY_START_H, 0x00);
        crtc.SetRegister(CRTC6545Constants.R13_DISPLAY_START_L, 0x00);
        crtc.UpdateDisplayStart();
    }

    public void Dispose() => _board.Dispose();
}
