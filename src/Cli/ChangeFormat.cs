using Core.Events;

/// <summary>Спільне форматування рядка про зміну для консолі й файлу (живе в Cli).</summary>
internal static class ChangeFormat
{
    public static string Line(LibraryChangedEventArgs e)
    {
        string details = e.Kind switch
        {
            LibraryChangeKind.CopyIssued => $"видано читачу {e.ReaderId} до {e.DueDate:yyyy-MM-dd}",
            LibraryChangeKind.CopyReturned => $"повернено читачем {e.ReaderId}",
            _ => "додано до фонду"
        };
        return $"{e.At:HH:mm:ss} {e.Kind,-12} {e.CopyId} {details}";
    }
}
