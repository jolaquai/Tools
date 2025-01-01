using Microsoft.Extensions.Logging;

namespace Monitors.Shared.Services;

public class InternetConnectivityMonitor(ILogger<InternetConnectivityMonitor> logger, JsonSerializerOptions jsonSerializerOptions) : BackgroundService
{
    private readonly ILogger<InternetConnectivityMonitor> _logger = logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions;
    // A queue leniency of 1 would mean that only one result of "disconnected" is enough to write that as an event, since the "majority" of 1 is 1
    // A queue leniency of 4 would mean that more than 2 results of "disconnected" are needed to write that as an event, since the majority of 4 is 3
    private const int _queueLeniency = 4;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring and recording internet up-/downtime...");

    restart:
        var dir = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\Reports\Connection");
        var reportDate = DateTime.Today;
        var connection = Path.Combine(dir, $"connection_{reportDate:yyyy-MM-dd}.json");
        List<string> events;
        if (File.Exists(connection))
        {
            events = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(connection))!;
        }
        else
        {
            events = [];
        }

        Directory.CreateDirectory(dir);

        var (lastStatus, reason) = GetConnectionStatus();
        var lastStatuses = new Queue<(bool, string)>(_queueLeniency);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Today != reportDate)
            {
                goto restart;
            }

            var now = DateTime.Now;

            while (lastStatuses.Count >= _queueLeniency - 1)
            {
                lastStatuses.Dequeue();
            }

            (var lastResult, reason) = GetConnectionStatus();
            lastStatuses.Enqueue((lastResult, reason));
            if (lastStatuses.Count < _queueLeniency - 1)
            {
                await Task.Delay(10000, stoppingToken);
                continue;
            }

            var compounded = lastStatuses.Majority(t => t.Item1);
            if (compounded != lastStatus)
            {
                if (compounded)
                {
                    // Freshly connected
                    var status = $"{now:HH':'mm':'ss} - Connected";
                    events.Add(status);
                    _logger.LogInformation(status);
                }
                else
                {
                    // Freshly disconnected
                    var status = $"{now:HH':'mm':'ss} - Disconnected ('{reason}')";
                    events.Add(status);
                    _logger.LogInformation(status);
                }

                lastStatus = compounded;
                File.WriteAllText(connection, JsonSerializer.Serialize(events, _jsonSerializerOptions));
            }

            await Task.Delay(10000, stoppingToken);
        }
    }

    private static readonly string[] _dnsServers =
    [
        "1.1.1.1",
        "8.8.8.8",
        "9.9.9.9",
    ];
    private static (bool, string) GetConnectionStatus()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return (false, "NetworkInterface.GetIsNetworkAvailable() claims there's no network available.");
        }

        for (var i = 0; i < 3; i++)
        {
            foreach (var server in _dnsServers)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        client.Connect(server, 53);
                        return (true, null);
                    }
                }
                catch (SocketException)
                {
                }
            }
        }

        return (false, "Failed to connect to any of the DNS servers after 3 attempts.");
    }
}
