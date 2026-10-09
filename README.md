# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: **Бібліотека**.

Сутності: `Book`, `BookCopy`, `Reader`, `Loan`.

Призначення: облік видач примірників книг читачам і їх повернень.

## Структура solution

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Abstractions/
    │   ├── Domain/
    │   ├── Dto/
    │   ├── Import/
    │   ├── Services/
    │   ├── Storage/
    │   └── SampleData.cs
    └── Cli/
        ├── Program.cs
        └── StoreFactory.cs
```

Залежність між проєктами:

```text
Cli → Core
```

## Основні команди

Збірка:

```powershell
dotnet build
```

Запуск:

```powershell
dotnet run --project src/Cli
```

Self-contained publish:

```powershell
dotnet publish src/Cli -c Release -f net10.0 -r win-x64 --self-contained true
```

Framework-dependent publish:

```powershell
dotnet publish src/Cli -c Release -f net10.0 -r win-x64 --self-contained false
```

## Порівняння публікацій

| RID | Режим | Розмір | Потрібен runtime |
|---|---|---:|---|
| win-x64 | self-contained | 76,68 МБ | Ні |
| win-x64 | framework-dependent | 0,19 МБ | Так (.NET 10) |

Self-contained містить .NET Runtime, тому займає більше місця. Framework-dependent потребує встановленого .NET Runtime.

## Лабораторна робота 4 — доменна модель

Реалізовано доменні сутності `BookCopy`, `Loan`, `Reader` та бізнес-правила.

Основні інваріанти:

- обов'язкові `Id`, `ISBN`, назва книги та `ReaderId`;
- рік видання має бути більшим за 0;
- не можна повторно видати вже виданий примірник;
- не можна повернути невиданий примірник;
- дата повернення не може бути раніше дати видачі;
- один читач може мати не більше 5 відкритих видач;
- дозволені переходи станів `Open → Closed` та `Open → Cancelled`.

Основні операції:

- `BookCopy.Create()`, `Issue()`, `Return()`;
- `Loan.Open()`, `Close()`, `Cancel()`;
- мапінг між сутністю та DTO.

Для помилок використовуються `ArgumentException`, `ArgumentOutOfRangeException` та `InvalidOperationException`.

## Лабораторна робота 5 — сервісний шар і сховища

Реалізовано:

- `ILibraryStore` — контракт сховища;
- `InMemoryLibraryStore` — сховище в пам'яті;
- `FileLibraryStore` — JSON-сховище;
- `LendingService` — сервіс прикладних операцій;
- ручне Dependency Injection через конструктор.

Схема залежностей:

```text
Cli → LendingService → ILibraryStore
                      ↓
       InMemoryLibraryStore | FileLibraryStore
```

Основні операції:

- додавання книги;
- видача;
- повернення;
- список книг;
- пошук за ID.

### Запуск у пам'яті

```powershell
dotnet run --project src/Cli
```

Після завершення програми дані не зберігаються.

### Запуск із файловим сховищем

```powershell
dotnet run --project src/Cli -- --file
```

Дані зберігаються у JSON і відновлюються при наступному запуску.

## Додаткові завдання Lab05

### 1. CachingLibraryStore

Реалізовано декоратор `CachingLibraryStore`, який приймає інший `ILibraryStore` і додає кешування.

```powershell
dotnet run --project src/Cli -- --cache
```

або:

```powershell
dotnet run --project src/Cli -- --file --cache
```

### 2. Пошук через Func<BookCopy, bool>

До сховища додано:

```csharp
IReadOnlyList<BookCopy> FindCopies(
    Func<BookCopy, bool> predicate);
```

Приклад:

```csharp
copy => copy.Year < 1900
```

### 3. StoreFactory

Логіку створення сховищ винесено з `Program.cs` у:

```csharp
StoreFactory.Create(args)
```

Фабрика аналізує аргументи `--file` і `--cache` та створює потрібну конфігурацію.

## Варіанти запуску

```powershell
dotnet run --project src/Cli
dotnet run --project src/Cli -- --cache
dotnet run --project src/Cli -- --file
dotnet run --project src/Cli -- --file --cache
```

DI-контейнер `Microsoft.Extensions.DependencyInjection` не використовується.