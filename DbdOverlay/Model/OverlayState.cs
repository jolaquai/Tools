using System.Windows.Controls;

using DbdOverlay.Utility.ObservableCollection;
using DbdOverlay.Windows;

namespace DbdOverlay.Model;

public class OverlayState
{
    public ControlPanel ControlPanel { get; }
    public OverlayWindow OverlayWindow { get; }
    public Interop.WindowBoundsMonitor WindowBoundsMonitor { get; }

    public ObservableCollectionFast<Perk> SelectedPerks { get; } = [null, null, null, null];
    public ObservableCollectionFast<Perk> Perks { get; }
    public Dictionary<PerkTag, CheckBox> Categories { get; }

    public OverlayState(ControlPanel controlPanel, OverlayWindow overlayWindow, Interop.WindowBoundsMonitor windowBoundsMonitor, IEnumerable<Perk> perks)
    {
        ControlPanel = controlPanel;
        OverlayWindow = overlayWindow;
        WindowBoundsMonitor = windowBoundsMonitor;

        Perks = new ObservableCollectionFast<Perk>(perks as Perk[] ?? perks)
        {
            Comparer = Comparer<Perk>.Create((x, y) => x.Title.CompareTo(y.Title))
        };
        // We don't need the sort on every further change (because there aren't gonna be any), so remove the comparer
        Perks.Comparer = null;
        Perks.Filter = perk => (
                ControlPanel.Group_Target_Both.IsChecked is true
                || (ControlPanel.Group_Target_Killer.IsChecked is true && perk.For == "Killer")
                || (ControlPanel.Group_Target_Survivor.IsChecked is true && perk.For == "Survivor")
            )
            && Categories?.Any(kv => kv.Value.IsChecked is true && perk.Tags.HasFlag(kv.Key)) is true;

        Categories = Enum.GetValues<PerkTag>().ToDictionary(e => e, e =>
        {
            var cb = new CheckBox()
            {
                Content = e.ToString(),
                IsChecked = true
            };
            cb.Checked += (_, _) => Perks.RaiseCollectionChanged();
            cb.Unchecked += (_, _) => Perks.RaiseCollectionChanged();
            return cb;
        });
    }
}
