using Core.Domain;

namespace Core.Abstractions;

/// <summary>
/// Контракт сховища бібліотеки. Лише ті операції, які справді потрібні LendingService.
/// Видачі (Loan) зберігаються разом з читачем: Reader — агрегат, який володіє своїми видачами.
/// </summary>
public interface ILibraryStore
{
    IReadOnlyList<BookCopy> ListCopies();
    BookCopy? GetCopy(string id);
    void AddCopy(BookCopy copy);
    void UpdateCopy(BookCopy copy);

    Reader? GetReader(string id);
    void AddReader(Reader reader);
    void UpdateReader(Reader reader);
}