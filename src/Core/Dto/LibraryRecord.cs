namespace Core.Dto;

public abstract record LibraryRecord;
public sealed record BookRecord(BookDto Book) : LibraryRecord;
public sealed record ReaderRecord(ReaderDto Reader) : LibraryRecord;