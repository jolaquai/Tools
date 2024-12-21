using Microsoft.Extensions.DependencyInjection;

using Monitors.Shared.Models.ExternalRunner;
using Monitors.Shared.Services;

namespace Monitors.Shared.Util;

public static class DIExtensions
{
    public static ProgramTimeMonitorBuilder AddProgramTimeMonitor(this IServiceCollection services)
    {
        services.AddHostedService<ProgramTimeMonitor>();
        return new ProgramTimeMonitorBuilder(services);
    }
    public static ExternalRunnerBuilder AddExternalRunners(this IServiceCollection services)
    {
        services.AddHostedService<ExternalRunnerService>();
        services.AddSingleton<IExternalRunner, ExecutableRunner>();
        services.AddSingleton<IExternalRunner, PythonScriptRunner>();
        return new ExternalRunnerBuilder(services);
    }
}
