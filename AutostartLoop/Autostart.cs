using System.Text.Json;

using AutostartLoop.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutostartLoop;

public static partial class Autostart
{
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
        builder.Services.AddHostedService<DownloadsUnzipper>();
        builder.Services.AddHostedService<IntegratedGraphicsChipProcessor>();
        builder.Services.AddHostedService<AutostartApp>();

        builder.Services.AddSingleton(_jsonSerializerOptions);

        var host = builder.Build();

        host.RunAsync();
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
