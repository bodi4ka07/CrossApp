using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Видача примірника читачеві. Дата видачі фіксується один раз під час відкриття,
/// дата повернення з'являється лише при закритті і не може бути раніше дати видачі.
/// </summary>
public sealed class Loan
{
    private DateOnly? _returnedOn;

    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateOnly IssuedOn { get; }
    public DateOnly? ReturnedOn => _returnedOn;
    public bool IsClosed => _returnedOn.HasValue;

    private Loan(string id, string copyId, string readerId, DateOnly issuedOn, DateOnly? returnedOn)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        _returnedOn = returnedOn;
    }

    /// <summary>
    /// Відкрити видачу: примірник переходить у стан «виданий», і лише після цього
    /// з'являється об'єкт видачі. Якщо примірник уже виданий — Issue кине виняток
    /// і жодна видача не створиться.
    /// </summary>
    public static Loan Open(string id, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        Loan loan = Restore(id, copy.Id, readerId, issuedOn, returnedOn: null);
        copy.Issue();
        return loan;
    }

    /// <summary>
    /// Закрити видачу: примірник повертається на полицю, фіксується дата повернення.
    /// </summary>
    public void Close(BookCopy copy, DateOnly returnedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        if (IsClosed)
            throw new InvalidOperationException(
                $"Видача {Id} вже закрита {_returnedOn:yyyy-MM-dd}, повторне закриття неможливе");
        if (copy.Id != CopyId)
            throw new InvalidOperationException(
                $"Видача {Id} оформлена на примірник {CopyId}, а передано {copy.Id}");
        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення не може бути раніше дати видачі {IssuedOn:yyyy-MM-dd}");

        copy.Return();
        _returnedOn = returnedOn;
    }

    /// <summary>Скільки днів триває (або тривала) видача.</summary>
    public int DurationInDays(DateOnly today) =>
        (_returnedOn ?? today).DayNumber - IssuedOn.DayNumber;

    // Одна точка перевірки для Open і FromDto: інваріанти однакові в обох випадках.
    private static Loan Restore(string id, string copyId, string readerId, DateOnly issuedOn,
        DateOnly? returnedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Номер видачі обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(copyId))
            throw new ArgumentException("Ідентифікатор примірника обов'язковий", nameof(copyId));
        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));
        if (returnedOn is { } returned && returned < issuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returned,
                $"Дата повернення не може бути раніше дати видачі {issuedOn:yyyy-MM-dd}");

        return new Loan(id.Trim(), copyId.Trim(), readerId.Trim(), issuedOn, returnedOn);
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn);

    public static Loan FromDto(LoanDto dto) =>
        Restore(dto.Id, dto.CopyId, dto.ReaderId, dto.IssuedOn, dto.ReturnedOn);

    public override string ToString() =>
        $"{Id}: примірник {CopyId} → читач {ReaderId}, видано {IssuedOn:yyyy-MM-dd}" +
        (IsClosed ? $", повернено {_returnedOn:yyyy-MM-dd}" : ", не повернено");
}
