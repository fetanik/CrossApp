using System.Text.Json;
using Cli.Notifications;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Exceptions;
using Core.Import;
using Core.Reports;
using Core.Services;
using Core.Threading;

using var userCts = new CancellationTokenSource();
using var timeoutCts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    try
    {
        userCts.Cancel();
    }
    catch (ObjectDisposedException)
    {
    }
};

int? timeoutMs;
int? delayMs;
try
{
    timeoutMs = ReadOptionalNonNegative(args, "--timeout-ms");
    delayMs = ReadOptionalNonNegative(args, "--delay-ms");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
    return;
}

if (timeoutMs is int configuredTimeout)
{
    timeoutCts.CancelAfter(configuredTimeout);
}

using CancellationTokenSource linkedCts = LinkedCancellation.Link(
    userCts.Token,
    timeoutCts.Token);

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

if (HasOption(args, "--import") || HasOption(args, "--import-stream"))
{
    await RunCatalogImportAsync(
        service,
        args,
        delayMs,
        linkedCts.Token,
        userCts.Token,
        timeoutCts.Token);
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
    catch (Exception ex) when (ex is DomainException or InvalidOperationException)
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
    catch (Exception ex) when (ex is DomainException or InvalidOperationException)
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
            arg.Equals("report-benchmark", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("report-empty", StringComparison.OrdinalIgnoreCase))
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
        case "report-empty":
            PrintEmptyReports();
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

static void PrintEmptyReports()
{
    Console.WriteLine("=== Звіти на порожніх колекціях ===");

    IReadOnlyList<TopReaderLine> topReaders =
        ReportService.TopReaders(Array.Empty<Loan>());
    IReadOnlyList<OverdueLoanLine> overdueLoans =
        ReportService.OverdueLoans(Array.Empty<Loan>(), DateTime.Today);
    IReadOnlyList<YearGroupSummary> yearGroups =
        ReportService.ByYear(Array.Empty<BookCopy>());
    CopyStatistics totals =
        ReportService.CopyTotals(Array.Empty<BookCopy>());

    Console.WriteLine($"Топ читачів, рядків:       {topReaders.Count}");
    Console.WriteLine($"Прострочених видач:        {overdueLoans.Count}");
    Console.WriteLine($"Груп за роком:             {yearGroups.Count}");
    Console.WriteLine($"Усього примірників:        {totals.TotalCopies}");
    Console.WriteLine($"Різних видань (ISBN):      {totals.DistinctEditions}");
    Console.WriteLine(
        $"Середня к-сть на видання: {totals.AverageCopiesPerEdition:0.##}");
    Console.WriteLine(
        $"Максимум примірників ISBN: {totals.MaxCopiesOfOneEdition}");
    Console.WriteLine("Винятків немає: списки порожні, статистика нульова.");
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

static async Task RunCatalogImportAsync(
    LendingService service,
    string[] args,
    int? delayMs,
    CancellationToken ct,
    CancellationToken userToken,
    CancellationToken timeoutToken)
{
    bool hasImport = TryReadTextOption(args, "--import", out string? importPath);
    bool hasStream = TryReadTextOption(args, "--import-stream", out string? streamPath);

    if ((hasImport && string.IsNullOrWhiteSpace(importPath)) ||
        (hasStream && string.IsNullOrWhiteSpace(streamPath)))
    {
        Console.WriteLine(
            "Використання: --import <файл.json> або --import-stream <файл.json> " +
            "[--timeout-ms N] [--delay-ms N]");
        return;
    }

    int changes = 0;
    EventHandler<LibraryChangedEventArgs> onImported = (_, e) =>
    {
        changes++;
        Console.WriteLine($"Changed: {e.Kind} CopyId={e.CopyId}");
    };

    service.Changed += onImported;

    try
    {
        if (delayMs is int delay)
        {
            Console.WriteLine(
                $"Очікування {delay} мс (демонстрація скасування)...");
            await Task.Delay(delay, ct);
        }

        var importer = new JsonCatalogImporter();

        if (hasImport)
        {
            await ImportBatchAsync(service, importer, importPath!, ct);
            Console.WriteLine("Повторний імпорт того самого каталогу:");
            await ImportBatchAsync(service, importer, importPath!, ct);
        }

        if (hasStream)
        {
            Console.WriteLine("=== Потоковий імпорт ===");
            Console.WriteLine(
                "Відсоток не показується: під час читання потоку загальна кількість записів ще невідома.");
            IProgress<int> processed = new SyncProgress(count =>
                Console.WriteLine($"Оброблено записів: {count}"));
            int added = await service.ImportStreamAsync(
                importer,
                streamPath!,
                ct,
                processed);
            Console.WriteLine($"Імпортовано записів: {added}");
        }

        Console.WriteLine($"Подій Changed під час імпорту: {changes}");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Імпорт скасовано.");
        if (timeoutToken.IsCancellationRequested &&
            !userToken.IsCancellationRequested)
        {
            Console.WriteLine("Причина: таймаут.");
        }
        else if (userToken.IsCancellationRequested)
        {
            Console.WriteLine("Причина: Ctrl+C.");
        }
    }
    catch (DomainException ex)
    {
        Console.WriteLine($"Дані відхилено: {ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine($"Файл не знайдено: {ex.FileName ?? ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine($"Файл не знайдено: {ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Помилка читання JSON: {ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Помилка вводу-виводу: {ex.Message}");
        Environment.ExitCode = 1;
    }
    finally
    {
        service.Changed -= onImported;
    }
}

static async Task ImportBatchAsync(
    LendingService service,
    JsonCatalogImporter importer,
    string path,
    CancellationToken ct)
{
    IProgress<int> progress = new SyncProgress(percent =>
        Console.WriteLine($"Прогрес імпорту: {percent}%"));
    int added = await service.ImportAsync(importer, path, ct, progress);
    Console.WriteLine($"Імпортовано записів: {added}");
}

static bool HasOption(string[] args, string name)
{
    string prefix = name + "=";
    return args.Any(arg =>
        arg.Equals(name, StringComparison.OrdinalIgnoreCase) ||
        arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}

static bool TryReadTextOption(string[] args, string name, out string? value)
{
    value = null;
    string prefix = name + "=";

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Length || args[i + 1].StartsWith('-'))
            {
                return true;
            }

            value = args[i + 1];
            return true;
        }

        if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = args[i][prefix.Length..];
            return true;
        }
    }

    return false;
}

static int? ReadOptionalNonNegative(string[] args, string name)
{
    if (!TryReadTextOption(args, name, out string? text))
    {
        return null;
    }

    if (text is not null &&
        int.TryParse(text, out int value) &&
        value >= 0)
    {
        return value;
    }

    throw new ArgumentException(
        $"Параметр {name} потребує невід'ємного цілого числа.");
}

file sealed class SyncProgress : IProgress<int>
{
    private readonly Action<int> _handler;

    public SyncProgress(Action<int> handler)
    {
        _handler = handler;
    }

    public void Report(int value) => _handler(value);
}
