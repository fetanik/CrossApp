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

    Console.WriteLine();

    Console.WriteLine(
        "=== Додаткове завдання 1: DTO → сутності ===");

    ImportResult<BookDto> imported =
        new(
            new List<BookDto>
            {
                new(
                    "B-101",
                    "978-617-000001-1",
                    "Коректна книга",
                    2020),

                new(
                    "B-102",
                    "",
                    "Книга без ISBN",
                    2021),

                new(
                    "B-103",
                    "978-617-000003-5",
                    "Книга з неправильним роком",
                    -10)
            },
            new List<string>
            {
                "рядок 15: помилка імпорту з тижня 3"
            });

    ImportResult<BookCopy> domainResult =
        BookDomainMapper.ToEntities(imported);

    Console.WriteLine(
        $"Створено сутностей: {domainResult.Items.Count}");

    foreach (BookCopy entity in domainResult.Items)
    {
        Console.WriteLine(
            $" {entity}");
    }

    Console.WriteLine(
        $"Помилок: {domainResult.Errors.Count}");

    foreach (string error in domainResult.Errors)
    {
        Console.WriteLine(
            $" ! {error}");
    }

    Console.WriteLine();

    Console.WriteLine(
        "=== Додаткове завдання 2: максимум 5 відкритих видач ===");

    string readerId = "R-100";

    var existingLoans = new List<Loan>
    {
        Loan.Open(
            "L-101",
            "B-201",
            readerId,
            new DateTime(2026, 10, 1)),

        Loan.Open(
            "L-102",
            "B-202",
            readerId,
            new DateTime(2026, 10, 2)),

        Loan.Open(
            "L-103",
            "B-203",
            readerId,
            new DateTime(2026, 10, 3)),

        Loan.Open(
            "L-104",
            "B-204",
            readerId,
            new DateTime(2026, 10, 4)),

        Loan.Open(
            "L-105",
            "B-205",
            readerId,
            new DateTime(2026, 10, 5))
    };

    Console.WriteLine(
        $"Читач {readerId} має " +
        $"{existingLoans.Count} відкритих видач.");

    BookCopy sixthBook = BookCopy.Create(
        "B-206",
        "978-617-000206-0",
        "Шоста книга",
        2022);

    TryDo(
        "спроба створити шосту відкриту видачу",
        () => LoanService.IssueBook(
            "L-106",
            sixthBook,
            readerId,
            new DateTime(2026, 10, 6),
            existingLoans));

    Console.WriteLine(
        $"Стан книги після відмови: {sixthBook}");

    Console.WriteLine();

    Console.WriteLine(
        "=== Додаткове завдання 3: стани Loan ===");

    Loan statusLoan = Loan.Open(
        "L-301",
        "B-301",
        "R-301",
        new DateTime(2026, 10, 1));

    Console.WriteLine(
        $"Початковий стан: {statusLoan.Status}");

    statusLoan.Close(
        new DateTime(2026, 10, 10));

    Console.WriteLine(
        $"Після Close: {statusLoan.Status}");

    TryDo(
        "спроба скасувати вже закриту видачу",
        () => statusLoan.Cancel());

    Loan cancelledLoan = Loan.Open(
        "L-302",
        "B-302",
        "R-302",
        new DateTime(2026, 10, 2));

    Console.WriteLine(
        $"Початковий стан другої видачі: " +
        $"{cancelledLoan.Status}");

    cancelledLoan.Cancel();

    Console.WriteLine(
        $"Після Cancel: {cancelledLoan.Status}");

    TryDo(
        "спроба закрити скасовану видачу",
        () => cancelledLoan.Close(
            new DateTime(2026, 10, 12)));
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