namespace PdfConverter.Model;

/// <summary>
/// Represents a rectangular region on a PDF page as measured from the bottom-left corner.
/// </summary>
internal record struct Rectangle
{
    public double Left { get; init; }
    public double Bottom { get; init; }
    public double Right { get; init; }
    public double Top { get; init; }
    public Point BottomLeft => new Point(Left, Bottom);
    public Point TopRight => new Point(Right, Top);
    public Point TopLeft => new Point(Left, Top);
    public Point BottomRight => new Point(Right, Bottom);

    public double Width => Right - Left;
    public double Height => Top - Bottom;

    public Rectangle(double left, double bottom, double right, double top)
    {
        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }
    public Rectangle(Point bottomLeft, Point topRight)
    {
        Left = bottomLeft.X;
        Bottom = bottomLeft.Y;
        Right = topRight.X;
        Top = topRight.Y;
    }
}
