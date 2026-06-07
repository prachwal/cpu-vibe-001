namespace Cpu.Tui.Configuration;

public sealed class TuiAppConfiguration : ITuiAppConfiguration
{
    private readonly TuiSettingsStore _store;
    private TuiAppSettings _current;

    public TuiAppConfiguration(TuiSettingsStore store)
    {
        _store = store;
        _current = store.Load();
    }

    public TuiAppSettings Current => _current;

    public event Action? Changed;

    public void Apply(TuiAppSettings settings)
    {
        _store.Save(settings);
        Reload();
    }

    public void Reload()
    {
        _current = _store.Load();
        Changed?.Invoke();
    }
}
