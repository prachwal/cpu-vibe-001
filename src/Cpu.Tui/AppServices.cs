using System.Reflection;
using Cpu.Module;
using Cpu.Tui.Diagnostics;
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
        services.AddSingleton<ErrorCollector>();

        var moduleTypes = new List<Type>();
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var t in asm.GetTypes())
                {
                    if (!t.IsAbstract && !t.IsInterface &&
                        typeof(IAppModule).IsAssignableFrom(t) &&
                        t != typeof(MainMenuModule) &&
                        !t.Name.Contains("DemoPlayer"))
                        moduleTypes.Add(t);
                }
            }
            catch { }
        }

        // Rejestrujemy moduły jako Transient (każdy dostaje swoją instancję)
        foreach (var t in moduleTypes)
            services.AddTransient(typeof(IAppModule), t);

        // MainMenuModule przyjmuje wszystkie IAppModule w konstruktorze
        services.AddSingleton<MainMenuModule>(sp =>
        {
            var modules = sp.GetServices<IAppModule>();
            return new MainMenuModule(modules);
        });

        services.AddSingleton<ModuleManager>();
        services.AddSingleton<App>();

        _provider = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}
