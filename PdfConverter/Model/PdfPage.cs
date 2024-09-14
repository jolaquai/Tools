using UglyToad.PdfPig.Content;

namespace PdfConverter.Model;

internal record class PdfPage
{
    public required Page Page { get; init; }
    public required int Sort { get; init; }
}
