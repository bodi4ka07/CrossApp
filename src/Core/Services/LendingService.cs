using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

/// <summary>
/// Бізнес-операції бібліотеки. Залежить лише від інтерфейсу ILibraryStore,
/// слів «File» чи «Dictionary» тут немає. Інваріанти перевіряють сутності.
/// </summary>
public sealed class LendingService(ILibraryStore store)
{
    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public BookCopy AddBook(string isbn, string title)
    {
        BookCopy copy = BookCopy.Create(NewId("C"), isbn, title);
        _store.AddCopy(copy);
        return copy;
    }

    public Reader RegisterReader(string fullName, string email)
    {
        Reader reader = Reader.Register(NewId("R"), fullName, email);
        _store.AddReader(reader);
        return reader;
    }

    public Loan IssueCopy(string readerId, string copyId, DateOnly issuedOn)
    {
        Reader reader = _store.GetReader(readerId)
            ?? throw new InvalidOperationException($"Немає читача з id={readerId}.");
        BookCopy copy = _store.GetCopy(copyId)
            ?? throw new InvalidOperationException($"Немає примірника з id={copyId}.");

        Loan loan = reader.TakeLoan(NewId("L"), copy, issuedOn); // перевірки лімітів і стану — в домені
        _store.UpdateCopy(copy);
        _store.UpdateReader(reader);
        return loan;
    }

    public void ReturnCopy(string readerId, string copyId, DateOnly returnedOn)
    {
        Reader reader = _store.GetReader(readerId)
            ?? throw new InvalidOperationException($"Немає читача з id={readerId}.");
        BookCopy copy = _store.GetCopy(copyId)
            ?? throw new InvalidOperationException($"Немає примірника з id={copyId}.");
        Loan loan = reader.FindOpenLoan(copyId)
            ?? throw new InvalidOperationException(
                $"У читача {readerId} немає відкритої видачі примірника {copyId}.");

        reader.ReturnLoan(loan, copy, returnedOn);
        _store.UpdateCopy(copy);
        _store.UpdateReader(reader);
    }

    public IReadOnlyList<BookCopy> All() => _store.ListCopies();

    public BookCopy? Find(string id) => _store.GetCopy(id);

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";
}