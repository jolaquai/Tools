using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Hosting;

namespace AutostartLoop;

internal partial class AutostartApp : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Debugger.IsAttached)
        {
            Console.WriteLine("Skipped Autostart routines (--debug was present)");
            Debugger.Break();

            await Task.Delay(-1, stoppingToken);
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
        var deleteBloatTask = Task.Run(DeleteBloat, stoppingToken);
        Console.WriteLine();

        await Task.WhenAll(moveClipsTask, deleteBloatTask);
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

    [LibraryImport("shell32.dll", EntryPoint = "SHEmptyRecycleBinA", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SHEmptyRecycleBin(nint hwnd, string pszRootPath, uint dwFlags);
    /// <summary>
    /// Empties all Recycle Bins on all drives by invoking <see cref="SHEmptyRecycleBin(nint, string, uint)"/>.
    /// </summary>
    private static void EmptyRecycleBin()
    {
        SHEmptyRecycleBin(nint.Zero, null, 1 | 2 | 4);
    }
}
