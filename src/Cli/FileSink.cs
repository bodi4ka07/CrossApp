using Core.Abstractions;
using Core.Events;

/// <summary>Приймач сповіщень: дописує рядок у файл (каталог створюється кодом).</summary>
internal sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public void Notify(LibraryChangedEventArgs e) =>
        File.AppendAllText(_path, ChangeFormat.Line(e) + Environment.NewLine);
}
