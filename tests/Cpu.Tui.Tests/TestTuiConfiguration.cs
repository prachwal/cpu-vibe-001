using Cpu.Tui;
using Cpu.Tui.Configuration;

namespace Cpu.Tui.Tests;

public static class TestTuiConfiguration
{
    public static ITuiAppConfiguration Create(TuiAppSettings? initial = null)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cpu-vibe-tui-{Guid.NewGuid():N}.json");
        var store = new TuiSettingsStore(path);
        if (initial != null)
            store.Save(initial.Clone());
        return new TuiAppConfiguration(store);
    }
}
