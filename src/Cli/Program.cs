using Core.Domain;
using Core.Dto;
using Core.Import;

if (args.Contains("--domain"))
{
    string domainArgPath = args.FirstOrDefault(a => a != "--domain") ?? Path.Combine("data", "sample.csv");
    RunDomainDemo(domainArgPath);
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

void RunDomainDemo(string booksPath)
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
    Console.WriteLine("=== Стани видачі (enum + переходи) ===");
    BookCopy wrongCopy = BookCopy.Create("C-009", "978-966-10-5555-9", "Випадкова книга");
    Loan mistaken = reader.TakeLoan("L-009", wrongCopy, issuedOn);
    Console.WriteLine($"{mistaken.Status}: {mistaken}");
    mistaken.Cancel(wrongCopy);
    Console.WriteLine($"{mistaken.Status}: {mistaken}");
    Console.WriteLine(wrongCopy);

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
    TryDo("порожня назва книги", () => BookCopy.Create("C-008", "978-966-10-5555-8", " "));
    TryDo("некоректний email", () => Reader.Register("R-002", "Іван Петренко", "ivan-at-example"));
    TryDo("повторна видача примірника", () => issuedCopy.Issue());
    TryDo("повернення невиданого примірника", () => copy.Return());
    TryDo("дата повернення раніше дати видачі",
        () => Loan.FromDto(new LoanDto("L-002", "C-002", "R-001", issuedOn, issuedOn.AddDays(-3))));
    TryDo("повторне закриття видачі", () => loan.Close(copy, issuedOn.AddDays(20)));
    TryDo("закриття скасованої видачі", () => mistaken.Close(wrongCopy, issuedOn.AddDays(2)));
    TryDo("невідомий стан у DTO",
        () => Loan.FromDto(new LoanDto("L-003", "C-002", "R-001", issuedOn, null, "Lost")));
    TryDo("стан Returned без дати повернення",
        () => Loan.FromDto(new LoanDto("L-004", "C-002", "R-001", issuedOn, null, "Returned")));
    TryDo("перевищення ліміту відкритих видач", () =>
    {
        Reader greedy = Reader.Register("R-003", "Олена Коваль", "olena@example.com");
        for (int i = 1; i <= Reader.MaxOpenLoans + 1; i++)
            greedy.TakeLoan($"L-1{i:00}", BookCopy.Create($"C-1{i:00}", "978-000-00-0000-0", $"Книга {i}"), issuedOn);
    });
    TryDo("закриття видачі чужим примірником", () =>
    {
        BookCopy ownCopy = BookCopy.Create("C-004", "978-966-10-5555-4", "Чиста архітектура");
        BookCopy otherCopy = BookCopy.Create("C-005", "978-966-10-5555-5", "Предметно-орієнтоване проєктування");
        Loan active = Loan.Open("L-005", ownCopy, "R-001", issuedOn);
        active.Close(otherCopy, issuedOn.AddDays(3));
    });
    TryDo("повернення видачі іншого читача", () =>
    {
        Reader other = Reader.Register("R-004", "Марія Шевчук", "maria@example.com");
        BookCopy otherBook = BookCopy.Create("C-006", "978-966-10-5555-6", "Алгоритми");
        Loan foreign = other.TakeLoan("L-006", otherBook, issuedOn);
        reader.ReturnLoan(foreign, otherBook, issuedOn.AddDays(5));
    });
    TryDo("порожній ідентифікатор читача",
        () => Loan.Open("L-007", copy, "   ", issuedOn));

    Console.WriteLine();
    Console.WriteLine($"Стан після всіх відмов: {copy}; відкритих видач у читача: {reader.OpenLoansCount}");

    Console.WriteLine();
    Console.WriteLine("=== Додаткове завдання: ImportResult<BookDto> → сутності ===");
    if (!File.Exists(booksPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(booksPath)}");
        return;
    }

    ImportResult<BookDto> imported = Path.GetExtension(booksPath).ToLowerInvariant() switch
    {
        ".json" => BookJsonImporter.Load(booksPath),
        _ => BookCsvImporter.Load(booksPath)
    };

    ImportResult<BookCopy> domain = BookCopyAssembler.ToDomain(imported);
    Console.WriteLine($"Створено примірників: {domain.Items.Count}, не пройшли перевірки: {domain.Errors.Count}");
    foreach (BookCopy c in domain.Items.Take(5))
        Console.WriteLine($"  {c}");
    foreach (string e in domain.Errors)
        Console.WriteLine($"  ! {e}");
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
