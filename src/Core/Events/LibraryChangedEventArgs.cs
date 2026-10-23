namespace Core.Events;

public sealed class LibraryChangedEventArgs(
    LibraryChangeKind kind,
    string copyId,
    string? readerId = null,
    DateTime? issuedOn = null) : EventArgs
{
    public LibraryChangeKind Kind { get; } = kind;
    public string CopyId { get; } = copyId;
    public string? ReaderId { get; } = readerId;
    public DateTime? IssuedOn { get; } = issuedOn;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}