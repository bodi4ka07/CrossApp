using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>Сховище в пам'яті: після виходу з програми дані зникають.</summary>
public sealed class InMemoryLibraryStore(
    IEnumerable<BookCopy>? copies = null,
    IEnumerable<Reader>? readers = null) : ILibraryStore
{
    private readonly Dictionary<string, BookCopy> _copies =
        (copies ?? []).ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Reader> _readers =
        (readers ?? []).ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BookCopy> ListCopies() => _copies.Values.ToList();

    public BookCopy? GetCopy(string id) => _copies.GetValueOrDefault(id);

    public void AddCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        if (_copies.ContainsKey(copy.Id))
            throw new InvalidOperationException($"Примірник з id={copy.Id} уже існує.");
        _copies.Add(copy.Id, copy);
    }

    public void UpdateCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        if (!_copies.ContainsKey(copy.Id))
            throw new InvalidOperationException($"Немає примірника з id={copy.Id}.");
        _copies[copy.Id] = copy;
    }

    public Reader? GetReader(string id) => _readers.GetValueOrDefault(id);

    public void AddReader(Reader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (_readers.ContainsKey(reader.Id))
            throw new InvalidOperationException($"Читач з id={reader.Id} уже існує.");
        _readers.Add(reader.Id, reader);
    }

    public void UpdateReader(Reader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (!_readers.ContainsKey(reader.Id))
            throw new InvalidOperationException($"Немає читача з id={reader.Id}.");
        _readers[reader.Id] = reader;
    }
}