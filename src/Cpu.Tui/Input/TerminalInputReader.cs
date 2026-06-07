namespace Cpu.Tui.Input;

using System.Runtime.InteropServices;

public sealed class TerminalInputReader
{
    private readonly Stream _stdin;
    private readonly Func<bool> _hasData;
    private readonly Queue<TerminalInput> _pending = new();
    private readonly List<byte> _buffer = new(64);
    private long _lastByteTick;
    private bool _useRawBytes;
    private readonly bool _allowRawMode;

    public TerminalInputReader(Stream? stdin = null, Func<bool>? hasData = null, bool allowRawMode = true)
    {
        _stdin = stdin ?? Console.OpenStandardInput();
        _hasData = hasData ?? (() => Console.KeyAvailable);
        _allowRawMode = allowRawMode;
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

    public void SetMouseCapture(bool enabled)
    {
        if (enabled == _useRawBytes)
            return;

        _useRawBytes = enabled;
        _buffer.Clear();

        if (enabled && _allowRawMode)
            UnixTerminalRawMode.Enter();
        else if (!enabled && _allowRawMode)
            UnixTerminalRawMode.Exit();
    }

    public void Pump()
    {
        if (_useRawBytes)
            PumpBytes();
        else
            PumpReadKey();
    }

    private void PumpReadKey()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            _pending.Enqueue(new TerminalInput(TerminalInputKind.Key, key));
        }
    }

    private void PumpBytes()
    {
        DiscardStaleIncomplete();

        while (_hasData())
        {
            int b = _stdin.ReadByte();
            if (b < 0)
                break;

            _buffer.Add((byte)b);
            _lastByteTick = Environment.TickCount64;
        }

        DrainBuffer();
    }

    private void DrainBuffer()
    {
        while (_buffer.Count > 0)
        {
            ReadOnlySpan<byte> span = CollectionsMarshal.AsSpan(_buffer);
            if (AnsiInputParser.TryParse(span, out int consumed, out TerminalInput parsed))
            {
                if (parsed.Kind != TerminalInputKind.Discard)
                    _pending.Enqueue(parsed);
                _buffer.RemoveRange(0, consumed);
                continue;
            }

            if (AnsiInputParser.IsIncomplete(span))
                break;

            _buffer.RemoveAt(0);
        }
    }

    private void DiscardStaleIncomplete()
    {
        if (_buffer.Count == 0)
            return;

        ReadOnlySpan<byte> span = CollectionsMarshal.AsSpan(_buffer);
        if (!AnsiInputParser.IsIncomplete(span))
            return;

        if (Environment.TickCount64 - _lastByteTick <= 50)
            return;

        if (_buffer[0] == 0x1b && AnsiInputParser.TrySkipLeadingSequence(span, out int consumed))
            _buffer.RemoveRange(0, consumed);
        else
            _buffer.RemoveAt(0);
    }
}
