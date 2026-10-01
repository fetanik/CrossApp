using Core.Dto;

namespace Core.Domain;

public sealed class BookCopy
{
    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public int Year { get; }
    public string? Author { get; }

    public bool IsIssued { get; private set; }

    private BookCopy(
        string id,
        string isbn,
        string title,
        int year,
        string? author,
        bool isIssued)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        Year = year;
        Author = author;
        IsIssued = isIssued;
    }

    public static BookCopy Create(
        string id,
        string isbn,
        string title,
        int year,
        string? author = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Ідентифікатор примірника не може бути порожнім",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(isbn))
        {
            throw new ArgumentException(
                "ISBN не може бути порожнім",
                nameof(isbn));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Назва книги не може бути порожньою",
                nameof(title));
        }

        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                year,
                "Рік видання має бути більшим за нуль");
        }

        return new BookCopy(
            id.Trim(),
            isbn.Trim(),
            title.Trim(),
            year,
            string.IsNullOrWhiteSpace(author)
                ? null
                : author.Trim(),
            false);
    }

    public void Issue()
    {
        if (IsIssued)
        {
            throw new InvalidOperationException(
                $"Примірник {Id} вже виданий, повторна видача неможлива");
        }

        IsIssued = true;
    }

    public void Return()
    {
        if (!IsIssued)
        {
            throw new InvalidOperationException(
                $"Примірник {Id} не виданий, тому його неможливо повернути");
        }

        IsIssued = false;
    }

    public BookDto ToDto() =>
        new(
            Id,
            Isbn,
            Title,
            Year,
            Author);

    public static BookCopy FromDto(BookDto dto) =>
        Create(
            dto.Id,
            dto.Isbn,
            dto.Title,
            dto.Year,
            dto.Author);

    public override string ToString()
    {
        string author =
            Author ?? "автор невідомий";

        string status =
            IsIssued ? "видана" : "доступна";

        return $"{Id} [{Isbn}] {Title} ({Year}), " +
               $"{author} — {status}";
    }
}