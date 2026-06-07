namespace Cpu.Tui.Diagnostics;

public sealed class ErrorCollector
{
    private readonly List<string> _errors = [];
    private readonly object _lock = new();

    public int Count { get { lock (_lock) return _errors.Count; } }
    public bool HasErrors => Count > 0;

    public void Add(string message)
    {
        lock (_lock)
        {
            _errors.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_errors.Count > 500) _errors.RemoveRange(0, _errors.Count - 500);
        }
    }

    public void Add(string context, Exception ex)
    {
        Add($"{context}: {ex.GetType().Name}: {ex.Message}");
    }

    public string[] GetErrors()
    {
        lock (_lock) return [.._errors];
    }

    public string GetLastError()
    {
        lock (_lock) return _errors.Count > 0 ? _errors[^1] : "";
    }

    public void Clear()
    {
        lock (_lock) _errors.Clear();
    }
}
