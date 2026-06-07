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
    public ushort SizeBytes => Size != null ? HexHelper.ParseHex(Size) : (ushort)0;
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

public class DisplayProfile
{
    [JsonPropertyName("cols")]
    public int Cols { get; set; } = 40;

    [JsonPropertyName("rows")]
    public int Rows { get; set; } = 24;

    [JsonPropertyName("font")]
    public string? Font { get; set; }
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
