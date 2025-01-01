using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Monitors.Shared.Models.ProcessTarget;
public class SiegeFirefoxFix(ILogger<SiegeFirefoxFix> logger) : IProcessTarget
{
    private readonly ILogger<SiegeFirefoxFix> _logger = logger;

    public Task RunAsync(CancellationToken stoppingToken)
    {
        var siege = Process.GetProcessesByName("RainbowSix");
        if (siege.Length == 0)
        {
            return Task.CompletedTask;
        }

        var pluginContainer = Process.GetProcessesByName("plugin-container");
        if (pluginContainer.Length == 0)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Killing plugin-container...");
        foreach (var process in pluginContainer)
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"""
                            Error killing plugin-container:
                            {ex.Message}
                            """);
            }
        }

        return Task.CompletedTask;
    }
}
