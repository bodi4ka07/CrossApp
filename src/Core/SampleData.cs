using Core.Domain;

namespace Core;

/// <summary>Початкові дані для демо (InMemory-сховище) і для тижня 7 (LINQ).</summary>
public static class SampleData
{
    public static IEnumerable<BookCopy> Copies() =>
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
        BookCopy.Create("C-014", "978-966-10-6666-4", "Мистецтво тестування"),
        BookCopy.Create("C-015", "978-966-10-6666-5", "Вступ до алгоритмів"),
        BookCopy.Create("C-016", "978-966-10-6666-6", "Структури даних та алгоритми"),
    ];

    public static IEnumerable<Reader> Readers() =>
    [
        Reader.Register("R-001", "Жегістовський Богдан", "bohdan@example.com"),
        Reader.Register("R-002", "Іван Петренко", "ivan@example.com"),
        Reader.Register("R-003", "Олена Коваль", "olena@example.com"),
    ];
}
