using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(LibraryChangedEventArgs change)
    {
        Console.WriteLine("[DI ConsoleSink] " + NotificationFormatter.Format(change));
    }
}
