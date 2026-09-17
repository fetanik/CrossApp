namespace Core.Dto;

public record LoanDto(
    string Id,
    string BookId,
    string ReaderId,
    DateTime IssuedAt,
    DateTime? ReturnedAt = null);