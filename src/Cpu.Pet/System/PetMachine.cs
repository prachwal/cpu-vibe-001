using Cpu.Board.Core;
using Cpu.Chips.Crtc6545;
using Cpu.Chips.Via6522;
using Cpu.Pet.Devices;
using Cpu.Pet.Devices.CbmDos;
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
    private readonly PetIeeeBus _ieeeBus;
    private readonly PetIeeeDiskDrive _diskDrive;
    private bool _previousDisplayEnable;
    private bool _crtcInitialized;

    public MachineBoard Board => _board;
    public PetPia6520 Pia => _pia;
    public PetKeyboardMatrix Keyboard => _keyboard;
    public PetIeeeBus IeeeBus => _ieeeBus;
    public PetIeeeDiskDrive DiskDrive => _diskDrive;
    public int Columns { get; }
    public int Rows { get; }
    public bool CursorVisible => _crtc.Chip.CursorEnable || !_crtcInitialized;

    public PetMachine(MachineProfile profile, int columns = 40, int rows = 25)
    {
        Columns = columns;
        Rows = rows;
        _keyboard = new PetKeyboardMatrix();
        _ieeeBus = new PetIeeeBus();
        _diskDrive = new PetIeeeDiskDrive(8);
        _ieeeBus.AttachDevice(_diskDrive);

        var bindingA = new PetKeyboardPiaBinding(_keyboard);
        var bindingB = new PetIeeePortBBinding(_keyboard, _ieeeBus);

        _board = new MachineBoard(profile);
        _pia = new PetPia6520(0xE810, bindingA, bindingB);
        _via = new Via6522Device(0xE840);
        _crtc = new Crtc6545Device(0xE880);

        _via.Chip.OnPortBWrite = (value) =>
        {
            _ieeeBus.OnATNWrite((value & 0x04) != 0);
        };

        _board.AttachDevice(_pia);
        _board.AttachDevice(_via);
        _board.AttachDevice(_crtc);

        InitPetCrtc(_crtc.Chip, columns);
        _crtcInitialized = true;
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
        _ieeeBus.Reset();
        InitPetCrtc(_crtc.Chip, Columns);
        _previousDisplayEnable = _crtc.Chip.DisplayEnable;
    }

    public void MountDisk(string d64Path)
    {
        var image = D64Image.Load(d64Path);
        _diskDrive.Engine.AttachImage(image);
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

    private const int MaxEchoPolls = 400;
    private const long EchoPollCycles = 500;
    private readonly Queue<byte> _pendingPetCodes = new();
    private readonly Queue<char> _pendingChars = new();

    public void EnqueuePetCode(byte code) => _pendingPetCodes.Enqueue(code);

    public void EnqueueChar(char ch) => _pendingChars.Enqueue(ch);

    public void ProcessPendingInput()
    {
        ProcessPetCodes();
        ProcessChars();
    }

    private void ProcessPetCodes()
    {
        while (_pendingPetCodes.Count > 0)
        {
            if (KeyboardBufferCount() >= MaxKeyBuffer)
                break;

            if (!TryWriteKeyboardBuffer(_pendingPetCodes.Dequeue()))
                break;

            DrainKeyboardBuffer();
        }
    }

    private void ProcessChars()
    {
        while (_pendingChars.Count > 0)
        {
            if (KeyboardBufferCount() >= MaxKeyBuffer)
                break;

            char ch = _pendingChars.Dequeue();
            byte code = PetScii.HostCharToKeyboardCode(ch);
            if (code == 0 || !TryWriteKeyboardBuffer(code))
                break;

            DrainKeyboardBuffer();
        }
    }

    private void DrainKeyboardBuffer()
    {
        for (int i = 0; i < MaxEchoPolls && KeyboardBufferCount() > 0; i++)
            Run(EchoPollCycles);
    }

    public bool TryTypeChar(char ch)
    {
        byte code = PetScii.HostCharToKeyboardCode(ch);
        if (code == 0)
            return true;

        return TryWriteKeyboardBuffer(code);
    }

    public void TypeChar(char ch) => TryTypeChar(ch);

    public int KeyboardBufferCount() =>
        _board.Bus.Read(KeyCountAddr);

    private bool TryWriteKeyboardBuffer(byte code)
    {
        int count = KeyboardBufferCount();
        if (count >= MaxKeyBuffer)
            return false;

        _board.Bus.Write((ushort)(KeyBufferAddr + count), code);
        _board.Bus.Write(KeyCountAddr, (byte)(count + 1));
        return true;
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
            _ieeeBus.Tick();

            byte portB = _ieeeBus.GetViaPortBInput();
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
