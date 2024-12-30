using System.Diagnostics;

namespace Monitors.Shared.Models.ExternalRunner;
public class PythonScriptRunner : ExternalRunner
{
    public override bool CanHandle(string path) => Path.GetExtension(path).Equals(".py", StringComparison.OrdinalIgnoreCase);
    public override Task RunAsync(string path, string[] args, CancellationToken stoppingToken)
    {
        var psi = new ProcessStartInfo()
        {
            FileName = "python",
            UseShellExecute = false,
            CreateNoWindow = false
        };
        psi.ArgumentList.Add(path);
        for (var i = 0; i < args.Length; i++)
        {
            psi.ArgumentList.Add(args[i]);
        }
        return RunDefaultAsync(psi, stoppingToken);
    }
}
