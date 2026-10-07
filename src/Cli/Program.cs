using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;

bool useFile =
    args.Any(
        arg => arg.Equals(
            "--file",
            StringComparison.OrdinalIgnoreCase));

string dataPath =
    Path.Combine(
        AppContext.BaseDirectory,
        "data",
        "library.json");

ILibraryStore store =
    useFile
        ? new FileLibraryStore(dataPath)
        : new InMemoryLibraryStore(
            SampleData.Copies());

var service =
    new LendingService(store);

Console.WriteLine(
    $"Сховище: {store.GetType().Name}");

if (useFile)
{
    Console.WriteLine(
        $"Файл: {dataPath}");
}

Console.WriteLine(
    $"Записів до сценарію: " +
    $"{service.AllBooks().Count}");

Console.WriteLine();

try
{
    Console.WriteLine(
        "=== Додавання книги ===");

    var book =
        service.AddBook(
            "9786179999999",
            "Демонстраційна книга",
            2026,
            "Тестовий автор");

    Console.WriteLine(book);

    Console.WriteLine();
    Console.WriteLine(
        "=== Видача книги ===");

    var loan =
        service.IssueCopy(
            book.Id,
            "R-001");

    Console.WriteLine(loan);

    Console.WriteLine();
    Console.WriteLine(
        "=== Повернення книги ===");

    service.ReturnCopy(
        loan.Id);

    Console.WriteLine(
        service.FindBook(book.Id));

    Console.WriteLine();
    Console.WriteLine(
        "=== Список книг ===");

    foreach (var item
             in service.AllBooks().Take(5))
    {
        Console.WriteLine(item);
    }

    Console.WriteLine();
    Console.WriteLine(
        "=== Сценарій відмови ===");

    try
    {
        service.ReturnCopy(
            loan.Id);
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"{ex.GetType().Name}: " +
            ex.Message);
    }
}
catch (Exception ex)
{
    Console.WriteLine(
        $"Помилка: {ex.Message}");
}