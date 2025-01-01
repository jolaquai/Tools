using System.Diagnostics;

namespace Monitors.Shared.Models.ExternalRunner;
public class ExecutableRunner : ExternalRunner
{
    public override bool CanHandle(string path) => Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase);
    public override Task RunAsync(string path, string[] args, CancellationToken stoppingToken)
    {
        var psi = new ProcessStartInfo()
        {
            FileName = path,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        for (var i = 0; i < args.Length; i++)
        {
            psi.ArgumentList.Add(args[i]);
        }
        return RunDefaultAsync(psi, stoppingToken);
    }
}
