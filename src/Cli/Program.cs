using Core.Domain;
using Core.Dto;
using Core.Import;

if (args.Contains("--domain"))
{
    RunDomainDemo();
    return 0;
}

if (args.Contains("--mixed"))
{
    string mixedArgPath = args.FirstOrDefault(a => a != "--mixed") ?? Path.Combine("data", "sample-mixed.csv");
    RunMixedDemo(mixedArgPath);
    return 0;
}

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<BookDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => BookCsvImporter.Load(path),
    ".json" => BookJsonImporter.Load(path),
    var ext => throw new NotSupportedException($"Непідтримуване розширення файлу: {ext}")
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (BookDto b in result.Items.Take(5))
    Console.WriteLine($"  {b.Id,-6} {b.Isbn,-18} {b.Title,-30} {b.Year}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

int total = result.Items.Count + result.Errors.Count;
double errorRate = total == 0 ? 0 : result.Errors.Count * 100.0 / total;
Console.WriteLine($"Статистика: усього {total} / прийнято {result.Items.Count} / пропущено {result.Errors.Count} / {errorRate:F1}% помилок");

return 0;

void RunMixedDemo(string mixedPath)
{
    if (!File.Exists(mixedPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(mixedPath)}");
        return;
    }

    ImportResult<LibraryRecord> mixed = LibraryMixedImporter.Load(mixedPath);

    int bookCount = mixed.Items.Count(r => r is BookRecord);
    int readerCount = mixed.Items.Count(r => r is ReaderRecord);

    Console.WriteLine($"Змішаний імпорт: книг {bookCount}, читачів {readerCount}");
    foreach (LibraryRecord record in mixed.Items)
    {
        string line = record switch
        {
            BookRecord b => $"  [Книга] {b.Book.Id} {b.Book.Title}",
            ReaderRecord r => $"  [Читач] {r.Reader.Id} {r.Reader.FullName}",
            _ => "  [?]"
        };
        Console.WriteLine(line);
    }

    if (mixed.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {mixed.Errors.Count}");
        foreach (string e in mixed.Errors)
            Console.WriteLine($"  ! {e}");
    }
}

void RunDomainDemo()
{
    DateOnly issuedOn = new(2026, 10, 1);

    Console.WriteLine("=== Сценарій 1: успіх ===");

    Reader reader = Reader.Register("R-001", "Жегістовський Богдан", "bohdan@example.com");
    BookCopy copy = BookCopy.Create("C-001", "978-966-10-5555-1", "Чистий код");
    Console.WriteLine(reader);
    Console.WriteLine(copy);

    Loan loan = reader.TakeLoan("L-001", copy, issuedOn);
    Console.WriteLine(loan);
    Console.WriteLine(copy);
    Console.WriteLine($"Відкритих видач у читача: {reader.OpenLoansCount}");

    reader.ReturnLoan(loan, copy, issuedOn.AddDays(14));
    Console.WriteLine(loan);
    Console.WriteLine(copy);
    Console.WriteLine($"Тривалість видачі: {loan.DurationInDays(issuedOn.AddDays(14))} дн.");

    Console.WriteLine();
    Console.WriteLine("=== Мапінг сутність ↔ DTO ===");
    LoanDto loanDto = loan.ToDto();
    Loan restored = Loan.FromDto(loanDto);
    Console.WriteLine($"ToDto:   {loanDto}");
    Console.WriteLine($"FromDto: {restored}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

    BookCopy issuedCopy = BookCopy.Create("C-002", "978-966-10-5555-2", "Рефакторинг");
    issuedCopy.Issue();

    TryDo("порожній ISBN", () => BookCopy.Create("C-003", "   ", "Патерни"));
    TryDo("некоректний email", () => Reader.Register("R-002", "Іван Петренко", "ivan-at-example"));
    TryDo("повторна видача примірника", () => issuedCopy.Issue());
    TryDo("повернення невиданого примірника", () => copy.Return());
    TryDo("дата повернення раніше дати видачі",
        () => Loan.FromDto(new LoanDto("L-002", "C-002", "R-001", issuedOn, issuedOn.AddDays(-3))));
    TryDo("повторне закриття видачі", () => loan.Close(copy, issuedOn.AddDays(20)));

    Console.WriteLine();
    Console.WriteLine($"Стан після всіх відмов: {copy}; відкритих видач у читача: {reader.OpenLoansCount}");

}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}
