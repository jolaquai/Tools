namespace FolderDecimator;

internal class Program
{
    static void Main(string[] args)
    {
        foreach (var dir in args)
        {
            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"Directory '{dir}' does not exist.");
                continue;
            }

            Console.WriteLine($"Delete directory '{dir}'? [y/n]");
            Console.Write($">> ");
            if (Console.ReadLine() == "y")
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to delete directory '{dir}': {e.Message}");
                    Console.WriteLine($"Trying again by deleting all files in '{dir}'...");

                    DeleteOneByOne(dir);
                }
            }
        }
    }

    private static void DeleteOneByOne(string dir)
    {
        try
        {
            var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to delete file '{file}': {e.Message}");
                }
            }

            try
            {
                Directory.Delete(dir, true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete directory '{dir}' in one-by-one mode: {e.Message}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to delete directory '{dir}' in one-by-one mode: {e.Message}");
        }
    }
}
