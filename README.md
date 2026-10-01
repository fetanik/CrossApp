# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Бібліотека.

Сутності: Book, BookCopy, Reader, Loan.

Призначення: облік видач примірників книг читачам і їх повернень.

## Структура solution

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
├── data/
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs
    │   ├── Dto/
    │   ├── Import/
    │   └── Domain/
    │       ├── BookCopy.cs
    │       └── Loan.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

Залежність між проєктами:

```text
Cli → Core
```

У Core використовуються каталоги:

```text
Core/Dto/
Core/Import/
Core/Domain/
```

У наступних роботах буде додано:
```text
Core/Storage/
```

## Команди

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

Self-contained містить .NET Runtime, тому займає більше місця.
Framework-dependent потребує встановленого .NET Runtime.

## Лабораторна робота 4 — доменна модель

У лабораторній роботі створено доменні сутності `BookCopy` та `Loan`.

### Інваріанти

- `Id` книги не може бути порожнім.
- `ISBN` не може бути порожнім.
- Назва книги не може бути порожньою.
- Рік видання має бути більшим за 0.
- Не можна повторно видати вже видану книгу.
- Не можна повернути книгу, яка не була видана.
- `Id` видачі не може бути порожнім.
- `ReaderId` не може бути порожнім.
- Дата повернення не може бути раніше дати видачі.
- Не можна повторно закрити вже закриту видачу.

### Доменні операції

Для `BookCopy` реалізовано:

- `Create(...)` — створення примірника книги;
- `Issue()` — видача книги;
- `Return()` — повернення книги;
- `ToDto()` — перетворення сутності в `BookDto`;
- `FromDto(...)` — відновлення сутності з `BookDto`.

Для `Loan` реалізовано:

- `Open(...)` — відкриття видачі;
- `Close(...)` — закриття видачі.

### Обробка помилок

Для некоректних вхідних аргументів використовуються:

- `ArgumentException`;
- `ArgumentOutOfRangeException`.

Для операцій, які неможливо виконати через поточний стан об'єкта, використовується:

- `InvalidOperationException`.

### Запуск лабораторної роботи 4

```powershell
dotnet run --project src/Cli
```