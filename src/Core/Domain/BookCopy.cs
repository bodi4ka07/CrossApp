using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Примірник книги на полиці. Стан «виданий / на полиці» змінюють
/// лише методи Issue і Return, тому примірник неможливо видати двічі.
/// </summary>
public sealed class BookCopy
{
    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public bool IsIssued { get; private set; }

    private BookCopy(string id, string isbn, string title, bool isIssued)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        IsIssued = isIssued;
    }

    // Єдиний спосіб створити примірник: усі перевірки тут.
    public static BookCopy Create(string id, string isbn, string title, bool isIssued = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор примірника обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва книги не може бути порожньою", nameof(title));

        return new BookCopy(id.Trim(), isbn.Trim().ToUpperInvariant(), title.Trim(), isIssued);
    }

    public void Issue()
    {
        if (IsIssued)
            throw new InvalidOperationException(
                $"Примірник {Id} ({Title}) вже виданий, повторна видача неможлива");

        IsIssued = true;
    }

    public void Return()
    {
        if (!IsIssued)
            throw new InvalidOperationException(
                $"Примірник {Id} ({Title}) не виданий, повертати нічого");

        IsIssued = false;
    }

    // Мапінг у формат тижня 3 і назад — знадобиться сховищу тижня 5.
    public BookCopyDto ToDto() => new(Id, Isbn, Title, IsIssued);

    public static BookCopy FromDto(BookCopyDto dto) =>
        Create(dto.Id, dto.Isbn, dto.Title, dto.IsIssued);

    // Примірник можна зібрати і з BookDto тижня 3: один рядок файлу — один примірник.
    public static BookCopy FromBookDto(BookDto dto) =>
        Create(dto.Id, dto.Isbn, dto.Title);

    public override string ToString() =>
        $"{Id} [{Isbn}] {Title} — {(IsIssued ? "виданий" : "на полиці")}";
}
