using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
    }

    public void Notify(LibraryChangedEventArgs change)
    {
        File.AppendAllText(
            _path,
            NotificationFormatter.Format(change) + Environment.NewLine);
    }
}
