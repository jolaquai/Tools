using System.Diagnostics;
using System.IO.Compression;

namespace FolderMigrator;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var paths = args.Select(Path.GetFullPath).Where(Directory.Exists).ToArray();
        if (paths.Length != 2)
        {
            WriteError("Please provide exactly two valid directories.");
            return;
        }
        var left = paths[0];
        var right = paths[1];
        if (!Directory.Exists(left) || !Directory.Exists(right))
        {
            WriteError("One or conflicting of the provided directories do not exist.");
            return;
        }
        if (left == right)
        {
            WriteError("The provided directories are the same.");
            return;
        }
        if (Path.GetFileName(left) != Path.GetFileName(right))
        {
            WriteError("The provided directories do not have the same name.");
            return;
        }

        // Logic is as follows:
        // 1. If a file only exists left, include it in the return list.
        // 2. If a file only exists right, include it in the return list.
        // 3. If a file exists in both directories, compare the last modification time. Take the newer file.
        // Directories are not included in the return list and should be created implicitly on copy.
        // Only compare relative paths, otherwise all files will always match as 'unique'.

        var leftFiles = Directory.GetFiles(left, "*", SearchOption.AllDirectories);
        var rightFiles = Directory.GetFiles(right, "*", SearchOption.AllDirectories);
        var leftRelative = leftFiles.Select(f => Path.GetRelativePath(left, f)).ToArray();
        var rightRelative = rightFiles.Select(f => Path.GetRelativePath(right, f)).ToArray();
        var conflicting = leftRelative.Intersect(rightRelative).Order().ToArray();
        var leftOnly = leftRelative.Except(rightRelative).ToArray();
        var rightOnly = rightRelative.Except(leftRelative).ToArray();
        var newerFullPaths = conflicting.Select(f =>
        {
            var leftTime = File.GetLastWriteTime(Path.Combine(left, f));
            var rightTime = File.GetLastWriteTime(Path.Combine(right, f));
            return leftTime > rightTime ? Path.Combine(left, f) : Path.Combine(right, f);
        }).ToArray();
        var result = leftOnly.Select(f => Path.Combine(left, f)).Concat(rightOnly.Select(f => Path.Combine(right, f))).Concat(newerFullPaths).ToArray();

        await using (var fs = File.Create("result.txt"))
        using (var sw = new StreamWriter(fs))
        {
            foreach (var file in result)
            {
                sw.WriteLine(file);
            }
        }
        if (Array.Exists(args, a => a.Equals("--copy", StringComparison.OrdinalIgnoreCase)))
        {
            if (Array.Exists(args, a => a.Equals("--zip", StringComparison.OrdinalIgnoreCase)))
            {
                // User wants the files in a zip file.
                await using (var ms = new MemoryStream())
                {
                    using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
                    {
                        foreach (var file in result)
                        {
                            var path = file
                                .Replace(left, "")
                                .Replace(right, "")
                                .TrimStart(Path.DirectorySeparatorChar);

                            var entry = zip.CreateEntry(path, CompressionLevel.NoCompression);
                            await using (var entryStream = entry.Open())
                            await using (var fileStream = File.OpenRead(file))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                    ms.Seek(0, SeekOrigin.Begin);
                    await using (var fs = File.Create($"{Path.GetFileName(left)}.zip"))
                    {
                        await ms.CopyToAsync(fs);
                    }
                }
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    ArgumentList =
                    {
                        "/select,",
                        Path.GetFullPath($"{Path.GetFileName(left)}.zip")
                    },
                    UseShellExecute = true
                });
            }
            else
            {
                // User just wants a new directory with the files.
                var newDir = Path.Combine($"{Path.GetFileName(left)}_merged");
                if (Directory.Exists(newDir))
                {
                    WriteError($"Directory already exists: {newDir}");
                    return;
                }
                Directory.CreateDirectory(newDir);
                foreach (var file in result)
                {
                    var path = file
                        .Replace(left, "")
                        .Replace(right, "")
                        .TrimStart(Path.DirectorySeparatorChar);
                    var newPath = Path.Combine(newDir, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    if (File.Exists(newPath))
                    {
                        WriteError($"File already exists: {newPath}");
                        return;
                    }
                    File.Copy(file, newPath, false);
                }
            }
        }
        else
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetFullPath("result.txt"),
                UseShellExecute = true
            });
        }
        while (true)
        {
            Console.WriteLine("Press Ctrl+C to exit.");
            Console.ReadLine();
        }
    }

    private static void WriteError(params string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Array.ForEach(args, Console.WriteLine);
        Console.ResetColor();
    }
}
