namespace Core.Dto;

public record BookCopyDto(
    string Id,
    string Isbn,
    string Title,
    bool IsIssued);
