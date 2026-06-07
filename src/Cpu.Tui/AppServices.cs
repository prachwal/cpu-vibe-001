using System.Reflection;
using Cpu.Module;
using Cpu.Tui.Devices.Pia;
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
        services.AddSingleton<TermViewManager>();

        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var t in asm.GetTypes())
                {
                    if (!t.IsAbstract && !t.IsInterface &&
                        typeof(IAppModule).IsAssignableFrom(t))
                        services.AddTransient(typeof(IAppModule), t);
                }
            }
            catch { }
        }

        services.AddSingleton<ModuleManager>();
        services.AddSingleton<App>();

        _provider = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}
