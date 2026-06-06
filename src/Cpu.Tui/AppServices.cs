using Cpu.Module;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Graphics;
using Cpu.Tui.Modules;
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
            if (_provider == null) Configure();
            return _provider!;
        }
    }

    public static void Configure()
    {
        var services = new ServiceCollection();

        var stdout = Console.OpenStandardOutput();
        services.AddSingleton(stdout);
        services.AddSingleton<ITerminalRenderer>(sp =>
            new AnsiTerminalRenderer(sp.GetRequiredService<Stream>()));

        services.AddSingleton<ScreenBuffer>();
        services.AddSingleton<EchoTerminal>();
        services.AddSingleton(new PiaDevice(0x8800));
        services.AddSingleton<PiaTerminalAdapter>();
        services.AddSingleton<TermViewManager>();

        var config = AppConfig.Load();

        foreach (var mc in config.Modules)
        {
            var type = Type.GetType(mc.TypeName);
            if (type == null)
            {
                Console.Error.WriteLine($"Module type not found: {mc.TypeName}");
                continue;
            }
            services.AddTransient(typeof(IAppModule), type);
        }

        services.AddSingleton<ModuleManager>();
        services.AddSingleton<App>();

        _provider = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}
