using System.Diagnostics;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Monitors.Shared.Util;

namespace Monitors.Shared.Models.ExternalRunner;

public abstract class ExternalRunner : IExternalRunner
{
    public abstract bool CanHandle(string path);
    public abstract Task RunAsync(string path, string[] args, CancellationToken stoppingToken);

    /// <summary>
    /// For the specified <see cref="ProcessStartInfo"/> instance, starts a new process, registering default lifetime management.
    /// </summary>
    protected virtual async Task RunDefaultAsync(ProcessStartInfo psi, CancellationToken stoppingToken)
    {
        var process = Process.Start(psi);

        var stopTask = stoppingToken.WhenCancelled();
        var terminateTask = process.WaitForExitAsync();
        if (await Task.WhenAny(stopTask, terminateTask) != terminateTask)
        {
            process.Kill();
            await terminateTask;
        }
    }
}
