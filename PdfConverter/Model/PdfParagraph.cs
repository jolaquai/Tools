using System.Collections;

namespace PdfConverter.Model;
internal class PdfParagraph : IList<TextChunk>
{
    public TextChunk this[int index] { get => Chunks[index]; set => Chunks[index] = value; }

    public List<TextChunk> Chunks { get; } = [];

    public PdfParagraph(IEnumerable<TextChunk> chunks)
    {
        Chunks.AddRange(chunks);
    }

    public int Count => Chunks.Count;
    public bool IsReadOnly => ((ICollection<TextChunk>)Chunks).IsReadOnly;

    public void Add(TextChunk item) => Chunks.Add(item);
    public void Clear() => Chunks.Clear();
    public bool Contains(TextChunk item) => Chunks.Contains(item);
    public void CopyTo(TextChunk[] array, int arrayIndex) => Chunks.CopyTo(array, arrayIndex);
    public IEnumerator<TextChunk> GetEnumerator() => ((IEnumerable<TextChunk>)Chunks).GetEnumerator();
    public int IndexOf(TextChunk item) => Chunks.IndexOf(item);
    public void Insert(int index, TextChunk item) => Chunks.Insert(index, item);
    public bool Remove(TextChunk item) => Chunks.Remove(item);
    public void RemoveAt(int index) => Chunks.RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Chunks).GetEnumerator();
}
