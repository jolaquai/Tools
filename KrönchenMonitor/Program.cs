using System.Runtime.InteropServices;

using Monitors.Shared.Models.ExternalRunner;
using Monitors.Shared.Models.ProcessTarget;
using Monitors.Shared.Services;
using Monitors.Shared.Util;

namespace KrönchenMonitor;

public static partial class Autostart
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        IndentCharacter = ' '
    };

    [LibraryImport("kernel32.dll")]
    private static partial nint GetConsoleWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public static async Task Main(string[] args)
    {
        var builder = new HostApplicationBuilder(args);

        var self = GetConsoleWindow();
        ShowWindow(self, SW_HIDE);
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            var exception = (Exception)eventArgs.ExceptionObject;
            var exceptionType = exception.GetType();
            var exceptionMessage = exception.Message;
            var exceptionStackTrace = exception.StackTrace;
            ShowWindow(self, SW_SHOW);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"""
                Unhandled '{exceptionType}': {exceptionMessage}
                {exceptionStackTrace}
                """);
            Console.ResetColor();
            Console.ReadLine();
            Environment.Exit(exception.HResult);
        };

        var ptmBuilder = builder.Services.AddProgramTimeMonitor();
        if (File.Exists("config.json"))
        {
            var config = JsonDocument.Parse(File.ReadAllText("config.json")).RootElement;
            if (config.TryGetProperty("Groups", out var vGroups))
            {
                foreach (var groupConf in vGroups.EnumerateArray())
                {
                    var groupName = groupConf.GetProperty("Name").GetString();
                    if (groupConf.TryGetProperty("ProcessNames", out var vProcs))
                    {
                        var procs = vProcs.EnumerateArray().Select(vP => vP.GetString()).ToArray();
                        ptmBuilder.WithGroup(groupName, procs);
                    }
                }
            }
            if (config.TryGetProperty("Root", out var vRoot))
            {
                ptmBuilder.WithRootDirectory(vRoot.GetString());
            }

            ExternalRunnerBuilder erBuilder = null;
            if (config.TryGetProperty("External", out var vExternals))
            {
                foreach (var array in vExternals.EnumerateArray().Select(p => p.EnumerateArray()))
                {
                    erBuilder ??= builder.Services.AddExternalRunners();

                    var parameters = array.Select(p => p.GetString()).ToArray();
                    var path = parameters[0];
                    var arguments = parameters[1..];
                    erBuilder.AddExternal(path, arguments);
                }
            }
        }

        builder.Services.AddHostedService<ProcessesHelper>();
        builder.Services.AddSingleton<IProcessTarget, WarframeMonitorStarter>();

        builder.Services.AddSingleton(_jsonSerializerOptions);

        var host = builder.Build();

        await host.RunAsync();
    }
}