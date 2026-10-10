namespace Core.Dto;

/// <summary>Формат файлу сховища: примірники та читачі (кожен зі своїми видачами).</summary>
public record LibraryFileDto(
    List<BookCopyDto> Copies,
    List<ReaderFileDto> Readers);

public record ReaderFileDto(
    ReaderDto Reader,
    List<LoanDto> Loans);