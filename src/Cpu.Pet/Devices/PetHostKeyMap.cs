namespace Cpu.Pet.Devices;

/// <summary>
/// PET 2001 host keyboard ↔ PETSCII key buffer codes.
/// Single source for input translation and reference UI.
/// </summary>
public static class PetHostKeyMap
{
    public static IReadOnlyList<PetHostKeyBinding> SpecialKeys { get; } =
    [
        new("Return", PetKeyboardCodes.Return, "Enter",
            MapsFromHost: true, HostChar: '\r', HostKey: ConsoleKey.Enter),
        new("Delete/←", PetKeyboardCodes.DeleteLeft, "Backspace",
            MapsFromHost: true, HostChar: '\b', HostKey: ConsoleKey.Backspace),
        new("Delete/←", PetKeyboardCodes.DeleteLeft, "Delete",
            MapsFromHost: true, HostChar: '\x7F', HostKey: ConsoleKey.Delete),
        new("Space", PetKeyboardCodes.Space, "Space",
            MapsFromHost: true, HostChar: ' ', HostKey: ConsoleKey.Spacebar),
        new("Cursor up", 0x11, "Up",
            MapsFromHost: true, HostKey: ConsoleKey.UpArrow),
        new("Cursor down", 0x1D, "Down",
            MapsFromHost: true, HostKey: ConsoleKey.DownArrow),
        new("Cursor right", 0x12, "Right",
            MapsFromHost: true, HostKey: ConsoleKey.RightArrow),
        new("Insert", 0x13, "(PET Ins)"),
        new("Home/Clear", 0x5E, "(PET Home)"),
        new("RVS off", 0x00, "(PET RVS off)"),
        new("LOAD macro", PetKeyboardCodes.LoadDirectCommand, "(PET key $83)"),
    ];

    public static IReadOnlyList<PetHostKeyBinding> PanelRows { get; } =
        SpecialKeys
            .GroupBy(b => b.PetCode)
            .Select(g =>
            {
                PetHostKeyBinding first = g.First();
                string hosts = string.Join(", ", g.Select(x => x.HostLabel).Distinct());
                return first with { HostLabel = hosts };
            })
            .OrderBy(b => b.PetCode)
            .ToArray();

    public static bool TryMapHostChar(char ch, out byte petCode)
    {
        foreach (PetHostKeyBinding binding in SpecialKeys)
        {
            if (!binding.MapsFromHost || binding.HostChar != ch)
                continue;
            petCode = binding.PetCode;
            return true;
        }

        petCode = 0;
        return false;
    }

    public static bool TryMapConsoleKey(ConsoleKeyInfo key, out byte petCode)
    {
        foreach (PetHostKeyBinding binding in SpecialKeys)
        {
            if (!binding.MapsFromHost)
                continue;

            if (binding.HostKey == key.Key)
            {
                petCode = binding.PetCode;
                return true;
            }

            if (binding.HostChar.HasValue && key.KeyChar == binding.HostChar.Value)
            {
                petCode = binding.PetCode;
                return true;
            }
        }

        petCode = 0;
        return false;
    }

    public static string FormatPanelLine(PetHostKeyBinding binding, int width)
    {
        string pet = binding.PetLabel;
        if (pet.Length > 10)
            pet = pet[..10];

        string code = $"${binding.PetCode:X2}";
        string host = binding.HostLabel;
        int hostW = Math.Max(1, width - 12);
        if (host.Length > hostW)
            host = host[..hostW];

        string line = string.Format("{0,-" + hostW + "} {1,10} {2}", host, pet, code);
        if (line.Length > width)
            line = line[..width];
        else if (line.Length < width)
            line += new string(' ', width - line.Length);
        return line;
    }
}
