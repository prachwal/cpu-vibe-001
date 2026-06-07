namespace Cpu.Tui.Input;

public sealed class TerminalInputReader
{
    private readonly Queue<TerminalInput> _pending = new();
    private readonly List<byte> _escapeBuffer = new(32);

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
            ReadOne();
    }

    private void ReadOne()
    {
        ConsoleKeyInfo key = Console.ReadKey(true);
        if (key.Key != ConsoleKey.Escape)
        {
            _pending.Enqueue(new TerminalInput(TerminalInputKind.Key, key));
            return;
        }

        _escapeBuffer.Clear();
        _escapeBuffer.Add(0x1b);
        long deadline = Environment.TickCount64 + 30;

        while (Environment.TickCount64 < deadline)
        {
            if (!Console.KeyAvailable)
            {
                Thread.Sleep(1);
                continue;
            }

            ConsoleKeyInfo next = Console.ReadKey(true);
            AppendKeyChar(next);

            if (TryFinishEscapeSequence())
                return;
        }

        if (_escapeBuffer.Count > 1 && TryFinishEscapeSequence())
            return;

        _pending.Enqueue(new TerminalInput(TerminalInputKind.Key,
            new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false)));
    }

    private void AppendKeyChar(ConsoleKeyInfo key)
    {
        if (key.KeyChar != '\0')
            _escapeBuffer.Add((byte)key.KeyChar);
        else if (key.Key == ConsoleKey.Escape)
            _escapeBuffer.Add(0x1b);
    }

    private bool TryFinishEscapeSequence()
    {
        byte[] raw = _escapeBuffer.ToArray();

        while (raw.Length > 0)
        {
            if (!AnsiInputParser.TryParse(raw, out int consumed, out TerminalInput parsed))
                break;

            if (parsed.Kind != TerminalInputKind.Discard)
                _pending.Enqueue(parsed);

            if (consumed >= raw.Length)
                return true;

            raw = raw[consumed..];
        }

        return false;
    }
}
