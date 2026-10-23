/// рядок звіту про прострочені видачі.
namespace Core.Reports;

/// <summary>Рядок звіту «прострочені видачі». DTO: контракт звіту не залежить від доменних сутностей.</summary>
public sealed record OverdueLine(
    string ReaderId, string ReaderName, string CopyId,
    DateOnly IssuedOn, DateOnly DueDate, int DaysOverdue);
