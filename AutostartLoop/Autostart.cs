using System.Collections.Concurrent;
using System.CommandLine;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AutostartLoop;

public static partial class Autostart
{
    /// <summary>
    /// A format string for <see cref="TimeSpan"/> instances.
    /// </summary>
    private const string Format = @"c";
    private const int TotalWidth = 20;
    private static readonly string AssemblyPath = typeof(Autostart).Assembly.Location;

    private static readonly string eol = Environment.NewLine;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        IndentCharacter = ' '
    };

    public static void Main(string[] args)
    {
        var builder = new HostApplicationBuilder(args);
        builder.Services.AddHostedService<ProgramTimeMonitor>();
        builder.Services.AddHostedService<InternetConnectivityMonitor>();

        builder.Services.AddSingleton(_jsonSerializerOptions);

        var host = builder.Build();
        host.Run();
    }
}

internal partial class AutostartApp(IServiceProvider serviceProvider) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Debugger.IsAttached)
        {
            Console.WriteLine("Skipped Autostart routines (--debug was present)");
            Debugger.Break();

            await Task.Delay(-1);
            return;
        }

        Console.WriteLine("Cleaning projects...");
        CleanProjects();
        Console.WriteLine();

        Console.WriteLine("Moving Siege clips...");
        var moveClipsTask = MoveClipsToMonthlyDirectories();
        Console.WriteLine();

        Console.WriteLine("Deleting empty folders...");
        DeleteFolders();
        Console.WriteLine();

        Console.WriteLine("Emptying Recycle Bin(s)...");
        EmptyRecycleBin();
        Console.WriteLine();

        Console.WriteLine("Clearing temp files...");
        ClearTemps();
        Console.WriteLine();

        Console.WriteLine("Deleting bloat...");
        var deleteBloatTask = Task.Run(() => DeleteBloat());
        Console.WriteLine();

        UninstallIntegratedGraphicsChip();

        await Task.WhenAll(moveClipsTask, deleteBloatTask);
        await BackgroundTasksProc();
        // Console.ReadKey();
    }
    public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

    private static async Task BackgroundTasksProc()
    {
        Console.WriteLines($"""

            {new string('-', Console.BufferWidth)}

            Background operations:
                - {nameof(UninstallIntegratedGraphicsChip)}
                - Downloads ZIP file watcher (unpacks downloaded ZIPs)

            """);

        while (true)
        {
            await Task.WhenAll(
                Task.Run(() => UninstallIntegratedGraphicsChip(true)),
                Task.Run(UnpackDownloads)
            );

            await Task.Delay(1_000);
        }
    }

    private static readonly string[] _bloatPatterns =
    [
        @"C:\Users\user\AppData\Local\Overwolf\CrashDumps",
        @"C:\Users\user\AppData\Local\CrashDumps",
        @"C:\*.dmp"
    ];
    private static readonly EnumerationOptions _enumerationOptions = new EnumerationOptions()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = true,
    };
    private static void DeleteBloat()
    {
        for (var i = 0; i < _bloatPatterns.Length; i++)
        {
            var pattern = _bloatPatterns[i];
            pattern = Environment.ExpandEnvironmentVariables(pattern);

            if (File.Exists(pattern))
            {
                try
                {
                    File.Delete(pattern);
                }
                catch { }
                Console.WriteLine($"Deleted '{pattern}'...");
            }
            else if (Directory.Exists(pattern))
            {
                try
                {
                    Directory.Delete(pattern, true);
                }
                catch { }
                Console.WriteLine($"Deleted '{pattern}'...");
            }
            else if (pattern.Contains('*'))
            {
                try
                {
                    var fses = Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(pattern), Path.GetFileName(pattern), _enumerationOptions);
                    foreach (var file in fses)
                    {
                        if (File.Exists(file))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch { }
                        }
                        else if (Directory.Exists(file))
                        {
                            try
                            {
                                Directory.Delete(file, true);
                            }
                            catch { }
                        }
                        Console.WriteLine($"Deleted '{file}'...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Directory.EnumerateFiles failed with pattern '{pattern}': '{ex.Message}'", ConsoleColor.Red);
                }
            }
        }
    }

    private static readonly string[] _emptyDirectoryPaths =
    [
        @"C:\Users\User\Pictures\Roblox",
        @"C:\Users\User\Documents\ShareX\Screenshots",
    ];
    /// <summary>
    /// Deletes folders at predetermined locations:
    /// <list type="bullet">
    /// <item/>Roblox in-game screenshots
    /// <item/>ShareX screenshots
    /// <item/>Empty directories in <c>E:\YOUTUBE\Captures</c>
    /// </list>
    /// </summary>
    private static void DeleteFolders()
    {
        foreach (var dir in _emptyDirectoryPaths)
        {
            Console.WriteLine($"Deleting directory '{dir}'...");
            try
            {
                Directory.Delete(dir, true);
            }
            catch { }
        }

        foreach (var emptyDir in Directory.EnumerateDirectories(@"E:\YOUTUBE\Captures").Where(path => !Directory.EnumerateFileSystemEntries(path).Any()))
        {
            Console.WriteLine($"Deleting empty directory '{emptyDir}'...");
            Directory.Delete(emptyDir);
        }
    }

    private const string newNvidiaPath = @"C:\Users\user\Videos\NVIDIA";
    private const string topPath = @"E:\YOUTUBE\Captures";
    /// <summary>
    /// Moves <c>.mp4</c> files in <c>E:\YOUTUBE\Captures\*</c> to subdirectories named after the year and month they were last written to.
    /// </summary>
    private static async Task MoveClipsToMonthlyDirectories()
    {
        var parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Debugger.IsAttached ? 1 : Environment.ProcessorCount
        };

        if (Path.Exists(newNvidiaPath))
        {
            // New: NVIDIA App doesn't let you choose where to save clips, so first move them from %USERPROFILE%\Videos\NVIDIA\* to E:\YOUTUBE\Captures
            // Update: It does! I just didn't fckn find the option xd
            // I'll keep this as a backup, but this should never run anymore
            var nvidiaClips = Directory.EnumerateFiles(@"C:\Users\User\Videos\NVIDIA", "*.mp4", SearchOption.AllDirectories);
            Parallel.ForEach(nvidiaClips, parallelOptions, clip =>
            {
                var relativePath = Path.GetRelativePath(newNvidiaPath, clip);
                var newPath = Path.Combine(topPath, relativePath);

                File.Move(clip, newPath, true);
            });
            Console.WriteLine($"Moved {(nvidiaClips.TryGetNonEnumeratedCount(out var count) ? count : nvidiaClips.Count())} clips from NVIDIAs new directory back to Captures.");
        }

        // Original implementation
        var gameDirectories = Directory.GetDirectories(topPath, "*", SearchOption.TopDirectoryOnly);

        Parallel.ForEach(gameDirectories, parallelOptions, gameDir =>
        {
            var files = Directory.EnumerateFiles(gameDir, "*.mp4", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var time = File.GetLastWriteTime(file);
                var newDirName = time.ToString("yyyy-MM");
                var dirName = Path.Combine(gameDir, newDirName);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                var newPath = Path.Combine(dirName, Path.GetFileName(file));

                if (!Path.GetDirectoryName(file).Equals(newDirName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Moved '{file}' to '{newPath}'...");

                    File.Move(file, newPath, true);
                }
            }
        });

        await Task.Delay(1000);
    }

    private static void ForEach<T>(IEnumerable<T> collection, Action<T> action)
    {
        foreach (var item in collection)
        {
            action(item);
        }
    }

    [GeneratedRegex(@"\d{4}-\d{2}", RegexOptions.Compiled)]
    private static partial Regex DirectoryNameRegex();

    /// <summary>
    /// Deletes all <c>bin</c> and <c>obj</c> directories found anywhere in <c>E:\PROGRAMMING\Projects\C#</c>.
    /// </summary>
    private static void CleanProjects()
    {
        const string topPath = @"E:\PROGRAMMING\Projects\C#";
        var enumOpt = new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };
        var clearSize = 0L;
        var paths = Directory.GetDirectories(topPath, "*", enumOpt).Where(dirPath => dirPath.EndsWith("bin", StringComparison.OrdinalIgnoreCase) || dirPath.EndsWith("obj", StringComparison.OrdinalIgnoreCase));

        Parallel.ForEach(paths, () => 0L, (path, _, l) =>
        {
            l += new DirectoryInfo(path).GetFiles("*", enumOpt).AsParallel().Sum(fileInfo => fileInfo.Length);

            Console.WriteLine($"Deleted bin/obj directory '{path.Replace(topPath, ".")}'");
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
            }

            return l;
        }, l => Interlocked.Add(ref clearSize, l));

        Console.WriteLine($" -> Cleared {clearSize / 1024d / 1024:0.00} MB");
    }

    private static readonly string[] _templates =
    [
        Path.GetTempPath(),
        @"C:\Windows\Temp",
        "%TEMP%",
        "%TMP%",
        @"%LOCALAPPDATA%\Temp"
    ];
    private static readonly string[] _paths = _templates
        .Select(Environment.ExpandEnvironmentVariables)
        .DistinctBy(s => s.ToUpperInvariant().Trim())
        .Where(Directory.Exists)
        .ToArray();
    /// <summary>
    /// Clears temp files from a few select locations.
    /// </summary>
    private static void ClearTemps()
    {
        var files = _paths.Select(path => Directory.GetFiles(path, "*", SearchOption.AllDirectories));
        var deletedSum = 0L;
        var remainingSize = 0L;

        Parallel.ForEach(
            files,
            () => new Locals(),
            (files, _, local) =>
            {
                foreach (var file in files)
                {
                    var length = 0L;
                    try
                    {
                        length = new FileInfo(file).Length;
                    }
                    catch
                    {
                    }

                    try
                    {
                        File.Delete(file);
                        local.Sum += length;
                    }
                    catch
                    {
                        local.Remaining += length;
                    }
                }
                return local;
            },
            local =>
            {
                Interlocked.Add(ref deletedSum, local.Sum);
                Interlocked.Add(ref remainingSize, local.Remaining);
            }
        );

        Console.WriteLine($" -> Cleared {deletedSum / 1024d / 1024:0.00} MB");
        if (remainingSize > 0)
        {
            Console.WriteLine($" -> Failed to clear {remainingSize / 1024d / 1024:0.00} MB");
        }
    }
    private struct Locals
    {
        public long Sum
        {
            get; set;
        }
        public long Remaining
        {
            get; set;
        }
    }

    private static volatile bool graphicsChipProcessingRunning;
    /// <summary>
    /// Uninstalls the integrated graphics chip device and drivers.
    /// </summary>
    private static void UninstallIntegratedGraphicsChip(bool callFromBackground = false)
    {
        if (graphicsChipProcessingRunning)
        {
            return;
        }

        graphicsChipProcessingRunning = true;

        try
        {
            if (!callFromBackground)
            {
                Console.WriteLine("Uninstalling integrated graphics chip device and drivers...");
            }

            var p = Process.Start(new ProcessStartInfo()
            {
                FileName = "pnputil.exe",
                Arguments = @"/enum-devices /class Display",
                RedirectStandardOutput = true
            });
            p.WaitForExit();
            var t1 = p.StandardOutput.ReadToEnd().Trim();

            // Parse the command's output into a list of devices
            var split = t1.Split(Environment.NewLine + Environment.NewLine)[1..];
            var devices = split.Select(block =>
            {
                var lines = block.Split(Environment.NewLine);
                var device = new Device();
                foreach (var line in lines)
                {
                    var split = line.Split(": ");
                    var key = split[0].Trim();
                    var value = split[1].Trim();
                    switch (key)
                    {
                        case "Instance ID":
                        {
                            device.InstanceID = value;
                            break;
                        }
                        case "Device Description":
                        {
                            device.DeviceDescription = value;
                            break;
                        }
                        case "Class Name":
                        {
                            device.ClassName = value;
                            break;
                        }
                        case "Class GUID":
                        {
                            device.ClassGUID = value;
                            break;
                        }
                        case "Manufacturer Name":
                        {
                            device.ManufacturerName = value;
                            break;
                        }
                        case "Status":
                        {
                            device.Status = value;
                            break;
                        }
                        case "Driver Name":
                        {
                            device.DriverName = value;
                            break;
                        }
                    }
                }
                return device;
            }).ToArray();

            var device = devices.SingleOrDefault(device => device.DeviceDescription.Equals("AMD Radeon(TM) Graphics", StringComparison.Ordinal));
            if (device is null)
            {
                if (!callFromBackground)
                {
                    Console.WriteLine("    No integrated graphics device found, all good.");
                }

                return;
            }

            // Uninstall the device
            if (!callFromBackground)
            {
                Console.WriteLine($"    Uninstalling device '{device.DeviceDescription}'...");
            }

            var uninstallation = Process.Start(new ProcessStartInfo()
            {
                FileName = "pnputil.exe",
                Arguments = $"/remove-device \"{device.InstanceID}\"",
                RedirectStandardOutput = true
            });
            p.WaitForExit();
            var t2 = p.StandardOutput.ReadToEnd();
            if (p.ExitCode != 0)
            {
                if (!callFromBackground)
                {
                    Console.WriteLines($"""
                    Uninstallation pnputil call exited with code '{p.ExitCode}'.
                    -> '{t2}'
                """, ConsoleColor.Red);
                }
            }
            else
            {
                Console.WriteLine($"    Successfully uninstalled device '{device.DeviceDescription}'.", ConsoleColor.DarkYellow);
            }
        }
        finally
        {
            graphicsChipProcessingRunning = false;
        }
    }
    #region private record class Device
    private record class Device
    {
        public string InstanceID
        {
            get; set;
        }
        public string DeviceDescription
        {
            get; set;
        }
        public string ClassName
        {
            get; set;
        }
        public string ClassGUID
        {
            get; set;
        }
        public string ManufacturerName
        {
            get; set;
        }
        public string Status
        {
            get; set;
        }
        public string DriverName
        {
            get; set;
        }
    }
    #endregion

    [LibraryImport("shell32.dll", EntryPoint = "SHEmptyRecycleBinA", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SHEmptyRecycleBin(nint hwnd, string pszRootPath, uint dwFlags);
    /// <summary>
    /// Empties all Recycle Bins on all drives by invoking <see cref="SHEmptyRecycleBin(nint, string, uint)"/>.
    /// </summary>
    private static void EmptyRecycleBin()
    {
        SHEmptyRecycleBin(nint.Zero, null, 1 | 2 | 4);
    }
    private static void UnpackDownloads()
    {
        foreach (var archivePath in Directory.EnumerateFiles(@"C:\Users\User\Downloads", "*.zip", SearchOption.TopDirectoryOnly))
        {
            var extractPath = Path.Combine(Path.GetDirectoryName(archivePath), Path.GetFileNameWithoutExtension(archivePath));
            if (!Directory.Exists(extractPath))
            {
                ZipFile.ExtractToDirectory(archivePath, extractPath);
            }
            File.Delete(archivePath);
        }
    }
}
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
internal class InternetConnectivityMonitor(JsonSerializerOptions jsonSerializerOptions) : BackgroundService
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

internal static class Console
{
    private static readonly Lock _syncRoot = new Lock();
    private static readonly string eol = Environment.NewLine;
    public static void WriteLine()
    {
        lock (_syncRoot)
        {
            System.Console.WriteLine();
        }
    }
    public static void WriteLine(string message, ConsoleColor color = ConsoleColor.White) => Write(message + eol, color);
    public static void WriteLines(string str, ConsoleColor color = ConsoleColor.White) => WriteLines(str.Split(eol), color);
    public static void WriteLines(IEnumerable<string> lines, ConsoleColor color = ConsoleColor.White) => Array.ForEach(lines as string[] ?? lines.ToArray(), line =>
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            WriteLine();
        }
        else
        {
            WriteLine(line, color);
        }
    });
    public static int BufferWidth => System.Console.BufferWidth - DateTime.Now.ToString("HH':'mm':'ss").Length - 3;
    public static void Write(string message, ConsoleColor color = ConsoleColor.White)
    {
        var now = DateTime.Now.ToString("HH':'mm':'ss");
        lock (_syncRoot)
        {
            System.Console.ForegroundColor = color;
            System.Console.Write($"[{now}] {message}");
            System.Console.ResetColor();
        }
    }
    public static void Clear()
    {
        lock (_syncRoot)
        {
            System.Console.Clear();
        }
    }
}
