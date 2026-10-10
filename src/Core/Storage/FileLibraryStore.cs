using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

/// <summary>
/// Файлове сховище: кеш у пам'яті, дозавантаження при першому зверненні,
/// запис на диск після кожної зміни. На диск ідуть DTO, а не сутності;
/// при читанні FromDto проходить ті самі інваріанти, що й створення.
/// </summary>
public sealed class FileLibraryStore(string path) : ILibraryStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly Dictionary<string, BookCopy> _copies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Reader> _readers = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            LibraryFileDto? data = JsonSerializer.Deserialize<LibraryFileDto>(File.ReadAllText(_path));
            if (data is not null)
            {
                foreach (BookCopyDto dto in data.Copies ?? [])
                {
                    BookCopy copy = BookCopy.FromDto(dto);
                    _copies[copy.Id] = copy;
                }

                foreach (ReaderFileDto item in data.Readers ?? [])
                {
                    Reader reader = Reader.FromDto(item.Reader, item.Loans ?? []);
                    _readers[reader.Id] = reader;
                }
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        var data = new LibraryFileDto(
            _copies.Values.Select(c => c.ToDto()).ToList(),
            _readers.Values
                .Select(r => new ReaderFileDto(r.ToDto(), r.Loans.Select(l => l.ToDto()).ToList()))
                .ToList());

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(data, Options));
    }

    public IReadOnlyList<BookCopy> ListCopies()
    {
        EnsureLoaded();
        return _copies.Values.ToList();
    }

    public BookCopy? GetCopy(string id)
    {
        EnsureLoaded();
        return _copies.GetValueOrDefault(id);
    }

    public void AddCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        EnsureLoaded();
        if (_copies.ContainsKey(copy.Id))
            throw new InvalidOperationException($"Примірник з id={copy.Id} уже існує.");
        _copies.Add(copy.Id, copy);
        Flush();
    }

    public void UpdateCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        EnsureLoaded();
        if (!_copies.ContainsKey(copy.Id))
            throw new InvalidOperationException($"Немає примірника з id={copy.Id}.");
        _copies[copy.Id] = copy;
        Flush();
    }

    public Reader? GetReader(string id)
    {
        EnsureLoaded();
        return _readers.GetValueOrDefault(id);
    }

    public void AddReader(Reader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        EnsureLoaded();
        if (_readers.ContainsKey(reader.Id))
            throw new InvalidOperationException($"Читач з id={reader.Id} уже існує.");
        _readers.Add(reader.Id, reader);
        Flush();
    }

    public void UpdateReader(Reader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        EnsureLoaded();
        if (!_readers.ContainsKey(reader.Id))
            throw new InvalidOperationException($"Немає читача з id={reader.Id}.");
        _readers[reader.Id] = reader;
        Flush();
    }
}