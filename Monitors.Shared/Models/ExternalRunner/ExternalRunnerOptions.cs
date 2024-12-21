namespace Monitors.Shared.Models.ExternalRunner;

public class ExternalRunnerOptions
{
    public HashSet<(string, string[])> Externals { get; } = [];
}