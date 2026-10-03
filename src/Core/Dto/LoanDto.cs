namespace Core.Dto;

public record LoanDto(
    string Id,
    string CopyId,
    string ReaderId,
    DateOnly IssuedOn,
    DateOnly? ReturnedOn = null);
