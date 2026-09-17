namespace Core.Dto;

public abstract record LibraryEntryDto;

public sealed record BookEntryDto(BookDto Book)
    : LibraryEntryDto;

public sealed record ReaderEntryDto(ReaderDto Reader)
    : LibraryEntryDto;