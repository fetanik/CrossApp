namespace Core.Dto;

public sealed record BookCopyStateDto(
    string Id,
    string Isbn,
    string Title,
    int Year,
    string? Author,
    bool IsIssued);

public sealed record LoanStateDto(
    string Id,
    string BookCopyId,
    string ReaderId,
    DateTime IssuedOn,
    DateTime? ReturnedOn,
    string Status);

public sealed record LibraryStateDto(
    List<BookCopyStateDto> Copies,
    List<LoanStateDto> Loans);