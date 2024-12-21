
using System.Diagnostics;

namespace Monitors.Shared.Models.ProcessTarget;

public class WarframeMonitorStarter : IProcessTarget
{
    private const string warframe = "Warframe.x64";
    private const string monitorName = "WarframeMarketPriceMonitor";
    private const string monitorPath = $@"C:\01_Korone\Games\Warframe\WarframeMarketPriceMonitor\{monitorName}.exe";

    private bool warframeHasExisted;

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        var warframeExistsLocal = Process.GetProcessesByName(warframe).Length > 0;
        // Leave _warframeHasExisted on false to allow the monitor start logic to run once if Warframe is already running
        if (warframeHasExisted == warframeExistsLocal)
        {
            // We know about this state, so we don't need to do anything
            return;
        }
        warframeHasExisted = warframeExistsLocal;

        if (warframeExistsLocal)
        {
            // Warframe is running, so we need to start the monitor
            if (Process.GetProcessesByName(monitorName).Length > 0)
            {
                // Monitor is already running, so we don't need to do anything
                return;
            }
            Process.Start(monitorPath);
        }
        else
        {
            // Warframe is not running, so get rid of the monitor
            await Task.WhenAll(Process.GetProcessesByName(monitorName).Select(async p =>
            {
                p.Kill();
                await p.WaitForExitAsync();
            }));
        }
    }
}
