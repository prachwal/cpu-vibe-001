using System.Text.Json.Serialization;

namespace Cpu.Tui.Modules;

public sealed class AppConfig
{
    [JsonPropertyName("modules")]
    public ModuleConfig[] Modules { get; set; } = [];

    [JsonPropertyName("keyBindings")]
    public Dictionary<string, string> KeyBindings { get; set; } = [];

    public static AppConfig Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return new AppConfig();
        string json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }
}

public sealed class ModuleConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string TypeName { get; set; } = "";

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("showInBar")]
    public bool ShowInBar { get; set; } = true;

    [JsonIgnore]
    public ConsoleKey? ConsoleKey => Key != null ? (ConsoleKey)Enum.Parse(typeof(ConsoleKey), Key) : null;
}
