using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

/// <summary>
/// Бізнес-операції бібліотеки. Залежить лише від інтерфейсу ILibraryStore,
/// слів «File» чи «Dictionary» тут немає. Інваріанти перевіряють сутності.
/// </summary>
public sealed class LendingService(ILibraryStore store, IEnumerable<INotificationSink>? sinks = null)
{
    /// <summary>На скільки днів видається примірник (для DueDate в події).</summary>
    public const int LoanPeriodDays = 14;

    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>Стан бібліотеки змінився. Core лише повідомляє, що сталося; реакцію визначає підписник.</summary>
    public event EventHandler<LibraryChangedEventArgs>? Changed;

    // Додатково: приймачі, передані через конструктор (альтернатива події, див. README).
    private readonly List<INotificationSink> _sinks = sinks?.ToList() ?? [];

    private void OnChanged(string copyId, LibraryChangeKind kind, string? readerId = null, DateOnly? dueDate = null)
    {
        var args = new LibraryChangedEventArgs(copyId, kind, readerId, dueDate);
        Changed?.Invoke(this, args);
        foreach (INotificationSink sink in _sinks)
            sink.Notify(args);
    }

    public BookCopy AddBook(string isbn, string title)
    {
        BookCopy copy = BookCopy.Create(NewId("C"), isbn, title);
        _store.AddCopy(copy);
        OnChanged(copy.Id, LibraryChangeKind.BookAdded); // після зміни, не до
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
        OnChanged(copy.Id, LibraryChangeKind.CopyIssued, reader.Id, issuedOn.AddDays(LoanPeriodDays));
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
        OnChanged(copy.Id, LibraryChangeKind.CopyReturned, reader.Id);
    }

    public IReadOnlyList<BookCopy> All() => _store.ListCopies();

    public BookCopy? Find(string id) => _store.GetCopy(id);

    /// <summary>
    /// Пошук примірників за довільною умовою, наприклад <c>c => c.IsIssued</c>.
    /// Критерій задає той, хто викликає, а результат повертається значенням — тому тут Func, а не подія.
    /// </summary>
    public IReadOnlyList<BookCopy> FindAll(Func<BookCopy, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.ListCopies().Where(predicate).ToList();
    }

    public IReadOnlyList<BookCopy> Search(Func<BookCopy, bool> predicate) => FindAll(predicate);

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";
}