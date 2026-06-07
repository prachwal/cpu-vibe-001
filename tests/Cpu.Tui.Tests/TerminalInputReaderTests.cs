using Cpu.Tui.Input;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Cpu.Tui.Tests;

public class TerminalInputReaderTests
{
    [Fact]
    public void MouseSequenceThenLetter_ProducesMouseAndKey()
    {
        byte[] data = Encoding.ASCII.GetBytes("\x1b[<35;10;5Ma");
        var stream = new MemoryStream(data);
        var reader = new TerminalInputReader(stream, () => stream.Position < stream.Length, allowRawMode: false);

        reader.SetMouseCapture(true);
        reader.Pump();

        reader.TryDequeue(out TerminalInput mouse).Should().BeTrue();
        mouse.Kind.Should().Be(TerminalInputKind.Mouse);
        mouse.Mouse.IsMotion.Should().BeTrue();

        reader.Pump();
        reader.TryDequeue(out TerminalInput key).Should().BeTrue();
        key.Kind.Should().Be(TerminalInputKind.Key);
        key.Key.KeyChar.Should().Be('a');
    }

    [Fact]
    public void OrphanMouseFragment_IsDiscardedNotKey()
    {
        byte[] data = [(byte)'[', (byte)'<', (byte)'3'];
        var stream = new MemoryStream(data);
        var reader = new TerminalInputReader(stream, () => stream.Position < stream.Length, allowRawMode: false);

        reader.SetMouseCapture(true);
        reader.Pump();

        while (reader.TryDequeue(out TerminalInput item))
            item.Kind.Should().NotBe(TerminalInputKind.Mouse);

        stream.Position.Should().Be(data.Length);
    }

    [Fact]
    public void ArrowBytes_ParseAsKey()
    {
        byte[] data = Encoding.ASCII.GetBytes("\x1b[A");
        var stream = new MemoryStream(data);
        var reader = new TerminalInputReader(stream, () => stream.Position < stream.Length, allowRawMode: false);

        reader.SetMouseCapture(true);
        reader.Pump();

        reader.TryDequeue(out TerminalInput input).Should().BeTrue();
        input.Kind.Should().Be(TerminalInputKind.Key);
        input.Key.Key.Should().Be(ConsoleKey.UpArrow);
    }
}
