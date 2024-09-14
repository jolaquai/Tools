using System.Text;

namespace PdfConverter.Model;

/// <summary>
/// Represents the smallest unit of text that can be styled. It is to be built as if using <see cref="StringBuilder"/>.
/// </summary>
internal class TextChunk
{
    private StringBuilder builder = new StringBuilder();
    private string text;
    public string Text => builder?.ToString() ?? text;
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public double FontSize { get; init; }

    public TextChunk Append(string text)
    {
        EnsureUnfrozen();
        builder.Append(text);
        return this;
    }
    public TextChunk Append(string text, string spacing)
    {
        EnsureUnfrozen();
        builder.Append(spacing).Append(text);
        return this;
    }
    /// <summary>
    /// Makes this <see cref="TextChunk"/> immutable and disallows further calls to <see cref="Append(string, string)"/>.
    /// </summary>
    public void Freeze()
    {
        text = builder.ToString();
        builder = null;
    }

    private bool TryGetAtUnfrozen(Index index, out char c)
    {
        c = '\uE000';
        if (index.GetOffset(builder.Length) is var i and > -1
            && i < builder.Length)
        {
            c = builder[index];
        }
        return c != '\uE000';
    }
    private void EnsureUnfrozen()
    {
        if (builder is null)
        {
            throw new InvalidOperationException("This chunk is frozen.");
        }
    }
}
