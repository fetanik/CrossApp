using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class LibraryMixedImporter
{
    private const char Separator = ';';

    public static ImportResult<LibraryEntryDto> Load(string path)
    {
        var items = new List<LibraryEntryDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) ||
                line.StartsWith('#'))
            {
                continue;
            }

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add(
                        $"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<LibraryEntryDto>(
            items,
            errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(
            Separator,
            StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["B", _, _, "", _]
                => new ParseFailed(
                    "назва книги порожня"),

            ["B", _, _, _, var year]
                when !int.TryParse(
                    year,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int y)
                || y < 1450
                || y > DateTime.Now.Year
                => new ParseFailed(
                    $"рік '{year}' поза допустимими межами"),

            ["B", var id, var isbn, var title, var year]
                => new ParseOk(
                    new BookEntryDto(
                        new BookDto(
                            id,
                            isbn,
                            title,
                            int.Parse(
                                year,
                                CultureInfo.InvariantCulture)))),

            ["R", _, "", _]
                => new ParseFailed(
                    "ім'я читача порожнє"),

            ["R", var id, var fullName, var email]
                => new ParseOk(
                    new ReaderEntryDto(
                        new ReaderDto(
                            id,
                            fullName,
                            string.IsNullOrWhiteSpace(email)
                                ? null
                                : email))),

            [var type, ..]
                => new ParseFailed(
                    $"невідомий тип запису '{type}'"),

            _ => new ParseFailed(
                $"неправильний формат рядка")
        };
    }

    private abstract record ParseOutcome;

    private sealed record ParseOk(
        LibraryEntryDto Value)
        : ParseOutcome;

    private sealed record ParseFailed(
        string Reason)
        : ParseOutcome;
}