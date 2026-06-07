using Cpu.Help.Rendering.Views;
using Cpu.Tui.Rendering;
using FluentAssertions;
using Xunit;

namespace Cpu.Tui.Tests;

public class HelpViewTests
{
    [Fact]
    public void Render_ShowsCurrentKeyMap()
    {
        var view = new HelpView();
        var renderer = new FakeTerminalRenderer(80, 25);
        var area = new TermRect(0, 0, 80, 24);
        view.Render(renderer, area, new PresentationSession(renderer, area));

        string text = CollectText(renderer);
        text.Should().Contain("F2 Setup");
        text.Should().Contain("Setup: theme, frames, panel side");
        text.Should().NotContain("F10 frame UTF");
    }

    private static string CollectText(FakeTerminalRenderer r)
    {
        var chars = new List<char>();
        for (int y = 0; y < r.Height; y++)
            for (int x = 0; x < r.Width; x++)
            {
                char ch = r.GetCell(x, y).Ch;
                if (ch >= ' ')
                    chars.Add(ch);
            }
        return new string(chars.ToArray());
    }
}
