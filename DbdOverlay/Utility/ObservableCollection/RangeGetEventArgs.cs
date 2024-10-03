
namespace DbdOverlay.Utility.ObservableCollection;

/// <summary>
/// Represents the event arguments for the RangeGet event.
/// </summary>
public class RangeGetEventArgs
{
    /// <summary>
    /// Gets the starting index of the range being accessed.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the number of items in the range being accessed.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeGetEventArgs"/> class.
    /// </summary>
    /// <param name="index">The starting index of the range being accessed.</param>
    /// <param name="count">The number of items in the range being accessed.</param>
    public RangeGetEventArgs(int index, int count)
    {
        Index = index;
        Count = count;
    }
}
