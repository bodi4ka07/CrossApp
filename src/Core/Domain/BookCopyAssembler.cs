using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Додаткове завдання 1: місток між тижнем 3 (DTO з файлу) і тижнем 4 (сутності).
/// Та сама ідея «дані + помилки»: рядки, які не пройшли інваріанти, не зупиняють
/// імпорт, а потрапляють у перелік помилок.
/// </summary>
public static class BookCopyAssembler
{
    public static ImportResult<BookCopy> ToDomain(ImportResult<BookDto> imported)
    {
        ArgumentNullException.ThrowIfNull(imported);

        var copies = new List<BookCopy>();
        var errors = new List<string>(imported.Errors);

        foreach (BookDto dto in imported.Items)
        {
            try
            {
                copies.Add(BookCopy.FromBookDto(dto));
            }
            catch (ArgumentException ex)
            {
                errors.Add($"запис '{dto.Id}': {ex.Message}");
            }
        }

        return new ImportResult<BookCopy>(copies, errors);
    }
}
