using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Читач бібліотеки разом зі своїми видачами. Список видач зберігається
/// в приватному полі, назовні віддається лише для читання.
/// </summary>
public sealed class Reader
{
    /// <summary>Скільки примірників читач може тримати на руках одночасно.</summary>
    public const int MaxOpenLoans = 5;

    private readonly List<Loan> _loans = [];

    public string Id { get; }
    public string FullName { get; }
    public string Email { get; }

    public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();
    public int OpenLoansCount => _loans.Count(l => !l.IsClosed);

    private Reader(string id, string fullName, string email)
    {
        Id = id;
        FullName = fullName;
        Email = email;
    }

    public static Reader Register(string id, string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Ім'я читача не може бути порожнім", nameof(fullName));
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException($"Некоректна електронна адреса: '{email}'", nameof(email));

        return new Reader(id.Trim(), fullName.Trim(), email.Trim());
    }

    /// <summary>
    /// Видати читачеві примірник: перевіряємо ліміт відкритих видач, і лише потім
    /// відкриваємо видачу (яка, своєю чергою, позначає примірник як виданий).
    /// </summary>
    public Loan TakeLoan(string loanId, BookCopy copy, DateOnly issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        if (OpenLoansCount >= MaxOpenLoans)
            throw new InvalidOperationException(
                $"Читач {FullName} ({Id}) уже має {OpenLoansCount} відкритих видач, " +
                $"ліміт — {MaxOpenLoans}");

        Loan loan = Loan.Open(loanId, copy, Id, issuedOn);
        _loans.Add(loan);
        return loan;
    }

    public void ReturnLoan(Loan loan, BookCopy copy, DateOnly returnedOn)
    {
        ArgumentNullException.ThrowIfNull(loan);

        if (!_loans.Contains(loan))
            throw new InvalidOperationException(
                $"Видача {loan.Id} не належить читачеві {Id}");

        loan.Close(copy, returnedOn);
    }

    public ReaderDto ToDto() => new(Id, FullName, Email);

    public static Reader FromDto(ReaderDto dto) =>
        Register(dto.Id, dto.FullName, dto.Email);

    public override string ToString() =>
        $"{Id} {FullName} <{Email}> — відкритих видач: {OpenLoansCount}";
}
