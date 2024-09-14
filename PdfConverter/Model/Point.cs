namespace PdfConverter.Model;

/// <summary>
/// Represents a point on a PDF page, measured from the bottom-left corner.
/// </summary>
internal record struct Point
{
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
    public double X { get; init; }
    public double Y { get; init; }
}
