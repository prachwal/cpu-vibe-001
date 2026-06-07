using System.Reflection;
using Cpu.Module;
using Cpu.Tui.Configuration;
using Cpu.Tui.Diagnostics;
using Cpu.Tui.Devices.Pia;
using Cpu.Tui.Modules;
using Cpu.Tui.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Cpu.Tui;

public static class AppServices
{
    private static ServiceProvider? _provider;

    private static readonly Dictionary<string, int> ModuleOrderByType = new(StringComparer.Ordinal)
    {
        ["SetupModule"] = 0,
        ["ScreenModule"] = 1,
        ["HelpModule"] = 2,
        ["DemoMenuModule"] = 3,
        ["ImageModule"] = 4,
        ["Apple1Module"] = 5,
        ["CanvasModule"] = 6,
    };

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
        services.AddSingleton<TuiSettingsStore>();
        services.AddSingleton<TuiAppConfiguration>();
        services.AddSingleton<ITuiAppConfiguration>(sp => sp.GetRequiredService<TuiAppConfiguration>());

        var moduleTypes = DiscoverModuleTypes();
        foreach (var t in moduleTypes)
            services.AddTransient(t);

        services.AddSingleton<MainMenuModule>(sp =>
        {
            var modules = new List<IAppModule>(moduleTypes.Count);
            foreach (var t in moduleTypes)
                modules.Add((IAppModule)sp.GetRequiredService(t));
            return new MainMenuModule(modules, sp.GetRequiredService<ErrorCollector>());
        });

        services.AddSingleton<ModuleManager>();
        services.AddSingleton<App>();

        _provider = services.BuildServiceProvider();
    }

    private static List<Type> DiscoverModuleTypes()
    {
        var found = new List<Type>();

        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var t in asm.GetTypes())
                {
                    if (t.IsAbstract || t.IsInterface || !typeof(IAppModule).IsAssignableFrom(t))
                        continue;
                    if (t == typeof(MainMenuModule) || t.Name.Contains("DemoPlayer"))
                        continue;
                    if (!found.Contains(t))
                        found.Add(t);
                }
            }
            catch
            {
            }
        }

        found.Sort((a, b) =>
        {
            int ao = ModuleOrderByType.GetValueOrDefault(a.Name, int.MaxValue);
            int bo = ModuleOrderByType.GetValueOrDefault(b.Name, int.MaxValue);
            int cmp = ao.CompareTo(bo);
            return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        return found;
    }

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}
