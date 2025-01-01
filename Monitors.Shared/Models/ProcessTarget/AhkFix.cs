using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Monitors.Shared.Models.ProcessTarget;
public class AhkFix(ILogger<AhkFix> logger) : IProcessTarget
{
    private readonly ILogger<AhkFix> _logger = logger;

    public Task RunAsync(CancellationToken stoppingToken)
    {
        var needKill = Process.GetProcessesByName("start_protected_game");
        if (needKill.Length == 0)
        {
            return Task.CompletedTask;
        }

        var ahks = Process.GetProcessesByName("AutoHotkey64");
        if (ahks.Length == 0)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Killing AutoHotkeys...");
        for (var i = 0; i < ahks.Length; i++)
        {
            try
            {
                ahks[i].Kill();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"""
                        Error killing AutoHotkey64:
                        {ex.Message}
                        """);
            }
        }

        return Task.CompletedTask;
    }
}
