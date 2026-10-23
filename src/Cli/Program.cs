using Core.Events;
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

// Lab06: шлях до журналу формується у Cli, а не в Core.
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

List<string> invocationOrder = new();
int changes = 0;

// Перший підписник: виводить подію в консоль і зберігає в app.log.
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

// Другий підписник: лямбда із замиканням над changes.
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

    // Спочатку перевіряємо інваріант: повторно закрити позику не можна.
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

    // Відписуємо лише логер. Лямбда-лічильник продовжує отримувати події.
    Console.WriteLine("=== Відписка першого підписника ===");
    service.Changed -= OnLibraryChanged;

    int linesBefore = File.ReadAllLines(logPath).Length;
    var loanAfterUnsubscribe = service.IssueCopy(book.Id, "R-002");
    int linesAfter = File.ReadAllLines(logPath).Length;

    Console.WriteLine($"Нову видачу створено: {loanAfterUnsubscribe.Id}");
    Console.WriteLine($"Подій отримано: {changes}");
    Console.WriteLine($"Рядків журналу до/після: {linesBefore}/{linesAfter}");
    Console.WriteLine();

    // Невдала повторна видача не створює події навіть для лічильника.
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
