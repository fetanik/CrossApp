using Core.Events;

namespace Core.Abstractions;

// Абстракція, через яку Core може повідомляти про зміни,
// не знаючи про Console.WriteLine або лог-файли.
public interface INotificationSink
{
    void Notify(LibraryChangedEventArgs change);
}
