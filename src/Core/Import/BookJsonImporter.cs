using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path)
    {
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<BookDto> items =
                JsonSerializer.Deserialize<List<BookDto>>(json, options) ?? [];

            return new ImportResult<BookDto>(items, errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"помилка JSON: {ex.Message}");

            return new ImportResult<BookDto>([], errors);
        }
    }
}