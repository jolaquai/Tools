using DbdOverlay.Windows;

namespace DbdOverlay;

public class OverlayState(ControlPanel controlPanel, OverlayWindow overlayWindow, Interop.WindowBoundsMonitor windowBoundsMonitor)
{
    public ControlPanel ControlPanel { get; } = controlPanel;
    public OverlayWindow OverlayWindow { get; } = overlayWindow;
    public Interop.WindowBoundsMonitor WindowBoundsMonitor { get; } = windowBoundsMonitor;

    public string[] PerkTitles { get; } = ["", "", "", ""];
    public string[] PerkDescriptions { get; } = ["", "", "", ""];
}
