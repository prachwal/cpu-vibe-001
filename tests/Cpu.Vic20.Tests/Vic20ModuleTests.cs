using Cpu.Tui.Configuration;
using Cpu.Tui.Rendering;
using Cpu.Vic20.Modules;
using FluentAssertions;
using Xunit;

namespace Cpu.Vic20.Tests;

public class Vic20ModuleTests
{
    [Fact]
    public void OnKey_PrintableKey_RendersTypedCharacter()
    {
        string settingsPath = Path.Combine(Path.GetTempPath(), $"vic20-tui-{Guid.NewGuid():N}.json");
        var config = new TuiAppConfiguration(new TuiSettingsStore(settingsPath));
        using var module = new ModuleHost(new Vic20Module(config));
        var renderer = new CaptureTerminalRenderer(100, 40);

        module.Instance.OnActivate();
        for (int i = 0; i < 280; i++)
            module.Instance.OnTick();

        module.Instance.OnKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false)).Should().BeTrue();
        for (int i = 0; i < 5; i++)
            module.Instance.OnTick();
        module.Instance.OnRender(renderer, renderer.Width, renderer.Height);

        renderer.Text.Should().Contain("A");
    }

    [Fact]
    public void OnKey_TypesCharacterNotInBootBanner()
    {
        string settingsPath = Path.Combine(Path.GetTempPath(), $"vic20-tui-{Guid.NewGuid():N}.json");
        var config = new TuiAppConfiguration(new TuiSettingsStore(settingsPath));
        using var module = new ModuleHost(new Vic20Module(config));
        var renderer = new CaptureTerminalRenderer(100, 40);

        module.Instance.OnActivate();
        for (int i = 0; i < 280; i++)
            module.Instance.OnTick();

        module.Instance.OnKey(new ConsoleKeyInfo('z', ConsoleKey.Z, false, false, false)).Should().BeTrue();
        for (int i = 0; i < 10; i++)
            module.Instance.OnTick();
        module.Instance.OnRender(renderer, renderer.Width, renderer.Height);

        renderer.Text.Should().Contain("Z");
    }

    private sealed class ModuleHost(Vic20Module instance) : IDisposable
    {
        public Vic20Module Instance { get; } = instance;

        public void Dispose() => Instance.OnDeactivate();
    }

    private sealed class CaptureTerminalRenderer : ITerminalRenderer
    {
        private char[,] _cells;

        public CaptureTerminalRenderer(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new char[height, width];
            Clear(ConsoleColor.Gray, ConsoleColor.Black);
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public string Text
        {
            get
            {
                char[] chars = new char[Width * Height];
                int i = 0;
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        chars[i++] = _cells[y, x];
                return new string(chars);
            }
        }

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new char[height, width];
            Clear(ConsoleColor.Gray, ConsoleColor.Black);
        }

        public void SetCell(int x, int y, char ch, ConsoleColor fg, ConsoleColor bg)
        {
            if ((uint)x < (uint)Width && (uint)y < (uint)Height)
                _cells[y, x] = ch;
        }

        public void SetCell(int x, int y, char ch, TerminalColor fg, TerminalColor bg) =>
            SetCell(x, y, ch, ConsoleColor.Gray, ConsoleColor.Black);

        public void SetText(int x, int y, ReadOnlySpan<char> text, ConsoleColor fg, ConsoleColor bg)
        {
            for (int i = 0; i < text.Length; i++)
                SetCell(x + i, y, text[i], fg, bg);
        }

        public void Clear(ConsoleColor fg, ConsoleColor bg)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _cells[y, x] = ' ';
        }

        public void Flush() { }

        public void Dispose() { }
    }
}
