using System.ComponentModel;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using DbdOverlay.Model;

namespace DbdOverlay.Windows;

public partial class OverlayWindow : Window
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
    public ControlPanel ControlPanel => State.ControlPanel;

    public void UpdateBindings()
    {
        void UpdateFor(FrameworkElement element)
        {
            if (this is null) return;

            // Update bindings for the current element
            UpdateElementBindings(element);

            // Recurse through visual children
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (var i = 0; i < childCount; i++)
            {
                if (VisualTreeHelper.GetChild(this, i) is FrameworkElement child)
                {
                    UpdateFor(child);
                }
            }
        }
        UpdateFor(this);
    }

    private static void UpdateElementBindings(FrameworkElement element)
    {
        // Update all bindings on dependency properties
        var elemType = element.GetType();
        foreach (var property in elemType.GetProperties())
        {
            var dependencyProperty = DependencyPropertyDescriptor.FromName(property.Name, elemType, elemType);
            if (dependencyProperty != null)
            {
                var binding = BindingOperations.GetBindingExpression(element, dependencyProperty.DependencyProperty);
                binding?.UpdateTarget();
            }
        }

        // If the element is a ContentControl, update its Content binding
        if (element is ContentControl contentControl)
        {
            var contentBinding = contentControl.GetBindingExpression(ContentControl.ContentProperty);
            contentBinding?.UpdateTarget();
        }
    }
}
