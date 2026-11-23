using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Domain;
using Core.Dto;
using Core.Exceptions;

namespace Core.Import;

public sealed class JsonCatalogImporter : ICatalogImporter, ICatalogStreamImporter
{
    public async Task<IReadOnlyList<BookCopy>> LoadAsync(
        string path,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using FileStream stream = OpenRead(path);

        using JsonDocument document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: ct);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("JSON має містити масив записів каталогу.");
        }

        var copies = new List<BookCopy>();
        int number = 0;

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            number++;
            copies.Add(MapRecord(element, number));
        }

        return copies;
    }

    public async IAsyncEnumerable<BookCopy> ReadAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using FileStream stream = OpenRead(path);

        int number = 0;

        await foreach (JsonElement element in JsonSerializer
            .DeserializeAsyncEnumerable<JsonElement>(stream, cancellationToken: ct)
            .WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            number++;
            yield return MapRecord(element, number);
        }
    }

    private static FileStream OpenRead(string path)
    {
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
    }

    private static BookCopy MapRecord(JsonElement element, int number)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new DomainException(
                $"Запис {number}: очікую JSON-об'єкт");
        }

        if (!TryReadYear(element, out int year, out string yearError))
        {
            throw new DomainException($"Запис {number}: {yearError}");
        }

        try
        {
            var dto = new BookDto(
                ReadString(element, "id") ?? "",
                ReadString(element, "isbn") ?? "",
                ReadString(element, "title") ?? "",
                year,
                ReadString(element, "author"));

            return BookCopy.FromDto(dto);
        }
        catch (ArgumentException ex)
        {
            throw new DomainException(
                $"Запис {number}: {ex.Message}",
                ex);
        }
    }

    private static bool TryReadYear(
        JsonElement element,
        out int year,
        out string error)
    {
        year = 0;

        if (!TryGetProperty(element, "year", out JsonElement value))
        {
            error = "рік відсутній";
            return false;
        }

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out year) ||
            year < 1450 ||
            year > DateTime.Now.Year)
        {
            error = "рік поза допустимими межами";
            return false;
        }

        error = "";
        return true;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static bool TryGetProperty(
        JsonElement element,
        string name,
        out JsonElement value)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
