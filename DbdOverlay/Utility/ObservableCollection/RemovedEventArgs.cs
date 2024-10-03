
namespace DbdOverlay.Utility.ObservableCollection;

/// <summary>
/// Represents the event arguments for the Removed event.
/// </summary>
/// <typeparam name="T">The type of the item being removed.</typeparam>
public class RemovedEventArgs<T>
{
    /// <summary>
    /// Gets the item that was removed.
    /// </summary>
    public T Item { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemovedEventArgs{T}"/> class.
    /// </summary>
    /// <param name="item">The item that was removed.</param>
    public RemovedEventArgs(T item)
    {
        Item = item;
    }
}
