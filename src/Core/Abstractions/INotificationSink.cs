using Core.Events;

namespace Core.Abstractions;

/// <summary>
/// Приймач сповіщень про зміни бібліотеки. Сервіс отримує приймачі через конструктор
/// (ручний DI), а реалізації (консоль, файл) живуть у Cli — Core про них нічого не знає.
/// </summary>
public interface INotificationSink
{
    void Notify(LibraryChangedEventArgs e);
}
