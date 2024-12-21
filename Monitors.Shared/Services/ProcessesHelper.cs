using System.Diagnostics;

using Monitors.Shared.Models.ProcessTarget;

namespace Monitors.Shared.Services;

public class ProcessesHelper(IEnumerable<IProcessTarget> targets) : BackgroundService
{
    private readonly IProcessTarget[] _targets = targets as IProcessTarget[] ?? [.. targets];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Looking for process problems...");

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
