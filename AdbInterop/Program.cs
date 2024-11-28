using System.ComponentModel.Design.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;

namespace AdbInterop;

internal class Program
{
    static async Task Main()
    {
        AdbServer.Instance.RestartServer(@"E:\PROGRAMMING\adb\adb.exe");
        var devices = AdbClient.Instance.GetDevices().ToArray();
        if (devices.Length != 1)
        {
            Console.WriteLine("No device connected or more than one device connected.");
            return;
        }
        var device = devices[0];
        // Get the device's serial number
        var serial = device.Serial;
        // Connect
        var syncService = device.CreateSyncService();
        if (!syncService.IsOpen)
        {
            await syncService.OpenAsync();
        }

        while (true)
        {
            Console.WriteLine("[1] Pull newest video");
            Console.WriteLine("[2] Pull newest photo");
            Console.WriteLine("[X] Exit");
            Console.WriteLine("Enter your choice: ");
            var choice = Console.ReadKey(true).Key;
            try
            {
                switch (choice)
                {
                    case ConsoleKey.D1:
                        await PullNewestNVideos(syncService, @"C:\Users\user\Downloads\PornDownload", 1);
                        break;
                    case ConsoleKey.D2:
                        await PullNewestNPhotos(syncService, @"C:\Users\user\Downloads\PornDownload", 1);
                        break;
                    case ConsoleKey.X:
                        return;
                    default:
                        Console.WriteLine("Invalid choice!");
                        break;
                }
            }
            catch { }
        }

        const string sourceDir = "/sdcard/android/data/org.thunderdog.challegram/files/";
        const string destDir = @"C:\Users\user\Downloads\PornDownload";
        Directory.CreateDirectory(destDir);
        await PullNewestNPhotos(syncService, destDir, 3);
    }

    private static Task PullNewestNVideos(SyncService syncService, string destDir, int count)
        => PullNewestNFrom(syncService, "/sdcard/android/data/org.thunderdog.challegram/files/" + "videos", destDir, count);
    private static Task PullNewestNPhotos(SyncService syncService, string destDir, int count)
        => PullNewestNFrom(syncService, "/sdcard/android/data/org.thunderdog.challegram/files/" + "photos", destDir, count);

    private static readonly Regex _fileNameSanityRegex = new Regex($"({string.Join('|', Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Select(k => Regex.Escape(k.ToString())))})", RegexOptions.Compiled);
    private static async Task PullNewestNFrom(SyncService syncService, string sourceDir, string destDir, int count)
    {
        var stats = syncService.GetDirectoryListing(sourceDir).Where(f => f.Path is not ("." or ".." or ".nomedia")).OrderByDescending(f => f.Time);

        var i = 0;
        foreach (var fileStat in stats)
        {
            var ext = Path.GetExtension(fileStat.Path);
            var withoutExt = _fileNameSanityRegex.Replace(Path.GetFileNameWithoutExtension(fileStat.Path), "_");
            await using (var fs = File.Create(Path.Combine(destDir, withoutExt + '_' + fileStat.Time.ToString("yyyy-MM-dd-HH-mm-ss") + ext)))
            {
                await syncService.PullAsync(sourceDir + '/' + fileStat.Path, fs);
            }

            if (++i == count) break;
        }

        if (i < count)
        {
            Console.WriteLine($"Not enough files to pull (only managed to pull {i}/{count})!");
        }
    }
}
