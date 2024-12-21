using Microsoft.Extensions.DependencyInjection;

using Monitors.Shared.Models.ProcessTarget;
using Monitors.Shared.Services;
using Monitors.Shared.Util;

namespace AutostartLoop;

public static partial class Autostart
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        IndentCharacter = ' '
    };

    public static async Task Main(string[] args)
    {
        var builder = new HostApplicationBuilder(args);

        builder.Services.AddProgramTimeMonitor();
        builder.Services.AddHostedService<InternetConnectivityMonitor>();
        builder.Services.AddHostedService<DownloadsUnzipper>();
        builder.Services.AddHostedService<IntegratedGraphicsChipProcessor>();

        builder.Services.AddHostedService<ProcessesHelper>();
        builder.Services.AddSingleton<IProcessTarget, AhkFix>();
        builder.Services.AddSingleton<IProcessTarget, SiegeFirefoxFix>();

        builder.Services.AddHostedService<AutostartApp>();

        builder.Services.AddSingleton(_jsonSerializerOptions);

        var host = builder.Build();

        await host.RunAsync();
    }
}
