using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

// Лабораторна 6: події сервісу, підписка з Cli (логи в консоль і в logs/app.log).
if (args.Contains("--events"))
{
    RunEventsDemo();
    return 0;
}

// Лабораторна 6, додаткове: INotificationSink через конструктор + Action<string> в імпорті.
if (args.Contains("--extra"))
{
    RunExtraDemo();
    return 0;
}

// Без аргументів або лише з --file: сервісний сценарій лабораторної 5.
// Старий імпорт (лаб. 3) тепер запускається з явним шляхом: -- data/sample.csv
if (args.Length == 0 || args.All(a => a is "--file" or "--cache" or "--lending"))
{
    RunLendingDemo(args.Contains("--file"));
    return 0;
}

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


void RunLendingDemo(bool useFile)
{
    // Composition root: вибір сховища делеговано StoreFactory (він теж у Cli).
    ILibraryStore store = StoreFactory.Create(args);
    var service = new LendingService(store);
    DateOnly lendToday = DateOnly.FromDateTime(DateTime.Today);

    Console.WriteLine($"Сховище: {store.GetType().Name}");
    if (useFile)
        Console.WriteLine($"Файл: {StoreFactory.DataPath}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 1: успіх ===");
    BookCopy lendBook = service.AddBook("978-0-00-000000-0", "Нова книга");
    Reader lendReader = service.RegisterReader("Тестовий Читач", "reader@example.com");
    Console.WriteLine($"Додано: {lendBook}");
    Console.WriteLine($"Зареєстровано: {lendReader}");

    Loan lendLoan = service.IssueCopy(lendReader.Id, lendBook.Id, lendToday);
    Console.WriteLine($"Видано: {lendLoan}");
    Console.WriteLine($"Знайдено за id: {service.Find(lendBook.Id)}");

    service.ReturnCopy(lendReader.Id, lendBook.Id, lendToday.AddDays(7));
    Console.WriteLine($"Повернено: {service.Find(lendBook.Id)}");

    IReadOnlyList<BookCopy> allCopies = service.All();
    Console.WriteLine($"Усього примірників у сховищі: {allCopies.Count}; останні 3:");
    foreach (BookCopy copyItem in allCopies.TakeLast(3))
        Console.WriteLine($"  {copyItem}");

    Console.WriteLine($"Пошук (Func): виданих зараз — {service.Search(c => c.IsIssued).Count}; " +
        $"з «код» у назві — {service.Search(c => c.Title.Contains("код", StringComparison.OrdinalIgnoreCase)).Count}");

    if (store is CachingLibraryStore caching)
        Console.WriteLine($"Кеш: влучань {caching.Hits}, промахів {caching.Misses}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: відмови ===");
    TryDo("неіснуючий читач", () => service.IssueCopy("R-999", lendBook.Id, lendToday));
    TryDo("неіснуючий примірник", () => service.IssueCopy(lendReader.Id, "C-999", lendToday));
    TryDo("повернення без відкритої видачі", () => service.ReturnCopy(lendReader.Id, lendBook.Id, lendToday));
    TryDo("дубль id примірника", () => store.AddCopy(lendBook));
}

void RunEventsDemo()
{
    ILibraryStore store = StoreFactory.Create(args);
    var service = new LendingService(store);
    DateOnly today = DateOnly.FromDateTime(DateTime.Today);

    // Лог-файл живе в Cli: Core про файлову систему для логів нічого не знає.
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    // Підписник 1: локальний метод (щоб потім можна було відписатися).
    void OnLibraryChanged(object? sender, LibraryChangedEventArgs e)
    {
        string line = ChangeFormat.Line(e);
        Console.WriteLine(line);
        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    service.Changed += OnLibraryChanged;

    // Підписник 2: лямбда із замиканням над локальною змінною. Викликається після першого, в порядку підписки.
    int changes = 0;
    service.Changed += (_, _) => changes++;

    Console.WriteLine($"Сховище: {store.GetType().Name}");
    Console.WriteLine($"Лог: {logPath}");
    Console.WriteLine();
    Console.WriteLine("=== Події ===");
    Reader reader = service.RegisterReader("Читач для подій", "events@example.com"); // події немає: це не зміна книжкового фонду
    BookCopy book = service.AddBook("978-0-00-000000-1", "Книга для подій");
    service.IssueCopy(reader.Id, book.Id, today);
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Відмова: події немає ===");
    TryDo("повторна видача виданого примірника", () => service.IssueCopy(reader.Id, book.Id, today));
    TryDo("неіснуючий читач", () => service.IssueCopy("R-999", book.Id, today));
    Console.WriteLine($"Подій отримано: {changes} (відмови не додали жодної)");

    Console.WriteLine();
    Console.WriteLine("=== Func замість події ===");
    foreach (BookCopy issued in service.FindAll(c => c.IsIssued))
        Console.WriteLine($"  видано: {issued}");

    Console.WriteLine();
    Console.WriteLine("=== Після відписки ===");
    service.Changed -= OnLibraryChanged;
    service.ReturnCopy(reader.Id, book.Id, today.AddDays(7)); // тиха операція: підписка керована
    Console.WriteLine($"Подій отримано: {changes} (+1 лише від лямбди-лічильника; у консолі й логу рядка немає)");
    Console.WriteLine($"Стан примірника: {service.Find(book.Id)}");
}

void RunExtraDemo()
{
    // --- Додаткове 1: приймачі сповіщень передаються сервісу через конструктор (ручний DI) ---
    string sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sinks.log");
    INotificationSink[] sinks = [new ConsoleSink(), new FileSink(sinkLogPath)];
    ILibraryStore sinkStore = StoreFactory.Create(args);
    var sinkService = new LendingService(sinkStore, sinks);
    DateOnly sinkToday = DateOnly.FromDateTime(DateTime.Today);

    Console.WriteLine("=== Додаткове 1: INotificationSink (ConsoleSink + FileSink) ===");
    Console.WriteLine($"Файл приймача: {sinkLogPath}");
    Reader sinkReader = sinkService.RegisterReader("Читач для приймачів", "sinks@example.com");
    BookCopy sinkBook = sinkService.AddBook("978-0-00-000000-2", "Книга для приймачів");
    sinkService.IssueCopy(sinkReader.Id, sinkBook.Id, sinkToday);
    sinkService.ReturnCopy(sinkReader.Id, sinkBook.Id, sinkToday.AddDays(3));

    // --- Додаткове 2: Action<string> як параметр методу імпорту ---
    Console.WriteLine();
    Console.WriteLine("=== Додаткове 2: Action<string> у BookCsvImporter.Load ===");
    string csvFile = Path.Combine("data", "sample.csv");
    if (!File.Exists(csvFile))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(csvFile)}");
        return;
    }

    int skippedCount = 0;
    ImportResult<BookDto> importReport = BookCsvImporter.Load(csvFile, message =>
    {
        skippedCount++; // замикання над локальною змінною
        Console.WriteLine($"  пропущено: {message}");
    });
    Console.WriteLine($"Прийнято {importReport.Items.Count}, звітів через Action: {skippedCount}");
}