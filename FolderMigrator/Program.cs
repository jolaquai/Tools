namespace FolderMigrator;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Path to the parent directory that should be migrated
        var from = args[0];
        // Path to the directory that should CONTAIN the migrated directory
        var to = args[1];
        var newTopPath = Path.Combine(to, Path.GetFileName(from));

        if (!Directory.Exists(from))
        {
            Console.WriteLine($"Directory {from} does not exist.");
            return;
        }
        if (!Directory.Exists(to))
        {
            Console.WriteLine($"Directory {to} does not exist.");
            return;
        }
        if (Directory.Exists(newTopPath))
        {
            Console.WriteLine($"Directory {newTopPath} already exists.");
            return;
        }
        if (from == to)
        {
            Console.WriteLine("Source and destination are the same.");
            return;
        }
        if (from.Contains(to))
        {
            Console.WriteLine("Destination is a subdirectory of the source.");
            return;
        }
        if (to.Contains(from))
        {
            Console.WriteLine("Source is a subdirectory of the destination.");
            return;
        }

        Directory.CreateDirectory(newTopPath);

        Parallel.ForEach(
            Directory.GetDirectories(from, "*", SearchOption.AllDirectories),
            dirPath =>
            {
                var newDirPath = dirPath.Replace(from, newTopPath);
                Directory.CreateDirectory(newDirPath);
            }
        );

        Parallel.ForEach(
            Directory.GetFiles(from, "*", SearchOption.AllDirectories),
            newPath =>
            {
                var newFilePath = newPath.Replace(from, newTopPath);
                File.Move(newPath, newFilePath);
            }
        );
    }
}
