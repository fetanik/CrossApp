using Core.Services;

StoreConfiguration configuration =
    StoreFactory.Create(args);

var service =
    new LendingService(
        configuration.Store);

Console.WriteLine(
    $"Сховище: {configuration.BaseStoreName}");

Console.WriteLine(
    $"Кеш: " +
    $"{(configuration.CacheEnabled ? "увімкнено" : "вимкнено")}");

if (configuration.FilePath is not null)
{
    Console.WriteLine(
        $"Файл: {configuration.FilePath}");
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
        "=== Пошук через Func<BookCopy, bool> ===");

    var oldBooks =
        service.SearchBooks(
            copy => copy.Year < 1900);

    Console.WriteLine(
        "Книги, видані до 1900 року:");

    foreach (var item in oldBooks)
    {
        Console.WriteLine(
            $"{item.Title} ({item.Year})");
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