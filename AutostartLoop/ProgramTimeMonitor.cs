using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Hosting;

namespace AutostartLoop;

internal class ProgramTimeMonitor(JsonSerializerOptions jsonSerializerOptions) : BackgroundService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Monitoring and recording program time...");

    restart:
        var second = TimeSpan.FromSeconds(1);
        var dir = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\Reports");
        var reportDate = DateTime.Today;
        var report = Path.Combine(dir, $"report_{reportDate:yyyy-MM-dd}.json");
        ConcurrentDictionary<string, TimeSpan> timers;

        if (File.Exists(report))
        {
            // Allow picking up after a reboot if the report for today already exists
            timers = JsonSerializer.Deserialize<ConcurrentDictionary<string, TimeSpan>>(File.ReadAllText(report));
        }
        else
        {
            timers = [];
        }

        Directory.CreateDirectory(dir);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Today != reportDate)
            {
                goto restart;
            }

            var now = DateTime.Now;
            var maxEnd = now + second;

            Process.GetProcesses().AsParallel().DistinctBy(p => p.ProcessName).AsParallel().ForAll(process =>
            {
                timers.AddOrUpdate(
                    process.ProcessName,
                    _ => second,
                    (_, time) => time + second
                );
            });

            File.WriteAllText(report, JsonSerializer.Serialize(timers, _jsonSerializerOptions));

            await Task.Delay(maxEnd - DateTime.Now is var val && val >= TimeSpan.Zero ? val : TimeSpan.Zero, stoppingToken);
        }
    }
}
