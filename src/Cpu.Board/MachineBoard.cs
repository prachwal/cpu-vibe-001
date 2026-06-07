using CpuBase;
using Mos6502Cpu = Mos6502.Core.Cpu;
using MosCpuVariant = Mos6502.CpuVariant;

namespace Cpu.Board.Core;

public sealed class MachineBoard : IDisposable
{
    private readonly MachineProfile _profile;
    private readonly Mos6502Cpu _cpu;
    private readonly Bus _bus;
    private readonly List<IDevice> _devices = [];

    public MachineProfile Profile => _profile;
    public Mos6502Cpu Cpu => _cpu;
    public Bus Bus => _bus;
    public IReadOnlyList<IDevice> Devices => _devices;

    public MachineBoard(MachineProfile profile)
    {
        _profile = profile;

        var romRoot = Path.Combine(AppContext.BaseDirectory, "roms");
        var profileDir = profile.Name.ToLowerInvariant().Replace(' ', '-');

        _bus = new Bus();

        foreach (var region in profile.Memory)
        {
            switch (region.Type.ToLowerInvariant())
            {
                case "ram":
                {
                    var ram = new RamDevice(region.Label ?? "RAM", region.StartAddress, region.SizeBytes);
                    _devices.Add(ram);
                    _bus.Attach(ram);
                    break;
                }
                case "rom":
                {
                    byte[] data = [];
                    if (region.File != null)
                    {
                        string romPath = Path.Combine(romRoot, profileDir, region.File);
                        if (File.Exists(romPath))
                            data = File.ReadAllBytes(romPath);
                    }
                    var rom = new RomDevice(region.Label ?? "ROM", region.StartAddress, data);
                    _devices.Add(rom);
                    _bus.Attach(rom);
                    break;
                }
            }
        }

        var memoryBridge = new BusBackedMemory(_bus);
        _cpu = new Mos6502Cpu(memoryBridge, profile.Cpu.Type.ToLowerInvariant() switch
        {
            "mos6502" => MosCpuVariant.Mos6502,
            "65c02" => MosCpuVariant.Wdc65C02,
            "2a03" => MosCpuVariant.Ricoh2A03,
            _ => MosCpuVariant.Mos6502
        });
    }

    public void Reset()
    {
        _bus.Reset();
        _cpu.Reset();
        if (_profile.Cpu.EntryPointAddress is ushort ep)
            _cpu.Regs.PC = ep;
    }

    public void Step()
    {
        _cpu.Step();
    }

    public void Run(long cycles)
    {
        long target = _cpu.Cycles + cycles;
        while (_cpu.Cycles < target)
            _cpu.Step();
    }

    public void AttachDevice(IDevice device)
    {
        _devices.Add(device);
        _bus.Attach(device);
    }

    public static MachineProfile LoadProfile(string path)
    {
        string json = File.ReadAllText(path);
        var result = System.Text.Json.JsonSerializer.Deserialize<MachineProfile>(json);
        return result ?? throw new InvalidOperationException("Failed to deserialize profile");
    }

    public void Dispose()
    {
        _cpu.Memory.Reset();
    }
}

public sealed class BusBackedMemory : Mos6502.Core.Memory
{
    private readonly Bus _bus;

    public BusBackedMemory(Bus bus) => _bus = bus;

    public override byte Read(ushort address) => _bus.Read(address);

    public override void Write(ushort address, byte value) => _bus.Write(address, value);
}
