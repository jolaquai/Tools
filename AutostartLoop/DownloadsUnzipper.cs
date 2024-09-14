using System.IO.Compression;

using Microsoft.Extensions.Hosting;

namespace AutostartLoop;

internal class DownloadsUnzipper : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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

            await Task.Delay(10_000, stoppingToken);
        }
    }
}
