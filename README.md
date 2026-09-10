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
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

Залежність між проєктами:

```text
Cli → Core
```

У наступних роботах у Core плануються каталоги:

```text
Core/Dto/
Core/Domain/
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