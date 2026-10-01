using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path)
    {
        var items = new List<BookDto>();
        var errors = new List<string>();

        string json = File.ReadAllText(path, Encoding.UTF8);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("JSON має містити масив записів");
                return new ImportResult<BookDto>(items, errors);
            }

            int number = 0;

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                number++;

                if (element.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"запис {number}: очікую JSON-об'єкт");
                    continue;
                }

                string id = GetString(element, "id") ?? "";
                string isbn = GetString(element, "isbn") ?? "";
                string title = GetString(element, "title") ?? "";
                string? author = GetString(element, "author");

                if (string.IsNullOrWhiteSpace(isbn) ||
                    string.IsNullOrWhiteSpace(title))
                {
                    errors.Add(
                        $"запис {number}: ISBN або назва книги порожні");
                    continue;
                }

                if (!TryGetProperty(element, "year", out JsonElement yearElement))
                {
                    errors.Add($"запис {number}: рік відсутній");
                    continue;
                }

                if (yearElement.ValueKind != JsonValueKind.Number ||
                    !yearElement.TryGetInt32(out int year) ||
                    year < 1450 ||
                    year > DateTime.Now.Year)
                {
                    errors.Add(
                        $"запис {number}: рік '{yearElement}' поза допустимими межами");
                    continue;
                }

                items.Add(
                    new BookDto(
                        id,
                        isbn,
                        title,
                        year,
                        author));
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"помилка структури JSON: {ex.Message}");
        }

        return new ImportResult<BookDto>(items, errors);
    }

    private static string? GetString(
        JsonElement element,
        string name)
    {
        return TryGetProperty(element, name, out JsonElement value)
            ? value.GetString()
            : null;
    }

    private static bool TryGetProperty(
        JsonElement element,
        string name,
        out JsonElement value)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}