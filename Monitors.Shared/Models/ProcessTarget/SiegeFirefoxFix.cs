using System.Diagnostics;

namespace Monitors.Shared.Models.ProcessTarget;
public class SiegeFirefoxFix : IProcessTarget
{
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

        Console.WriteLine("Killing plugin-container...");
        foreach (var process in pluginContainer)
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"""
                            Error killing plugin-container:
                            {ex.Message}
                            """);
            }
        }

        return Task.CompletedTask;
    }
}
