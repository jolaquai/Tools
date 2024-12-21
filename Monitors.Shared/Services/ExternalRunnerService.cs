using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Monitors.Shared.Models.ExternalRunner;

namespace Monitors.Shared.Services;

/// <summary>
/// Services <see cref="IExternalRunner"/> instances, running registered external objects and managing their lifetimes.
/// </summary>
public class ExternalRunnerService(IServiceProvider services, IOptions<ExternalRunnerOptions> options) : BackgroundService
{
    private readonly ExternalRunnerOptions _options = options.Value;
    private readonly IServiceScope _scope = services.CreateScope();
    private IServiceProvider _services;

    private Task[] tasks;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _services = _scope.ServiceProvider;
        var runners = _services.GetServices<IExternalRunner>().ToArray();

        EnsureAllExternalsHandled(runners);
        EnsureExternalsExist();

        tasks = new Task[_options.Externals.Count];
        foreach (var (i, (path, args)) in _options.Externals.Index())
        {
            if (Array.Find(runners, r => r.CanHandle(path)) is IExternalRunner runner)
            {
                tasks[i] = runner.RunAsync(path, args, stoppingToken);
            }
        }
        return Task.WhenAll(tasks);
    }
    private void EnsureAllExternalsHandled(IExternalRunner[] runners)
    {
        var unhandled = _options.Externals
            .Select(t => t.Item1)
            .Where(path => !runners.Any(runner => runner.CanHandle(path)));
        if (unhandled.Any())
        {
            throw new InvalidOperationException($"No runner found for the following external objects:{Environment.NewLine}{string.Join(Environment.NewLine, unhandled)}");
        }
    }
    private void EnsureExternalsExist()
    {
        var missing = _options.Externals
            .Select(t => t.Item1)
            .Where(path => !File.Exists(path));
        if (missing.Any())
        {
            throw new FileNotFoundException($"The following external objects do not exist:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
        }
    }
}
