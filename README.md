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
├── data/
├── tests/
│   └── Core.Tests/
└── src/
    ├── Core/
    │   ├── Abstractions/
    │   ├── Domain/
    │   ├── Dto/
    │   ├── Import/
    │   ├── Reports/
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
tests/Core.Tests → Core
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

Починаючи з лабораторної №8, порушення бізнес-правил (повторна видача, повернення невиданого примірника, заборонений перехід стану видачі, ліміт відкритих видач, дубль ідентифікатора) повідомляються через `DomainException`. Некоректні аргументи й надалі дають `ArgumentException`. Пошкоджений JSON сховища та синтаксична помилка JSON залишаються технічними винятками.

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


## Лабораторна робота №6

Тема: Доменні події, делегати Action/Func, лямбди.

Реалізовано:
- Доменні події BookAdded, CopyIssued, CopyReturned.
- EventHandler<LibraryChangedEventArgs>.
- Два підписники: логер та лічильник подій.
- Лямбда-вираз із замиканням.
- Підписку та відписку через += і -=.
- Запис повідомлень у logs/app.log.
- Оброблення винятків без генерації подій.
- Пошук через Func<BookCopy, bool>.

### Додаткові завдання

1. Реалізовано INotificationSink з ConsoleSink і FileSink.
   Використано Dependency Injection через конструктор.

2. Реалізовано Action<string> для повідомлень
   про пропущені рядки під час CSV-імпорту.

3. Розглянуто особливості та небезпеки async void
   в асинхронних обробниках подій.

### Демонстрація

Основна частина:

dotnet run --project src/Cli --framework net10.0

INotificationSink:

dotnet run --project src/Cli --framework net10.0 -- --sink-demo

Action<string>:

dotnet run --project src/Cli --framework net10.0 -- --import-demo

## Лабораторна робота №7

Тема: звіти через LINQ to Objects — фільтри, проєкції, групування, топ-N і агрегати.

Обчислення зібрані в `ReportService`. Методи приймають `IEnumerable<T>` і повертають невеликі record-DTO, а не доменні сутності. Консоль лише викликає сервіс і друкує таблицю.

У моделі немає `DueDate`. Для прострочених видач прийнято правило звіту: книгу треба повернути протягом 14 днів від `IssuedOn`. Зберігання `Loan` через це не змінювалось. У топ читачів входять відкриті й уже повернені видачі. Скасовані видачі не рахуються. Однаковий рахунок упорядковується за `ReaderId`.

### Звіти

1. `TopReaders` — топ-5 читачів за кількістю видач: `Where`, `GroupBy`, `Select`, а далі метод `TopBy` (`OrderByDescending`, `ThenBy`, `Take`).
2. `OverdueLoans` — ще не повернені видачі, у яких дата перевірки пізніша за `IssuedOn` плюс 14 днів: `Where`, `OrderBy`, `Select`. Дата перевірки передається в метод.
3. `ByYear` — кількість примірників у кожному році видання: `GroupBy`, `Select`, `Count`, `OrderBy`.
4. `CopyTotals` — усього примірників, кількість різних ISBN, середня кількість примірників на видання (`double`) і максимум примірників одного ISBN. Порожня колекція дає нулі.

### Команди

```powershell
dotnet run --project src/Cli -- report-top
dotnet run --project src/Cli -- report-overdue
dotnet run --project src/Cli -- report-groups
dotnet run --project src/Cli -- report-sum
dotnet run --project src/Cli -- report-deferred
```

Прапорці `--file` і `--cache` працюють як раніше. Назвою звіту вважається лише одна з команд вище, тож шлях до файлу не потрапляє у звіт.

### Відкладене виконання

Запит `Where` не проходить список у момент створення: він лише запам'ятовує умову і джерело. `Count()` виконує запит зараз, тому після додавання у той самий список елемента, який підходить під умову, другий `Count()` бачить уже більшу кількість. `ToList()` одразу копіює знайдені елементи в окремий список. Подальше очищення початкового списку змінює результат нового виконання запиту, але не змінює вже зроблену копію.

## Додаткові завдання лабораторної №7

### 1. GroupJoin двох колекцій

`CopyLoans` з'єднує примірники з видачами: `BookCopy.Id` дорівнює `Loan.BookCopyId`. Скасовані видачі не рахуються. Примірник без жодної видачі теж є в таблиці, але з нулями.

Оператори: `GroupJoin`, `Where`, `Count`, `OrderBy`.

```powershell
dotnet run --project src/Cli --framework net10.0 -- report-join
```

Очікується таблиця з ідентифікатором примірника, ISBN, загальною кількістю видач і кількістю відкритих видач.

### 2. Метод розширення TopBy

`TopBy<T>` приймає колекцію, показник типу `int`, кількість `n` і другий ключ сортування. Елементи йдуть за спаданням показника, а за рівного значення — за другим ключем. Порожня колекція або `n <= 0` дають порожній список.

Метод використано у `TopReaders` і в новому `TopBorrowedCopies`. Порядок `report-top` лишився тим самим: спочатку більша кількість видач, потім `ReaderId`.

Оператори всередині `TopBy`: `OrderByDescending`, `ThenBy`, `Take`.

```powershell
dotnet run --project src/Cli --framework net10.0 -- report-top
dotnet run --project src/Cli --framework net10.0 -- report-top-copies
```

`report-top-copies` показує п'ять примірників із найбільшою кількістю фактичних видач. Однаковий рахунок розходиться за ідентифікатором примірника.

### 3. Query syntax

`ByYear` залишився у method syntax. `ByYearQuerySyntax` робить те саме через `from`, `group by`, `into`, `orderby` і `select`. Порівняння йде за значеннями `YearGroupSummary`, а не за посиланням на список.

```powershell
dotnet run --project src/Cli --framework net10.0 -- report-query
```

Команда друкує групи за роком і повідомляє, чи збігаються обидві реалізації.

Method syntax — це ланцюжок викликів методів: `GroupBy`, `Select`, `OrderBy`. Query syntax — запис, схожий на SQL: джерело, групування і проєкція читаються зверху вниз. Для групування за роком query syntax наочніше показує, що саме є групою і що з неї вибирається. Method syntax зручніший, коли потрібні `StringComparer`, `Take` або власний метод на кшталт `TopBy`, бо ці параметри прямо стоять у виклику.

### 4. Stopwatch на 100 000 записах

`LinqBenchmark` створює 100 000 детермінованих записів `ReadingSample` у пам'яті і не записує їх у сховище бібліотеки. Після одного прогріву кожного варіанта п'ять разів вимірюється лише виконання запиту.

Варіант A спочатку матеріалізує `Where(...).ToList()`, а вже потім сортує і проєктує. Варіант B виконує `Where`, `OrderByDescending`, `Select` і один `ToList()` без проміжного списку. Умова, сортування і проєкція однакові. Час генерації даних у вимір не входить.

```powershell
dotnet run --project src/Cli --framework net10.0 -- report-benchmark
```

Команда показує кількість вхідних записів, кількість результатів, час кожного із п'яти запусків, медіану обох варіантів і чи збіглися результати. Який варіант швидший, видно з цих вимірів; між запусками числа можуть трохи відрізнятися.

Під час перевірки на цій машині обидва варіанти повернули однакові 40 000 рядків зі 100 000. П'ять часів варіанта A: 7, 9, 15, 13 і 15 мс, медіана 13 мс. Варіант B: 16, 15, 16, 13 і 13 мс, медіана 15 мс. За медіаною цього запуску швидший варіант A, хоча окремі виміри перетинаються, тому різниця невелика.

## Лабораторна робота №8

Тема: `async`/`await`, скасування, `DomainException` і unit-тести Core.

Реалізовано:

- xUnit-проєкт `tests/Core.Tests`. Він посилається лише на `Core`.
- `DomainException` для порушення бізнес-правил. Після такої помилки сховище не змінюється і подія `Changed` не піднімається.
- Строгий асинхронний імпорт каталогу `JsonCatalogImporter.LoadAsync`: `FileStream` з `useAsync: true`, `JsonDocument.ParseAsync`, перевірка `CancellationToken` у циклі. Старий `BookJsonImporter` як і раніше пропускає некоректні рядки й лишається для попередніх сценаріїв.
- `LendingService.ImportAsync`: імпортер передається параметром, уже наявні `Id` пропускаються, для кожного нового примірника піднімається `Changed`. Повторний імпорт не створює дублікатів.
- CLI: `Ctrl+C`, `--timeout-ms` через зв'язані токени, окремі повідомлення для скасування, доменної помилки, відсутнього файлу і технічної помилки JSON.
- Усі попередні команди лабораторних №6 і №7 збережені.

### Запуск

Збірка і тести з кореня `CrossApp`:

```powershell
dotnet build
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Звичайний сценарій лабораторних №5–7, як і раніше:

```powershell
dotnet run --project src/Cli --framework net10.0
dotnet run --project src/Cli --framework net10.0 -- report-top
```

### Асинхронний імпорт

Команду потрібно запускати з кореня репозиторія, щоб відносний шлях `data\...` знайшовся.

```powershell
dotnet run --project src/Cli --framework net10.0 -- --import data/demo-import.json
```

Очікується прогрес від 0% до 100%, рядки `Changed: BookAdded` для `IMP-01`…`IMP-04`, потім:

```text
Імпортовано записів: 4
```

Одразу після цього команда повторює імпорт того самого файлу. Другий результат:

```text
Імпортовано записів: 0
```

Прапорці `--file` і `--cache` можна додати як раніше. Тоді нові примірники потрапляють у вибране сховище.

### Скасування

Таймаут і `Ctrl+C` з'єднані через `CancellationTokenSource.CreateLinkedTokenSource`. Щоб скасування встигло спрацювати до кінця читання короткого файлу, перед імпортом є керована пауза `--delay-ms`. Це не заміна файлового вводу-виводу: сам каталог далі читається асинхронно.

Таймаут:

```powershell
dotnet run --project src/Cli --framework net10.0 -- --import data/demo-import.json --timeout-ms 200 --delay-ms 5000
```

Очікується:

```text
Імпорт скасовано.
Причина: таймаут.
```

`Ctrl+C` під час очікування:

```powershell
dotnet run --project src/Cli --framework net10.0 -- --import data/demo-import.json --delay-ms 30000
```

Після `Ctrl+C`:

```text
Імпорт скасовано.
Причина: Ctrl+C.
```

Процес не показує необроблений стектрейс.

### Некоректні доменні дані

`data/demo-invalid.json` є синтаксично правильним JSON. Другий запис має порожній ISBN.

```powershell
dotnet run --project src/Cli --framework net10.0 -- --import data/demo-invalid.json
```

Очікується:

```text
Дані відхилено: Запис 2: ...
```

Жоден запис із цього файлу не додається: `LoadAsync` перевіряє весь каталог до запису в сховище. Відсутній файл:

```powershell
dotnet run --project src/Cli --framework net10.0 -- --import data/no-such-file.json
```

```text
Файл не знайдено: ...
```

### Подія Changed

Під час `--import` кожне успішне додавання друкує `Changed: BookAdded`. Повторний імпорт і відхилений каталог нових подій не створюють. Сценарій видачі й повернення лабораторної №6 лишився на запуску без аргументів імпорту: повторна видача дає `DomainException`, а лічильник подій не зростає.

### Додаткові завдання

1. Потоковий імпорт: `JsonCatalogImporter.ReadAsync` повертає `IAsyncEnumerable<BookCopy>`, читає масив через `DeserializeAsyncEnumerable` і перевіряє токен з `[EnumeratorCancellation]`. Загальна кількість записів під час читання невідома, тому CLI показує лічильник `Оброблено записів: N`, а не вигаданий відсоток. Відсоток 0–100 лишається в `ImportAsync`, де список уже прочитаний.

```powershell
dotnet run --project src/Cli --framework net10.0 -- --import-stream data/demo-import.json
```

2. Зв'язані токени: `LinkedCancellation.Link` викликає `CreateLinkedTokenSource(userToken, timeoutToken)`. Окремий тест скасовує лише токен таймауту і перевіряє, що користувацький токен лишається активним.
3. Прогрес: `ImportAsync` приймає `IProgress<int>` і повідомляє відсоток від 0 до 100. Для потоку прогрес — це кількість уже прочитаних записів.
4. Покриття: пакет `coverlet.collector`. Після прогону Coverlet намагається повернути оригінальний `Core.dll` поверх інструментованої копії. На Windows це не вдається, доки тестовий хост тримає файл відображеним у пам'ять (`user-mapped section open`), і файл `coverage.cobertura.xml` не з'являється. `tests/Core.Tests/coverlet.runsettings` вимикає це відновлення (`DisableManagedInstrumentationRestore`) і лишає у звіті лише збірку `Core`. Тестовий проєкт підхоплює цей файл сам. Збірки колектора копіюються в каталог тестів: інакше SDK 10 підставляє лише теку пакета `coverlet.collector`, і xUnit не стартує.

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

Звіт лежить у `tests/Core.Tests/TestResults/<guid>/coverage.cobertura.xml`. Останній прогін: рядки **26,23%** (403 із 1536, `line-rate="0.2623"`), гілки **23,82%** (76 із 319, `branch-rate="0.2382"`). `Cli` у звіт не входить, бо тести посилаються лише на `Core`. Не покриті сховища файлів і кешу, старі імпортери CSV/JSON, які пропускають помилкові рядки, `SampleData` і частина звітів.

Потоковий імпорт може встигнути додати вже прочитані коректні записи до помилкового. Строгий `--import` так не робить: помилка будь-якого запису відхиляє файл цілком.
