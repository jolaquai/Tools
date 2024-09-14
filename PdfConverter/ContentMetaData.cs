using PdfConverter.Model;

namespace PdfConverter;

/// <summary>
/// Measures the space in which content is expected to be found on a page.
/// </summary>
internal record class ContentMetaData
{
    /// <summary>
    /// The region in which actual content, not including KTs, author names or page numbers, is expected to be found, on a left (odd-numbered) page.
    /// </summary>
    public required Rectangle LeftPage { get; init; }
    /// <summary>
    /// The region in which actual content, not including KTs, author names or page numbers, is expected to be found, on a right (even-numbered) page.
    /// </summary>
    public required Rectangle RightPage { get; init; }
    /// <summary>
    /// The maximum distance two line base lines may be from each other to be considered part of the same paragraph.
    /// </summary>
    public required double MaxParagraphLineHeight { get; init; }
    /// <summary>
    /// The default x-coordinate of the start of a line on a left (odd-numbered) page.
    /// </summary>
    public required double LeftDefaultLineStart { get; init; }
    /// <summary>
    /// The default x-coordinate of the start of a line on a right (even-numbered) page.
    /// </summary>
    public required double RightDefaultLineStart { get; init; }
    /// <summary>
    /// The most offset a line's start may be from the default start to be considered normal text.
    /// </summary>
    public required double MaxLineStartDifference { get; init; }
}
