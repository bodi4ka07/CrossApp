using Core.Abstractions;
using Core.Events;

/// <summary>Приймач сповіщень: друкує рядок у консоль.</summary>
internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(LibraryChangedEventArgs e) => Console.WriteLine(ChangeFormat.Line(e));
}
