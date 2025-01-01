using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Monitors.Shared.Models.ProcessTarget;

namespace Monitors.Shared.Services;

public class ProcessesHelper(IEnumerable<IProcessTarget> targets, ILogger<ProcessesHelper> logger) : BackgroundService
{
    private readonly ILogger<ProcessesHelper> _logger = logger;
    private readonly IProcessTarget[] _targets = targets as IProcessTarget[] ?? [.. targets];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Looking for process problems...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(200, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            foreach (var target in _targets)
            {
                await target.RunAsync(stoppingToken);
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}
