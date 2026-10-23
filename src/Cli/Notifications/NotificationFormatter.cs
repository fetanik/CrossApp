using Core.Events;

namespace Cli.Notifications;

internal static class NotificationFormatter
{
    public static string Format(LibraryChangedEventArgs e)
    {
        string line = $"{e.At:HH:mm:ss} {e.Kind,-12} CopyId={e.CopyId}";

        if (e.ReaderId is not null)
            line += $" ReaderId={e.ReaderId}";

        if (e.IssuedOn is DateTime issuedOn)
            line += $" дата видачі {issuedOn:dd.MM.yyyy}";

        return line;
    }
}
