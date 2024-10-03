using Microsoft.Extensions.DependencyInjection;

namespace DbdOverlay.Windows;
public partial class App : Application
{
    [STAThread]
    public static void Main()
    {
        var app = new App();
        app.Run();
    }

    private const string DbdProcName = "DeadByDaylight-Win64-Shipping";

    protected override void OnStartup(StartupEventArgs e)
    {
        var svcs = new ServiceCollection();

        // Windows
        svcs.AddSingleton<ControlPanel>();
        svcs.AddSingleton<OverlayWindow>();

        // Shared state
        // Deps
        svcs.AddSingleton<Interop.WindowBoundsMonitor>(static _ => new Interop.WindowBoundsMonitor(DbdProcName));
        // Pre-resolving deps like this is disgusting, but it prevents the circular dep from throwing in sp.GetRequiredService<OverlayState> later
        svcs.AddSingleton<OverlayState>(static sp => new OverlayState(
            sp.GetRequiredService<ControlPanel>(),
            sp.GetRequiredService<OverlayWindow>(),
            sp.GetRequiredService<Interop.WindowBoundsMonitor>()
        ));

        var sp = svcs.BuildServiceProvider();

        // In here, we need to make sure everything gets initialized with references to each other
        // To achieve this, the shared state is created with references to the windows
        // Through the SS, they can then obtain references to each other
        var overlayState = sp.GetRequiredService<OverlayState>();
        var controlPanel = overlayState.ControlPanel;
        controlPanel.State = overlayState;
        controlPanel.InitializeComponent();
        var overlayWindow = overlayState.OverlayWindow;
        overlayWindow.State = overlayState;
        overlayWindow.InitializeComponent();

        MainWindow = controlPanel;
        MainWindow.Show();
        MainWindow.Activate();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        controlPanel.Closed += (_, _) => Shutdown();
        overlayWindow.Closed += (_, _) => Shutdown();

        // Hook up the overlay window to the game
        overlayState.WindowBoundsMonitor.BoundsChanged += (_, maybeBounds) =>
        {
            if (maybeBounds is Interop.LPRECT bounds)
            {
                overlayWindow.Dispatcher.Invoke(() =>
                {
                    overlayWindow.BeginInit();
                    {
                        overlayWindow.Left = bounds.Left;
                        overlayWindow.Top = bounds.Top;
                        overlayWindow.Width = bounds.Right - bounds.Left;
                        overlayWindow.Height = bounds.Bottom - bounds.Top;
                    }
                    overlayWindow.EndInit();
                    overlayWindow.Show();
                });
            }
            else
            {
                overlayWindow.Dispatcher.Invoke(() => overlayWindow.Hide());
                controlPanel.Dispatcher.Invoke(() => controlPanel.Hide());

                if (!hasWarned)
                {
                    hasWarned = true;
                    MessageBox.Show("Dead by Daylight not found. Please make sure the game is running. The overlay and the control panel will remain hidden.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        };

        base.OnStartup(e);
    }
    private bool hasWarned;
}
