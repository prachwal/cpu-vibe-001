using Cpu.Board.Core;
using Cpu.Board.Core.Adapters;

string profilePath = args.Length > 0 ? args[0] : "profiles/apple-1.json";
string fullPath = Path.Combine(AppContext.BaseDirectory, profilePath);

Console.WriteLine($"Loading profile: {fullPath}");
Console.WriteLine($"File exists: {File.Exists(fullPath)}");

if (!File.Exists(fullPath))
{
    Console.Error.WriteLine($"Profile not found: {fullPath}");
    return;
}

var profile = MachineBoard.LoadProfile(fullPath);
Console.WriteLine($"Profile: {profile.Name}");
Console.WriteLine($"CPU: {profile.Cpu.Type}, Reset: ${profile.Cpu.ResetVectorAddress:X4}");
Console.WriteLine($"Memory regions: {profile.Memory.Length}");
foreach (var m in profile.Memory)
    Console.WriteLine($"  {m.Type} ${m.StartAddress:X4} size=${m.SizeBytes:X4} file={m.File ?? "(inline)"}");

Console.WriteLine($"PIA: ${profile.Pia?.BaseAddress:X4}");
Console.WriteLine($"Display: {profile.Display?.Cols}x{profile.Display?.Rows}");

Console.WriteLine("\nBuilding machine...");
using var board = new MachineBoard(profile);

Console.WriteLine($"CPU created: {board.Cpu.GetType().Name}");
Console.WriteLine($"Devices on bus: {board.Devices.Count}");
foreach (var d in board.Devices)
    Console.WriteLine($"  {d.Name}");

Console.WriteLine("\nAttaching PIA at bus level...");

var romList = board.Devices.OfType<CpuBase.RomDevice>().ToList();
Console.WriteLine($"ROM devices: {romList.Count}");
foreach (var rom in romList)
    Console.WriteLine($"  {rom.Name}: ${rom.Start:X4}-${rom.End:X4}");

var ramList = board.Devices.OfType<CpuBase.RamDevice>().ToList();
Console.WriteLine($"RAM devices: {ramList.Count}");
foreach (var ram in ramList)
    Console.WriteLine($"  {ram.Name}: ${ram.Start:X4}-${ram.End:X4}");

Console.WriteLine("\nResetting board...");
board.Reset();

Console.WriteLine($"PC after reset: ${board.Cpu.Regs.PC:X4}");
Console.WriteLine($"SP after reset: ${board.Cpu.Regs.SP:X2}");
Console.WriteLine($"A after reset: ${board.Cpu.Regs.A:X2}");

byte resetLo = board.Cpu.Memory.Read(0xFFFC);
byte resetHi = board.Cpu.Memory.Read(0xFFFD);
Console.WriteLine($"Reset vector [$FFFC]: ${resetLo:X2} ${resetHi:X2} -> ${(resetHi << 8) | resetLo:X4}");

byte wozFirst = board.Cpu.Memory.Read(0xFF00);
Console.WriteLine($"First byte of Woz Monitor [$FF00]: ${wozFirst:X2}");

if (wozFirst == 0)
{
    Console.Error.WriteLine("ERROR: Woz Monitor ROM appears empty (first byte = 0)");
    string expectedPath = Path.Combine(AppContext.BaseDirectory, "roms", "apple-1", "wozmon.bin");
    Console.Error.WriteLine($"Expected path: {expectedPath}");
    Console.Error.WriteLine($"File exists: {File.Exists(expectedPath)}");
    if (File.Exists(expectedPath))
    {
        var data = File.ReadAllBytes(expectedPath);
        Console.Error.WriteLine($"File size: {data.Length} bytes");
        Console.Error.WriteLine($"First bytes: {string.Join(" ", data.Take(8).Select(b => $"${b:X2}"))}");
    }
    return;
}

Console.WriteLine($"Woz Monitor loaded: {256} bytes at $FF00");
int nonZero = 0;
for (int i = 0; i < 256; i++)
    if (board.Cpu.Memory.Read((ushort)(0xFF00 + i)) != 0) nonZero++;
Console.WriteLine($"Non-zero bytes in Woz Monitor: {nonZero}/256");

Console.WriteLine("\nStepping CPU 50 instructions...");
for (int i = 0; i < 50; i++)
{
    ushort pcBefore = board.Cpu.Regs.PC;
    byte opcode = board.Cpu.Memory.Read(pcBefore);
    board.Step();

    if (i < 10 || i >= 40)
        Console.WriteLine($"  {i}: PC=${pcBefore:X4} op=${opcode:X2} A=${board.Cpu.Regs.A:X2} X=${board.Cpu.Regs.X:X2} Y=${board.Cpu.Regs.Y:X2} SP=${board.Cpu.Regs.SP:X2} cycles={board.Cpu.Cycles}");
}
Console.WriteLine($"  ... ({40} instructions skipped)");

Console.WriteLine($"\nTotal cycles after 50 steps: {board.Cpu.Cycles}");

Console.WriteLine("\nTesting display adapter...");
var display = new Apple1DisplayAdapter(40, 24);
display.Write((byte)'H');
display.Write((byte)'e');
display.Write((byte)'l');
display.Write((byte)'l');
display.Write((byte)'o');
display.Write(0x0D);
display.Write((byte)'W');
display.Write((byte)'o');
display.Write((byte)'r');
display.Write((byte)'l');
display.Write((byte)'d');

Console.Write("  Display row 0: ");
for (int c = 0; c < 5; c++) Console.Write(display.GetChar(c, 0));
Console.WriteLine();
Console.Write("  Display row 1: ");
for (int c = 0; c < 5; c++) Console.Write(display.GetChar(c, 1));
Console.WriteLine();

Console.WriteLine("\n=== DIAGNOSTIC COMPLETE ===");
Console.WriteLine($"Profile '{profile.Name}' validates OK.");
