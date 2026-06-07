namespace Cpu.Tui.Input;

public sealed class TerminalInputReader
{
    private readonly Stream _stdin;
    private readonly List<byte> _buffer = new(64);
    private readonly Queue<TerminalInput> _pending = new();

    public TerminalInputReader(Stream? stdin = null)
    {
        _stdin = stdin ?? Console.OpenStandardInput();
    }

    public bool TryDequeue(out TerminalInput input)
    {
        if (_pending.Count > 0)
        {
            input = _pending.Dequeue();
            return true;
        }

        input = default;
        return false;
    }

    public void Pump()
    {
        while (Console.KeyAvailable)
        {
            int b = _stdin.ReadByte();
            if (b < 0)
                break;

            _buffer.Add((byte)b);

            while (_buffer.Count > 0)
            {
                if (!AnsiInputParser.TryParse(_buffer.ToArray(), out int consumed, out TerminalInput parsed))
                    break;

                _pending.Enqueue(parsed);
                _buffer.RemoveRange(0, consumed);
            }

            if (_buffer.Count > 64)
                _buffer.Clear();
        }
    }
}
