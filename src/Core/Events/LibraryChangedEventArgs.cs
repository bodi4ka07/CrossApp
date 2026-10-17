namespace Core.Events;

/// <summary>
/// Дані події «стан бібліотеки змінився». ReaderId і DueDate порожні там, де не мають сенсу
/// (наприклад, для BookAdded читача немає, а при поверненні термін уже не потрібен).
/// </summary>
public sealed class LibraryChangedEventArgs(
    string copyId, LibraryChangeKind kind, string? readerId, DateOnly? dueDate) : EventArgs
{
    public string CopyId { get; } = copyId;
    public LibraryChangeKind Kind { get; } = kind;
    public string? ReaderId { get; } = readerId;
    public DateOnly? DueDate { get; } = dueDate;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
