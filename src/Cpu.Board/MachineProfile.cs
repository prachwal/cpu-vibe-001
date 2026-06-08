using System.Text.Json.Serialization;

namespace Cpu.Board.Core;

public class MachineProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("cpu")]
    public CpuProfile Cpu { get; set; } = new();

    [JsonPropertyName("memory")]
    public MemoryRegion[] Memory { get; set; } = [];

    [JsonPropertyName("pia")]
    public PiaProfile? Pia { get; set; }

    [JsonPropertyName("vic")]
    public VicProfile? Vic { get; set; }

    [JsonPropertyName("via")]
    public ViaProfile? Via { get; set; }

    [JsonPropertyName("display")]
    public DisplayProfile? Display { get; set; }
}

public class CpuProfile
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("resetVector")]
    public string ResetVector { get; set; } = "0xFFFC";

    [JsonIgnore]
    public ushort ResetVectorAddress => HexHelper.ParseHex(ResetVector);

    [JsonPropertyName("entryPoint")]
    public string? EntryPoint { get; set; }

    [JsonIgnore]
    public ushort? EntryPointAddress => EntryPoint != null ? HexHelper.ParseHex(EntryPoint) : null;
}

public class MemoryRegion
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("start")]
    public string Start { get; set; } = "";

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("file")]
    public string? File { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonIgnore]
    public ushort StartAddress => HexHelper.ParseHex(Start);

    [JsonIgnore]
    public int SizeBytes
    {
        get
        {
            if (Size == null) return 0;
            string s = Size.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                s = s[2..];
            return int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out int v) ? v : 0;
        }
    }
}

public class PiaProfile
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("portA")]
    public PortConfig? PortA { get; set; }

    [JsonPropertyName("portB")]
    public PortConfig? PortB { get; set; }

    [JsonIgnore]
    public ushort BaseAddress => HexHelper.ParseHex(Address);
}

public class PortConfig
{
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "input";

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("connector")]
    public string Connector { get; set; } = "none";
}

public class VicProfile
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = "0x9000";

    [JsonPropertyName("pal")]
    public bool Pal { get; set; }

    [JsonIgnore]
    public ushort BaseAddress => HexHelper.ParseHex(Address);
}

public class ViaProfile
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = "0x9110";

    [JsonPropertyName("mirrorEnd")]
    public string? MirrorEnd { get; set; }

    [JsonIgnore]
    public ushort BaseAddress => HexHelper.ParseHex(Address);

    [JsonIgnore]
    public ushort? MirrorEndAddress => MirrorEnd != null ? HexHelper.ParseHex(MirrorEnd) : null;
}

public class DisplayProfile
{
    [JsonPropertyName("cols")]
    public int Cols { get; set; } = 40;

    [JsonPropertyName("rows")]
    public int Rows { get; set; } = 24;

    [JsonPropertyName("font")]
    public string? Font { get; set; }
}

public static class ProfileHelper
{
    public static (string Label, string File, int Cols, int Rows)[] LoadProfiles(string profilePrefix)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "profiles");
        if (!Directory.Exists(dir))
            return [];

        var results = new List<(string, string, int, int)>();
        foreach (string file in Directory.GetFiles(dir, "*.json"))
        {
            string name = Path.GetFileName(file);
            if (profilePrefix != null && !name.StartsWith(profilePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                string json = File.ReadAllText(file);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                string label = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? name : name;
                int cols = 40, rows = 25;
                if (doc.RootElement.TryGetProperty("display", out var d))
                {
                    if (d.TryGetProperty("cols", out var c)) cols = c.GetInt32();
                    if (d.TryGetProperty("rows", out var r)) rows = r.GetInt32();
                }
                doc.Dispose();
                results.Add((label, name, cols, rows));
            }
            catch
            {
                results.Add((name, name, 40, 25));
            }
        }
        return results.ToArray();
    }
}

public static class HexHelper
{
    public static ushort ParseHex(string? s)
    {
        if (s == null) return 0;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s[2..];
        return Convert.ToUInt16(s, 16);
    }
}
