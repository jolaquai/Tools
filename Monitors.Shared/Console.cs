namespace Monitors.Shared;

public static class Console
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
