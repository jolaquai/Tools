namespace Monitors.Shared.Services;

public class InternetConnectivityMonitor(JsonSerializerOptions jsonSerializerOptions) : BackgroundService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Monitoring and recording internet up-/downtime...");

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
        var lastStatuses = new Queue<(bool, string)>(6);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Today != reportDate)
            {
                goto restart;
            }

            var now = DateTime.Now;

            while (lastStatuses.Count >= 5)
            {
                lastStatuses.Dequeue();
            }

            (var lastResult, reason) = GetConnectionStatus();
            lastStatuses.Enqueue((lastResult, reason));
            if (lastStatuses.Count < 5)
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
                    events.Add($"{now:HH':'mm':'ss} - Connected");
                }
                else
                {
                    // Freshly disconnected
                    events.Add($"{now:HH':'mm':'ss} - Disconnected ('{reason}')");
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
