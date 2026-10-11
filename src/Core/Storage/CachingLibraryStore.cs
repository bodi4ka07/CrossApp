using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>
/// Декоратор: той самий контракт ILibraryStore, нова поведінка — запам'ятовує вже
/// знайдені примірники й читачів і не звертається до внутрішнього сховища повторно.
/// </summary>
public sealed class CachingLibraryStore(ILibraryStore inner) : ILibraryStore
{
    private readonly ILibraryStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, BookCopy> _copies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Reader> _readers = new(StringComparer.OrdinalIgnoreCase);

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public IReadOnlyList<BookCopy> ListCopies() => _inner.ListCopies();

    public BookCopy? GetCopy(string id)
    {
        if (_copies.TryGetValue(id, out BookCopy? cached))
        {
            Hits++;
            return cached;
        }

        Misses++;
        BookCopy? copy = _inner.GetCopy(id);
        if (copy is not null) _copies[copy.Id] = copy;
        return copy;
    }

    public void AddCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        _inner.AddCopy(copy);
        _copies[copy.Id] = copy;
    }

    public void UpdateCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        _inner.UpdateCopy(copy);
        _copies[copy.Id] = copy;
    }

    public Reader? GetReader(string id)
    {
        if (_readers.TryGetValue(id, out Reader? cached))
        {
            Hits++;
            return cached;
        }

        Misses++;
        Reader? reader = _inner.GetReader(id);
        if (reader is not null) _readers[reader.Id] = reader;
        return reader;
    }

    public void AddReader(Reader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _inner.AddReader(reader);
        _readers[reader.Id] = reader;
    }

    public void UpdateReader(Reader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _inner.UpdateReader(reader);
        _readers[reader.Id] = reader;
    }
}
