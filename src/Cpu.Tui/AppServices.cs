using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Cpu.Tui;

public static class AppServices
{
    private static ServiceProvider? _provider;

    public static ServiceProvider Provider
    {
        get
        {
            if (_provider == null)
                Configure();
            return _provider!;
        }
    }

    public static void Configure()
    {
        var services = new ServiceCollection();

        // Stream — stdout
        var stdout = Console.OpenStandardOutput();
        services.AddSingleton(stdout);

        // Renderer
        services.AddSingleton<ITerminalRenderer>(sp =>
            new AnsiTerminalRenderer(sp.GetRequiredService<Stream>()));

        // Screen buffer
        services.AddSingleton<ScreenBuffer>();

        // Echo
        services.AddSingleton<EchoTerminal>();

        // PIA
        services.AddSingleton(new PiaDevice(0x8800));
        services.AddSingleton<PiaTerminalAdapter>();

        // App
        services.AddSingleton<App>();

        _provider = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}
