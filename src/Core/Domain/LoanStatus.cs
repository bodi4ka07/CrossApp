namespace Core.Domain;

/// <summary>
/// Явний стан видачі. Замість набору прапорців — одне поле,
/// допустимі переходи між станами перевіряє <see cref="Loan"/>.
/// </summary>
public enum LoanStatus
{
    /// <summary>Примірник на руках у читача.</summary>
    Active,

    /// <summary>Примірник повернено до бібліотеки.</summary>
    Returned,

    /// <summary>Видачу скасовано (оформлена помилково), примірник повернено на полицю.</summary>
    Cancelled
}
