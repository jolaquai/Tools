namespace DbdOverlay.Windows;

public partial class ControlPanel : Window
{
    /// <summary>
    /// Exposes the injected <see cref="OverlayState"/> publicly for binding.
    /// Should not be used outside of <see cref="ControlPanel"/> to actually access state. Other dependents should obtain their own reference via the DI container.
    /// It is imperative this be set to non-<see langword="null"/> <b>before</b> calling <see cref="InitializeComponent"/>.
    /// </summary>
    public OverlayState State { get; set; }
    /// <summary>
    /// Exposes the injected <see cref="OverlayWindow"/> publicly for binding.
    /// </summary>
    public OverlayWindow OverlayWindow => State.OverlayWindow;
}