using Core.Domain;

namespace Core;

/// <summary>
/// Початкові дані для демо (InMemory-сховище) і для звітів тижня 7: 20 примірників від трьох
/// видавців (є кілька примірників однієї книги), 6 читачів з різною кількістю видач, серед них
/// відкриті, повернені й прострочені. Дати відлічуються від сьогодні, тому прострочені є завжди.
/// </summary>
public static class SampleData
{
    /// <summary>
    /// Примірники й читачі створюються разом: видача позначає саме цей екземпляр примірника
    /// виданим, тож окремі виклики Copies() і Readers() узгодженого стану не дали б.
    /// </summary>
    public static (IReadOnlyList<BookCopy> Copies, IReadOnlyList<Reader> Readers) Library()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        List<BookCopy> copies =
        [
            BookCopy.Create("C-001", "978-966-10-5555-1", "Чистий код"),
            BookCopy.Create("C-002", "978-966-10-5555-2", "Рефакторинг"),
            BookCopy.Create("C-003", "978-966-10-5555-3", "Патерни проєктування"),
            BookCopy.Create("C-004", "978-966-10-5555-4", "Чиста архітектура"),
            BookCopy.Create("C-005", "978-966-10-5555-5", "Предметно-орієнтоване проєктування"),
            BookCopy.Create("C-006", "978-966-10-5555-6", "Алгоритми"),
            BookCopy.Create("C-007", "978-966-10-5555-7", "Програміст-прагматик"),
            BookCopy.Create("C-008", "978-966-10-5555-8", "Код досконалий"),
            BookCopy.Create("C-009", "978-966-10-5555-9", "CLR via C#"),
            BookCopy.Create("C-010", "978-966-10-6666-0", "C# in Depth"),
            BookCopy.Create("C-011", "978-966-10-6666-1", "Конкурентність у C#"),
            BookCopy.Create("C-012", "978-966-10-6666-2", "Entity Framework Core в дії"),
            BookCopy.Create("C-013", "978-966-10-6666-3", "Шаблони корпоративних застосунків"),
            BookCopy.Create("C-014", "978-617-12-7777-4", "Мистецтво тестування"),
            BookCopy.Create("C-015", "978-617-12-7777-5", "Вступ до алгоритмів"),
            BookCopy.Create("C-016", "978-617-12-7777-6", "Структури даних та алгоритми"),
            // Додаткові примірники тих самих книг (той самий ISBN).
            BookCopy.Create("C-017", "978-966-10-5555-1", "Чистий код"),
            BookCopy.Create("C-018", "978-966-10-5555-1", "Чистий код"),
            BookCopy.Create("C-019", "978-966-10-5555-2", "Рефакторинг"),
            BookCopy.Create("C-020", "978-966-10-5555-6", "Алгоритми"),
        ];

        List<Reader> readers =
        [
            Reader.Register("R-001", "Жегістовський Богдан", "bohdan@example.com"),
            Reader.Register("R-002", "Іван Петренко", "ivan@example.com"),
            Reader.Register("R-003", "Олена Коваль", "olena@example.com"),
            Reader.Register("R-004", "Марія Шевчук", "maria@example.com"),
            Reader.Register("R-005", "Тарас Мельник", "taras@example.com"),
            Reader.Register("R-006", "Софія Бондар", "sofia@example.com"),
        ];

        BookCopy CopyById(string id) => copies.First(c => c.Id == id);
        Reader ReaderById(string id) => readers.First(r => r.Id == id);

        // returnedDaysAgo == null — видача лишається відкритою.
        void Lend(string readerId, string loanId, string copyId, int issuedDaysAgo, int? returnedDaysAgo = null)
        {
            Reader reader = ReaderById(readerId);
            BookCopy copy = CopyById(copyId);
            Loan loan = reader.TakeLoan(loanId, copy, today.AddDays(-issuedDaysAgo));
            if (returnedDaysAgo is int back)
                reader.ReturnLoan(loan, copy, today.AddDays(-back));
        }

        // Строк видачі — 14 днів: видачі старші за 14 днів і відкриті — прострочені.
        Lend("R-001", "L-001", "C-001", 30);       // відкрита, прострочена на 16 днів
        Lend("R-001", "L-002", "C-002", 20);       // відкрита, прострочена на 6 днів
        Lend("R-001", "L-003", "C-003", 60, 50);   // повернена

        Lend("R-002", "L-004", "C-004", 10);       // відкрита, ще в строк
        Lend("R-002", "L-005", "C-005", 40, 30);   // повернена

        Lend("R-003", "L-006", "C-006", 15);       // відкрита, прострочена на 1 день

        Lend("R-004", "L-007", "C-007", 3);        // відкрита, в строк
        Lend("R-004", "L-008", "C-008", 90, 80);   // повернена
        Lend("R-004", "L-009", "C-009", 70, 65);   // повернена
        Lend("R-004", "L-010", "C-017", 100, 95);  // повернена

        // R-005 — жодної видачі.

        Lend("R-006", "L-011", "C-018", 2);        // відкрита, в строк
        Lend("R-006", "L-012", "C-010", 25, 20);   // повернена

        return (copies, readers);
    }
}
