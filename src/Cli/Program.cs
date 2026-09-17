using Core.Dto;
using Core.Import;

bool mixedMode = args.Any(
    a => a.Equals(
        "--mixed",
        StringComparison.OrdinalIgnoreCase));

string path = args.FirstOrDefault(
    a => !a.StartsWith("--"))
    ?? Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine(
        $"Файл не знайдено: {Path.GetFullPath(path)}");

    return 1;
}

if (mixedMode)
{
    ImportResult<LibraryEntryDto> result =
        LibraryMixedImporter.Load(path);

    Console.WriteLine(
        $"Завантажено записів: {result.Items.Count}");

    foreach (LibraryEntryDto item
             in result.Items.Take(5))
    {
        string text = item switch
        {
            BookEntryDto b =>
                $" BOOK   {b.Book.Id,-6} " +
                $"{b.Book.Title,-25} {b.Book.Year}",

            ReaderEntryDto r =>
                $" READER {r.Reader.Id,-6} " +
                $"{r.Reader.FullName,-25} " +
                $"{r.Reader.Email ?? "(email відсутній)"}",

            _ => " Невідомий запис"
        };

        Console.WriteLine(text);
    }

    PrintErrors(result);
    PrintStatistics(result);

    return 0;
}

string extension =
    Path.GetExtension(path).ToLowerInvariant();

ImportResult<BookDto>? bookResult =
    extension switch
    {
        ".csv" => BookCsvImporter.Load(path),
        ".json" => BookJsonImporter.Load(path),
        _ => null
    };

if (bookResult is null)
{
    Console.WriteLine(
        $"Непідтримуваний формат файлу: {extension}");

    return 1;
}

Console.WriteLine(
    $"Завантажено записів: {bookResult.Items.Count}");

foreach (BookDto book
         in bookResult.Items.Take(5))
{
    Console.WriteLine(
        $" {book.Id,-6} " +
        $"{book.Isbn,-15} " +
        $"{book.Title,-25} " +
        $"{book.Year}");
}

PrintErrors(bookResult);
PrintStatistics(bookResult);

return 0;


static void PrintErrors<T>(
    ImportResult<T> result)
{
    if (result.Errors.Count == 0)
        return;

    Console.WriteLine(
        $"Пропущено рядків: {result.Errors.Count}");

    foreach (string error in result.Errors)
    {
        Console.WriteLine($" ! {error}");
    }
}


static void PrintStatistics<T>(
    ImportResult<T> result)
{
    int accepted = result.Items.Count;
    int skipped = result.Errors.Count;
    int total = accepted + skipped;

    double errorPercentage =
        total == 0
            ? 0
            : (double)skipped / total * 100;

    Console.WriteLine(
        $"Статистика: усього {total} / " +
        $"прийнято {accepted} / " +
        $"пропущено {skipped} / " +
        $"помилок {errorPercentage:F1}%");
}