using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Видача примірника читачеві. Дата видачі фіксується один раз під час відкриття,
/// дата повернення з'являється лише при закритті і не може бути раніше дати видачі.
/// Стан описує <see cref="LoanStatus"/>; допустимі переходи перевіряє EnsureTransition.
/// </summary>
public sealed class Loan
{
    private DateOnly? _returnedOn;

    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateOnly IssuedOn { get; }
    public DateOnly? ReturnedOn => _returnedOn;
    public LoanStatus Status { get; private set; }
    public bool IsClosed => Status != LoanStatus.Active;

    private Loan(string id, string copyId, string readerId, DateOnly issuedOn,
        DateOnly? returnedOn, LoanStatus status)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        _returnedOn = returnedOn;
        Status = status;
    }

    /// <summary>
    /// Відкрити видачу: примірник переходить у стан «виданий», і лише після цього
    /// з'являється об'єкт видачі. Якщо примірник уже виданий — Issue кине виняток
    /// і жодна видача не створиться.
    /// </summary>
    public static Loan Open(string id, BookCopy copy, string readerId, DateOnly issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        Loan loan = Restore(id, copy.Id, readerId, issuedOn, returnedOn: null, LoanStatus.Active);
        copy.Issue();
        return loan;
    }

    /// <summary>
    /// Закрити видачу: примірник повертається на полицю, фіксується дата повернення.
    /// </summary>
    public void Close(BookCopy copy, DateOnly returnedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        EnsureTransition(LoanStatus.Returned);
        EnsureSameCopy(copy);

        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення не може бути раніше дати видачі {IssuedOn:yyyy-MM-dd}");

        copy.Return();
        _returnedOn = returnedOn;
        Status = LoanStatus.Returned;
    }

    /// <summary>
    /// Скасувати помилково оформлену видачу: примірник повертається на полицю,
    /// але дата повернення не фіксується — повернення не відбувалося.
    /// </summary>
    public void Cancel(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        EnsureTransition(LoanStatus.Cancelled);
        EnsureSameCopy(copy);

        copy.Return();
        Status = LoanStatus.Cancelled;
    }

    /// <summary>Скільки днів триває (або тривала) видача.</summary>
    public int DurationInDays(DateOnly today) =>
        (_returnedOn ?? today).DayNumber - IssuedOn.DayNumber;

    // Додаткове завдання 3: допустимі переходи стану — одним switch expression.
    private void EnsureTransition(LoanStatus target)
    {
        bool allowed = (Status, target) switch
        {
            (LoanStatus.Active, LoanStatus.Returned) => true,
            (LoanStatus.Active, LoanStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Видача {Id}: перехід {Status} → {target} неможливий");
    }

    private void EnsureSameCopy(BookCopy copy)
    {
        if (copy.Id != CopyId)
            throw new InvalidOperationException(
                $"Видача {Id} оформлена на примірник {CopyId}, а передано {copy.Id}");
    }

    // Одна точка перевірки для Open і FromDto: інваріанти однакові в обох випадках.
    private static Loan Restore(string id, string copyId, string readerId, DateOnly issuedOn,
        DateOnly? returnedOn, LoanStatus status)
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

        // Стан і дата повернення мають узгоджуватися між собою.
        bool consistent = (status, returnedOn is not null) switch
        {
            (LoanStatus.Returned, true) => true,
            (LoanStatus.Active, false) => true,
            (LoanStatus.Cancelled, false) => true,
            _ => false
        };

        if (!consistent)
            throw new ArgumentException(
                $"Стан '{status}' не узгоджується з датою повернення " +
                $"({(returnedOn is null ? "відсутня" : returnedOn.Value.ToString("yyyy-MM-dd"))})",
                nameof(status));

        return new Loan(id.Trim(), copyId.Trim(), readerId.Trim(), issuedOn, returnedOn, status);
    }

    public LoanDto ToDto() => new(Id, CopyId, ReaderId, IssuedOn, ReturnedOn, Status.ToString());

    public static Loan FromDto(LoanDto dto)
    {
        if (!Enum.TryParse(dto.Status, ignoreCase: true, out LoanStatus status))
            throw new ArgumentException(
                $"Невідомий стан видачі: '{dto.Status}'", nameof(dto));

        return Restore(dto.Id, dto.CopyId, dto.ReaderId, dto.IssuedOn, dto.ReturnedOn, status);
    }

    public override string ToString() =>
        $"{Id}: примірник {CopyId} → читач {ReaderId}, видано {IssuedOn:yyyy-MM-dd}, " +
        Status switch
        {
            LoanStatus.Returned => $"повернено {_returnedOn:yyyy-MM-dd}",
            LoanStatus.Cancelled => "скасовано",
            _ => "не повернено"
        };
}
