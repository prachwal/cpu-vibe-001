using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cpu.Tui.Configuration;

public sealed class TuiSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _path;

    public TuiSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(AppContext.BaseDirectory, "tui-settings.json");
    }

    public string FilePath => _path;

    public TuiAppSettings Load()
    {
        if (!File.Exists(_path))
            return TuiAppSettings.Default.Clone();

        try
        {
            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<TuiAppSettings>(json, JsonOptions)?.Clone()
                   ?? TuiAppSettings.Default.Clone();
        }
        catch
        {
            return TuiAppSettings.Default.Clone();
        }
    }

    public void Save(TuiAppSettings settings)
    {
        string? dir = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_path, json);
    }
}
