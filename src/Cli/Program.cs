using Cli.Notifications;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Reports;
using Core.Services;

StoreConfiguration configuration = StoreFactory.Create(args);
var service = new LendingService(configuration.Store);

Console.WriteLine($"Сховище: {configuration.BaseStoreName}");
Console.WriteLine($"Кеш: {(configuration.CacheEnabled ? "увімкнено" : "вимкнено")}");

if (configuration.FilePath is not null)
{
    Console.WriteLine($"Файл: {configuration.FilePath}");
}

Console.WriteLine($"Записів до сценарію: {service.AllBooks().Count}");
Console.WriteLine();

string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);


if (args.Any(arg => arg.Equals("--import-demo", StringComparison.OrdinalIgnoreCase)))
{
    string csvPath = Path.Combine(Environment.CurrentDirectory, "data", "sample.csv");
    Console.WriteLine("=== Додаткове завдання 2: Action<string> ===");
    Console.WriteLine($"Файл CSV: {csvPath}");

    if (!File.Exists(csvPath))
    {
        Console.WriteLine("Файл не знайдено. Запусти команду з кореня проєкту CrossApp.");
        return;
    }

    int skipped = 0;
    var imported = BookCsvImporter.Load(csvPath, message =>
    {
        skipped++;
        Console.WriteLine($"Пропущено: {message}");
    });

    Console.WriteLine($"Прийнято: {imported.Items.Count}");
    Console.WriteLine($"Пропущено: {skipped}");
    return;
}


if (args.Any(arg => arg.Equals("--sink-demo", StringComparison.OrdinalIgnoreCase)))
{
    string sinkPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");

    INotificationSink notificationSink = new CompositeNotificationSink(
        new ConsoleSink(),
        new FileSink(sinkPath));


    var sinkService = new LendingService(configuration.Store, notificationSink);

    Console.WriteLine("=== Додаткове завдання 1: INotificationSink / DI ===");
    var demoBook = sinkService.AddBook(
        "9786179999999", "Книга для перевірки DI", 2026, "Тестовий автор");
    var demoLoan = sinkService.IssueCopy(demoBook.Id, "R-DI");
    sinkService.ReturnCopy(demoLoan.Id);

    Console.WriteLine($"Події записані також до: {sinkPath}");
    return;
}

if (TryRunReport(args, service))
{
    return;
}

List<string> invocationOrder = new();
int changes = 0;


void OnLibraryChanged(object? sender, LibraryChangedEventArgs e)
{
    invocationOrder.Add("логер");

    string line = $"{e.At:HH:mm:ss} {e.Kind,-12} CopyId={e.CopyId}";

    if (e.ReaderId is not null)
    {
        line += $" ReaderId={e.ReaderId}";
    }

    if (e.IssuedOn is DateTime issuedOn)
    {
        line += $" дата видачі {issuedOn:dd.MM.yyyy}";
    }

    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnLibraryChanged;

EventHandler<LibraryChangedEventArgs> counterHandler = (_, _) =>
{
    invocationOrder.Add("лічильник");
    changes++;
};

service.Changed += counterHandler;

try
{
    Console.WriteLine("=== Додавання книги ===");
    var book = service.AddBook(
        "9786179999999",
        "Демонстраційна книга",
        2026,
        "Тестовий автор");
    Console.WriteLine(book);
    Console.WriteLine();

    Console.WriteLine("=== Видача книги ===");
    var loan = service.IssueCopy(book.Id, "R-001");
    Console.WriteLine(loan);
    Console.WriteLine();

    Console.WriteLine("=== Повернення книги ===");
    service.ReturnCopy(loan.Id);
    Console.WriteLine(service.FindBook(book.Id));
    Console.WriteLine();

    Console.WriteLine("=== Лабораторна 6: два підписники ===");
    Console.WriteLine($"Подій отримано: {changes}");
    Console.WriteLine("Порядок: " + string.Join(" -> ", invocationOrder));
    Console.WriteLine($"Шлях до журналу: {logPath}");
    Console.WriteLine();

    Console.WriteLine("=== Сценарій відмови (до відписки) ===");
    int beforeFailure = changes;
    try
    {
        service.ReturnCopy(loan.Id);
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    }
    Console.WriteLine($"Подій після помилки: {changes} (було {beforeFailure})");
    Console.WriteLine();

    Console.WriteLine("=== Список книг ===");
    foreach (var item in service.AllBooks().Take(5))
    {
        Console.WriteLine(item);
    }
    Console.WriteLine();

    Console.WriteLine("=== Пошук через Func<BookCopy, bool> ===");
    var oldBooks = service.SearchBooks(copy => copy.Year < 1900);
    Console.WriteLine("Книги, видані до 1900 року:");
    foreach (var item in oldBooks)
    {
        Console.WriteLine($"{item.Title} ({item.Year})");
    }

    var availableBooks = service.SearchBooks(copy => !copy.IsIssued);
    Console.WriteLine($"Доступних примірників: {availableBooks.Count}");
    Console.WriteLine();

    Console.WriteLine("=== Відписка першого підписника ===");
    service.Changed -= OnLibraryChanged;

    int linesBefore = File.ReadAllLines(logPath).Length;
    var loanAfterUnsubscribe = service.IssueCopy(book.Id, "R-002");
    int linesAfter = File.ReadAllLines(logPath).Length;

    Console.WriteLine($"Нову видачу створено: {loanAfterUnsubscribe.Id}");
    Console.WriteLine($"Подій отримано: {changes}");
    Console.WriteLine($"Рядків журналу до/після: {linesBefore}/{linesAfter}");
    Console.WriteLine();

    Console.WriteLine("=== Помилка без нової події ===");
    int beforeSecondFailure = changes;
    try
    {
        service.IssueCopy(book.Id, "R-003");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    }
    Console.WriteLine($"Подій після помилки: {changes} (було {beforeSecondFailure})");
}
catch (Exception ex)
{
    Console.WriteLine($"Помилка: {ex.GetType().Name}: {ex.Message}");
}
finally
{
    service.Changed -= OnLibraryChanged;
    service.Changed -= counterHandler;
}

static bool TryRunReport(string[] args, LendingService service)
{
    string? command = null;

    foreach (string arg in args)
    {
        if (arg.Equals("report-top", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-overdue", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-groups", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-sum", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-deferred", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-join", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-top-copies", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-query", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-benchmark", StringComparison.OrdinalIgnoreCase))
        {
            command = arg;
            break;
        }
    }

    if (command is null)
    {
        return false;
    }

    switch (command.ToLowerInvariant())
    {
        case "report-top":
            PrintTopReaders(service);
            break;
        case "report-overdue":
            PrintOverdue(service);
            break;
        case "report-groups":
            PrintGroups(service);
            break;
        case "report-sum":
            PrintTotals(service);
            break;
        case "report-deferred":
            PrintDeferred();
            break;
        case "report-join":
            PrintCopyLoans(service);
            break;
        case "report-top-copies":
            PrintTopCopies(service);
            break;
        case "report-query":
            PrintYearQuery(service);
            break;
        case "report-benchmark":
            PrintBenchmark();
            break;
    }

    return true;
}

static void PrintTopReaders(LendingService service)
{
    Console.WriteLine("=== Топ-5 читачів за кількістю видач ===");

    IReadOnlyList<TopReaderLine> lines =
        ReportService.TopReaders(service.AllLoans());

    if (lines.Count == 0)
    {
        Console.WriteLine("Видач немає.");
        return;
    }

    Console.WriteLine($"{"Читач",-12} {"Видач",6}");
    foreach (TopReaderLine line in lines)
    {
        Console.WriteLine($"{line.ReaderId,-12} {line.LoanCount,6}");
    }
}

static void PrintOverdue(LendingService service)
{
    DateTime asOf = DateTime.Today;

    Console.WriteLine(
        $"=== Прострочені видачі на {asOf:yyyy-MM-dd} " +
        $"(строк {ReportService.LoanPeriodDays} днів) ===");

    IReadOnlyList<OverdueLoanLine> lines =
        ReportService.OverdueLoans(service.AllLoans(), asOf);

    if (lines.Count == 0)
    {
        Console.WriteLine("Прострочених видач немає.");
        return;
    }

    Console.WriteLine(
        $"{"Видача",-8} {"Примірник",-10} {"Читач",-8} " +
        $"{"Видано",-12} {"Строк до",-12} {"Днів",5}");

    foreach (OverdueLoanLine line in lines)
    {
        Console.WriteLine(
            $"{line.LoanId,-8} {line.BookCopyId,-10} {line.ReaderId,-8} " +
            $"{line.IssuedOn,-12:yyyy-MM-dd} {line.DueOn,-12:yyyy-MM-dd} " +
            $"{line.DaysOverdue,5}");
    }
}

static void PrintGroups(LendingService service)
{
    Console.WriteLine("=== Примірники за роком видання ===");

    IReadOnlyList<YearGroupSummary> lines =
        ReportService.ByYear(service.AllBooks());

    if (lines.Count == 0)
    {
        Console.WriteLine("Примірників немає.");
        return;
    }

    Console.WriteLine($"{"Рік",-8} {"Примірників",12}");
    foreach (YearGroupSummary line in lines)
    {
        Console.WriteLine($"{line.Year,-8} {line.CopyCount,12}");
    }
}

static void PrintTotals(LendingService service)
{
    Console.WriteLine("=== Статистика примірників ===");

    CopyStatistics totals =
        ReportService.CopyTotals(service.AllBooks());

    if (totals.TotalCopies == 0)
    {
        Console.WriteLine("Примірників немає.");
    }

    Console.WriteLine($"Усього примірників:        {totals.TotalCopies}");
    Console.WriteLine($"Різних видань (ISBN):      {totals.DistinctEditions}");
    Console.WriteLine(
        $"Середня к-сть на видання: {totals.AverageCopiesPerEdition:0.##}");
    Console.WriteLine(
        $"Максимум примірників ISBN: {totals.MaxCopiesOfOneEdition}");
}

static void PrintDeferred()
{
    Console.WriteLine("=== Відкладене виконання LINQ ===");

    DeferredExecutionReport report = DeferredExecutionDemo.Run();

    Console.WriteLine(
        $"Count() до додавання:     {report.CountBeforeAdd}");
    Console.WriteLine(
        $"Count() після додавання:  {report.CountAfterAdd}");
    Console.WriteLine(
        $"Знімок ToList():          {report.SnapshotCount}");
    Console.WriteLine(
        $"Count() після Clear():    {report.CountAfterClear}");
    Console.WriteLine(
        $"Знімок після Clear():     {report.SnapshotCountAfterClear}");
    Console.WriteLine(
        "Запит виконався знову після зміни списку, " +
        "а знімок ToList() залишився незмінним.");
}

static void PrintCopyLoans(LendingService service)
{
    Console.WriteLine("=== Видачі кожного примірника ===");

    IReadOnlyList<CopyLoanSummary> lines =
        ReportService.CopyLoans(service.AllBooks(), service.AllLoans());

    if (lines.Count == 0)
    {
        Console.WriteLine("Примірників немає.");
        return;
    }

    Console.WriteLine(
        $"{"Примірник",-10} {"ISBN",-16} {"Видач",6} {"Відкритих",10}");

    foreach (CopyLoanSummary line in lines)
    {
        Console.WriteLine(
            $"{line.CopyId,-10} {line.Isbn,-16} {line.LoanCount,6} {line.OpenLoanCount,10}");
    }
}

static void PrintTopCopies(LendingService service)
{
    Console.WriteLine("=== Топ-5 примірників за кількістю видач ===");

    IReadOnlyList<TopCopyLine> lines =
        ReportService.TopBorrowedCopies(
            service.AllBooks(),
            service.AllLoans());

    if (lines.Count == 0)
    {
        Console.WriteLine("Примірників немає.");
        return;
    }

    Console.WriteLine(
        $"{"Примірник",-10} {"ISBN",-16} {"Видач",6}");

    foreach (TopCopyLine line in lines)
    {
        Console.WriteLine(
            $"{line.CopyId,-10} {line.Isbn,-16} {line.LoanCount,6}");
    }
}

static void PrintYearQuery(LendingService service)
{
    Console.WriteLine("=== Групування за роком: query syntax ===");

    IReadOnlyList<BookCopy> copies = service.AllBooks();
    IReadOnlyList<YearGroupSummary> methodSyntax =
        ReportService.ByYear(copies);
    IReadOnlyList<YearGroupSummary> querySyntax =
        ReportService.ByYearQuerySyntax(copies);

    if (querySyntax.Count == 0)
    {
        Console.WriteLine("Примірників немає.");
    }
    else
    {
        Console.WriteLine($"{"Рік",-8} {"Примірників",12}");
        foreach (YearGroupSummary line in querySyntax)
        {
            Console.WriteLine($"{line.Year,-8} {line.CopyCount,12}");
        }
    }

    bool same = ReportService.YearGroupsMatch(methodSyntax, querySyntax);
    Console.WriteLine(
        same
            ? "Method syntax і query syntax дають однакові значення."
            : "Method syntax і query syntax дають різні значення.");
}

static void PrintBenchmark()
{
    Console.WriteLine("=== Порівняння двох LINQ-запитів ===");

    BenchmarkReport report = LinqBenchmark.Run();

    Console.WriteLine($"Вхідних записів: {report.InputCount}");
    Console.WriteLine($"Результатів:     {report.ResultCount}");
    Console.WriteLine(
        "Варіант A, мс:   " + string.Join(", ", report.VariantAMilliseconds));
    Console.WriteLine(
        "Варіант B, мс:   " + string.Join(", ", report.VariantBMilliseconds));
    Console.WriteLine(
        $"Медіана A, мс:   {report.VariantAMedianMilliseconds}");
    Console.WriteLine(
        $"Медіана B, мс:   {report.VariantBMedianMilliseconds}");
    Console.WriteLine(
        report.ResultsMatch
            ? "Результати варіантів A і B однакові."
            : "Результати варіантів A і B відрізняються.");

    if (report.VariantAMedianMilliseconds < report.VariantBMedianMilliseconds)
    {
        Console.WriteLine("За медіаною швидший варіант A.");
    }
    else if (report.VariantBMedianMilliseconds < report.VariantAMedianMilliseconds)
    {
        Console.WriteLine("За медіаною швидший варіант B.");
    }
    else
    {
        Console.WriteLine("За медіаною час варіантів однаковий.");
    }
}
