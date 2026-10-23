using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

// Передає одне сповіщення декільком sink-об'єктам.
public sealed class CompositeNotificationSink : INotificationSink
{
    private readonly IReadOnlyList<INotificationSink> _sinks;

    public CompositeNotificationSink(params INotificationSink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        _sinks = Array.AsReadOnly((INotificationSink[])sinks.Clone());
    }

    public void Notify(LibraryChangedEventArgs change)
    {
        foreach (INotificationSink sink in _sinks)
            sink.Notify(change);
    }
}
