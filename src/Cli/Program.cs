using Core.Domain;
using Core.Dto;
using Core.Import;

bool importMode = args.Any(
    a => a.Equals(
        "--import",
        StringComparison.OrdinalIgnoreCase));

bool mixedMode = args.Any(
    a => a.Equals(
        "--mixed",
        StringComparison.OrdinalIgnoreCase));

if (!importMode)
{
    RunDomainDemo();
    return 0;
}

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


static void RunDomainDemo()
{
    Console.WriteLine(
        "=== Сценарій 1: успіх ===");

    BookCopy book = BookCopy.Create(
        "B-001",
        "978-617-123456-7",
        "Кобзар",
        2020,
        "Тарас Шевченко");

    Console.WriteLine(book);

    book.Issue();

    Console.WriteLine(book);

    Loan loan = Loan.Open(
        "L-001",
        book.Id,
        "R-001",
        new DateTime(2026, 10, 1));

    Console.WriteLine(loan);

    loan.Close(
        new DateTime(2026, 10, 10));

    book.Return();

    Console.WriteLine(loan);
    Console.WriteLine(book);

    Console.WriteLine();

    Console.WriteLine(
        "=== Сценарій 2: порушення інваріантів ===");

    TryDo(
        "порожній ISBN",
        () => BookCopy.Create(
            "B-002",
            " ",
            "Тестова книга",
            2020));

    TryDo(
        "некоректний рік",
        () => BookCopy.Create(
            "B-003",
            "978-1234567890",
            "Тестова книга",
            -5));

    book.Issue();

    bool stateBeforeFailedIssue =
        book.IsIssued;

    TryDo(
        "повторна видача книги",
        () => book.Issue());

    Console.WriteLine(
        $"Стан до помилки: {stateBeforeFailedIssue}, " +
        $"стан після помилки: {book.IsIssued}");

    Loan secondLoan = Loan.Open(
        "L-002",
        book.Id,
        "R-002",
        new DateTime(2026, 10, 10));

    TryDo(
        "повернення раніше дати видачі",
        () => secondLoan.Close(
            new DateTime(2026, 10, 5)));

    secondLoan.Close(
        new DateTime(2026, 10, 15));

    TryDo(
        "повторне закриття видачі",
        () => secondLoan.Close(
            new DateTime(2026, 10, 20)));
}


static void TryDo(
    string title,
    Action action)
{
    try
    {
        action();

        Console.WriteLine(
            $" {title}: виняток НЕ спрацював");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $" {title}: " +
            $"{ex.GetType().Name} — " +
            $"{ex.Message}");
    }
}