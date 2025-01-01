using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Monitors.Shared.Services;

public class ProgramTimeMonitor : BackgroundService
{
    private readonly ILogger<ProgramTimeMonitor> _logger;
    private readonly ProgramTimeMonitorOptions _options;

    public ProgramTimeMonitor(IServiceProvider sp, ILogger<ProgramTimeMonitor> logger, IOptions<ProgramTimeMonitorOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        _options.EqualityComparer ??= ProcessComparer.Instance;
        _options.Directory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Reports");
        _options.JsonSerializerOptions ??= sp.GetService<JsonSerializerOptions>() ?? new JsonSerializerOptions(JsonSerializerDefaults.General);

        _options.Validate();
    }

    /// <summary>
    /// Attempts invocation of a <see cref="Func{TResult}"/> and propagates its result or a fallback value if an exception is thrown.
    /// </summary>
    private static bool Try<T>(Func<T> func, T fallback, [NotNullWhen(true)] out T result)
    {
        try
        {
            result = func();
            return true;
        }
        catch
        {
            result = fallback;
            return false;
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring and recording program time...");

    restart:
        var second = TimeSpan.FromSeconds(1);
        var dir = _options.Directory;
        var reportDate = DateTime.Today;
        var report = Path.Combine(dir, $"report_{reportDate:yyyy-MM-dd}.json");

        // Support unconfigured state as before
        if (_options.Groups?.Count is not > 0)
        {
            ConcurrentDictionary<string, TimeSpan> timers = null;

            if (File.Exists(report) && Try(() => JsonSerializer.Deserialize<ConcurrentDictionary<string, TimeSpan>>(File.ReadAllText(report)), null, out var result))
            {
                // Allow picking up after a reboot if the report for today already exists
                timers = result;
            }

            timers ??= [];

            Directory.CreateDirectory(dir);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.Today != reportDate)
                {
                    goto restart;
                }

                var now = DateTime.Now;
                var maxEnd = now + second;

                Process.GetProcesses().AsParallel().Distinct((IEqualityComparer<Process>)_options.EqualityComparer).ForAll(process =>
                {
                    timers.AddOrUpdate(
                        process.ProcessName,
                        _ => second,
                        (_, time) => time + second
                    );
                });

                File.WriteAllText(report, JsonSerializer.Serialize(timers, _options.JsonSerializerOptions));

                await Task.Delay(maxEnd - DateTime.Now is var val && val >= TimeSpan.Zero ? val : TimeSpan.Zero, stoppingToken);
            }
        }
        else
        {
            ConcurrentDictionary<string, ConcurrentDictionary<string, TimeSpan>> timers = null;

            if (File.Exists(report) && Try(() => JsonSerializer.Deserialize<ConcurrentDictionary<string, ConcurrentDictionary<string, TimeSpan>>>(File.ReadAllText(report), _options.JsonSerializerOptions), null, out var result))
            {
                // Allow picking up after a reboot if the report for today already exists
                timers = result;
            }

            if (timers is null)
            {
                timers = [];
                for (var i = 0; i < _options.Groups.Count; i++)
                {
                    var (groupName, procNames) = _options.Groups[i];
                    ConcurrentDictionary<string, TimeSpan> group = [];
                    foreach (var proc in procNames)
                    {
                        group[proc] = TimeSpan.Zero;
                    }
                }
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
                Process.GetProcesses().AsParallel().Distinct((IEqualityComparer<Process>)_options.EqualityComparer).ForAll(process =>
                {
                    var added = false;
                    for (var i = 0; i < _options.Groups.Count; i++)
                    {
                        var (groupName, procNames) = _options.Groups[i];

                        if (procNames.Contains(process.ProcessName))
                        {
                            timers.AddOrUpdate(
                                $"{i + 1} {groupName}",
                                _ => new ConcurrentDictionary<string, TimeSpan> { [process.ProcessName] = second },
                                (_, group) =>
                                {
                                    group.AddOrUpdate(
                                        process.ProcessName,
                                        _ => second,
                                        (_, time) => time + second
                                    );
                                    added = true;
                                    return group;
                                }
                            );
                        }
                    }
                    if (!added)
                    {
                        timers.AddOrUpdate(
                            $"{_options.Groups.Count + 1} Uncategorized",
                            _ => new ConcurrentDictionary<string, TimeSpan> { [process.ProcessName] = second },
                            (_, group) =>
                            {
                                group.AddOrUpdate(
                                    process.ProcessName,
                                    _ => second,
                                    (_, time) => time + second
                                );
                                return group;
                            }
                        );
                    }
                });

                File.WriteAllText(report, JsonSerializer.Serialize(timers, _options.JsonSerializerOptions));
                await Task.Delay(maxEnd - DateTime.Now is var val && val >= TimeSpan.Zero ? val : TimeSpan.Zero, stoppingToken);
            }
        }
    }

    private struct ProcessComparer : IEqualityComparer<Process>, IEqualityComparer<string>
    {
        // Auto-property doesn't work here
#pragma warning disable IDE0032 // Use auto property
        private static ProcessComparer _instance = new ProcessComparer();
#pragma warning restore IDE0032 // Use auto property
        public static ref ProcessComparer Instance => ref _instance;
        public readonly bool Equals(Process x, Process y) => Equals(x?.ProcessName, y?.ProcessName);
        public readonly int GetHashCode([DisallowNull] Process obj) => GetHashCode(obj?.ProcessName);

        public readonly bool Equals(string x, string y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);
        public readonly int GetHashCode([DisallowNull] string obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
    }
}

public class ProgramTimeMonitorBuilder(IServiceCollection services)
{
    private readonly IServiceCollection _services = services;

    /// <summary>
    /// Adds a group of processes to be monitored together.
    /// Processes in the same group will be grouped together in the output file, before any processes that do not belong to any group.
    /// </summary>
    /// <param name="processNames">The names of the processes to add as a group.</param>
    /// <returns>The current instance of the <see cref="ProgramTimeMonitorBuilder"/>.</returns>
    public ProgramTimeMonitorBuilder WithGroup(string groupName, params string[] processes)
    {
        _services.Configure<ProgramTimeMonitorOptions>(options => options.Groups.Add((groupName, processes)));
        return this;
    }
    /// <summary>
    /// Overrides the default equality comparer to be used for comparing processes.
    /// </summary>
    /// <param name="equalityComparer">The equality comparer to use.</param>
    /// <returns>The current instance of the <see cref="ProgramTimeMonitorBuilder"/>.</returns>
    public ProgramTimeMonitorBuilder WithEqualityComparer(IEqualityComparer<Process> equalityComparer)
    {
        _services.Configure<ProgramTimeMonitorOptions>(options => options.EqualityComparer = (IEqualityComparer<string>)equalityComparer);
        return this;
    }
    /// <summary>
    /// Sets the root directory where the reports will be stored.
    /// </summary>
    /// <param name="root">The root directory to use.</param>
    /// <returns>The current instance of the <see cref="ProgramTimeMonitorBuilder"/>.</returns>
    public ProgramTimeMonitorBuilder WithRootDirectory(string root)
    {
        _services.Configure<ProgramTimeMonitorOptions>(options => options.Directory = root);
        return this;
    }
    /// <summary>
    /// Sets the <see cref="JsonSerializerOptions"/> to use for serializing the reports.
    /// </summary>
    /// <param name="jso">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns>The current instance of the <see cref="ProgramTimeMonitorBuilder"/>.</returns>
    public ProgramTimeMonitorBuilder WithJsonSerializerOptions(JsonSerializerOptions jso)
    {
        _services.Configure<ProgramTimeMonitorOptions>(options => options.JsonSerializerOptions = jso);
        return this;
    }
}

public class ProgramTimeMonitorOptions
{
    public List<(string, string[])> Groups { get; } = [];
    public IEqualityComparer<string> EqualityComparer { get; set; }
    public string Directory { get; set; }
    public JsonSerializerOptions JsonSerializerOptions { get; set; }

    /// <summary>
    /// Validates the state of this instance.
    /// </summary>
    public void Validate()
    {
        var allProcs = Groups.SelectMany(t => t.Item2).ToArray();
        if (allProcs.Length != allProcs.ToHashSet().Count)
        {
            throw new ArgumentException("Process names must be unique and one process can only belong to one group.");
        }
    }
}
