namespace Monitors.Shared.Models.ProcessTarget;

public interface IProcessTarget
{
    public Task RunAsync(CancellationToken stoppingToken);
}
