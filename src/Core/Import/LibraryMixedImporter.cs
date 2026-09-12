using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class LibraryMixedImporter
{
    private const char Separator = ';';

    public static ImportResult<LibraryRecord> Load(string path)
    {
        var items = new List<LibraryRecord>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<LibraryRecord>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["B", var id, var isbn, var title, var year]
                when int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                => new ParseOk(new BookRecord(new BookDto(id, isbn, title, y))),
            ["B", ..] => new ParseFailed("некоректний формат рядка книги (очікую B;id;isbn;title;year)"),
            ["R", var id, var fullName, var email]
                when !string.IsNullOrWhiteSpace(fullName) && email.Contains('@')
                => new ParseOk(new ReaderRecord(new ReaderDto(id, fullName, email))),
            ["R", ..] => new ParseFailed("некоректний формат рядка читача (очікую R;id;ім'я;email)"),
            [var prefix, ..] => new ParseFailed($"невідомий префікс типу: '{prefix}'"),
            _ => new ParseFailed("порожній або нерозпізнаний рядок")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(LibraryRecord Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}