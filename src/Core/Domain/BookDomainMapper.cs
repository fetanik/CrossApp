using Core.Dto;

namespace Core.Domain;

public static class BookDomainMapper
{
    public static ImportResult<BookCopy> ToEntities(
        ImportResult<BookDto> source)
    {
        var entities = new List<BookCopy>();
        var errors = new List<string>(source.Errors);

        foreach (BookDto dto in source.Items)
        {
            try
            {
                BookCopy book = BookCopy.FromDto(dto);
                entities.Add(book);
            }
            catch (ArgumentException ex)
            {
                errors.Add(
                    $"Запис {dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<BookCopy>(
            entities,
            errors);
    }
}