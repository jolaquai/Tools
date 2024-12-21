using Microsoft.Extensions.DependencyInjection;

namespace Monitors.Shared.Models.ExternalRunner;

public class ExternalRunnerBuilder(IServiceCollection services)
{
    private readonly IServiceCollection _services = services;

    /// <summary>
    /// Registers a new external object to run.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public ExternalRunnerBuilder AddExternal(string path, params string[] args)
    {
        _services.Configure<ExternalRunnerOptions>(o =>
        {
            o.Externals.Add((path, args));
        });
        return this;
    }
}
