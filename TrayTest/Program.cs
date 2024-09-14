using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TrayTest;

internal partial class Program
{
    private static NotifyIcon trayIcon;

    private static async Task Main()
    {
        // Hide console window
        //var hWnd = Interop.GetConsoleWindow();
        //Interop.ShowWindow(hWnd, Interop.SW_SHOW);

        trayIcon = new NotifyIcon()
        {
            Text = "Tray Test",
            Icon = new Icon(typeof(Program), "gear.ico"),
            ContextMenuStrip = new ContextMenuStrip()
            {
                Visible = true,
                Enabled = true,
                Items =
                {
                    { "Shit self", null, (sender, e) => MessageBox.Show("Successfully shat self 👍") },
                    { new ToolStripSeparator() },
                    { "Exit", null, (sender, e) => Application.Exit() },
                }
            },
            Visible = true,
        };
        trayIcon.Click += TrayIcon_Click;

        using var pm = new ProcessMonitor();
        pm.ProcessStarted += Pm_ProcessStarted;
        pm.ProcessExited += Pm_ProcessExited;
        pm.Start();

        await Task.Delay(-1);
    }

    private static void TrayIcon_Click(object? sender, EventArgs e)
    {
        trayIcon.ContextMenuStrip.ShowImageMargin = false;
        trayIcon.ContextMenuStrip.Visible = true;
    }

    private static async void Pm_ProcessExited(string processName, Process? process)
    {
        await Console.Out.WriteLineAsync($"'{processName}' exited");
    }
    private static async void Pm_ProcessStarted(string processName, Process? process)
    {
        await Console.Out.WriteLineAsync($"'{processName}' started");
    }

    ~Program()
    {
        trayIcon?.Dispose();
    }

    private static partial class Interop
    {
        internal const int SW_HIDE = 0;
        internal const int SW_SHOW = 5;

        [LibraryImport("kernel32.dll")]
        internal static partial nint GetConsoleWindow();
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ShowWindow(nint hWnd, int nCmdShow);
    }

    private class ProcessMonitor : IDisposable
    {
        private Process[] _processes = Process.GetProcesses();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _workerTask;

        public CancellationToken Token => _cts?.Token ?? new CancellationToken(true);

        internal ProcessMonitor() { }

        private async Task MonitorProcesses()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var allNew = Process.GetProcesses();
                // Only created ones:
                var startedProcesses = allNew.Except(_processes, ProcessComparer.Instance).ToArray();
                // Only exited ones:
                var exitedProcesses = _processes.Except(allNew, ProcessComparer.Instance).ToArray();

                foreach (var p in startedProcesses)
                {
                    ProcessStarted?.Invoke(p.ProcessName, p);
                }
                foreach (var p in exitedProcesses)
                {
                    ProcessExited?.Invoke(p.ProcessName, p);
                }

                _processes = allNew;
            }
        }

        private class ProcessComparer : IEqualityComparer<Process>
        {
            private ProcessComparer() { }
            public static ProcessComparer Instance { get; } = new ProcessComparer();
            public bool Equals(Process? x, Process? y) => x?.Id == y?.Id;
            public int GetHashCode([DisallowNull] Process obj) => obj.Id.GetHashCode();
        }

        public event Action<string, Process?> ProcessStarted;
        public event Action<string, Process?> ProcessExited;

        public void Start()
        {
            _workerTask = Task.Run(MonitorProcesses);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts = null;
            foreach (var p in _processes ?? [])
            {
                p.Dispose();
            }
            _processes = null;
            _workerTask?.Wait();
        }
    }
}
