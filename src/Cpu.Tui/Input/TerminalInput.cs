using Cpu.Module;

namespace Cpu.Tui.Input;

public enum TerminalInputKind { Key, Mouse, Discard }

public readonly record struct TerminalInput(
    TerminalInputKind Kind,
    ConsoleKeyInfo Key = default,
    MouseEvent Mouse = default);
