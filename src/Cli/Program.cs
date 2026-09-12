using Core.Dto;
using Core.Import;

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