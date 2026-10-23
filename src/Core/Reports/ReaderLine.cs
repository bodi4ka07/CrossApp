/// рядок звіту про читача та його кількість видач.
namespace Core.Reports;

/// <summary>Рядок звіту «читачі за кількістю видач».</summary>
public sealed record ReaderLine(string ReaderId, string FullName, int LoansCount, int OpenLoans);
