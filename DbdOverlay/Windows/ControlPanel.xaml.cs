using System.Windows.Data;

using DbdOverlay.Model;

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

    protected override void OnInitialized(EventArgs e)
    {
        Group_Target_Both.Checked += (_, _) => State.Perks.RaiseCollectionChanged();
        Group_Target_Killer.Checked += (_, _) => State.Perks.RaiseCollectionChanged();
        Group_Target_Survivor.Checked += (_, _) => State.Perks.RaiseCollectionChanged();

        base.OnInitialized(e);
    }

    private void PerkSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        OverlayWindow.UpdateBindings();
    }
}