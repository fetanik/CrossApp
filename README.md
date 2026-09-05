# CrossApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: Бібліотека.
Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам і їх повернень.

## Запуск

dotnet build

dotnet run --project src/Cli

## Середовище

.NET SDK 8.0.424  
Windows 11 x64  
RID: win-x64


## Додаткове завдання

### Self-contained publish

win-x64: 70,49 MB  
linux-x64: 70,51 MB

### JSON

Запуск програми у форматі JSON:

dotnet run --project src/Cli -- --json

### Docker

Програму було запущено в Linux-контейнері Docker.

Локальний OSDescription: Microsoft Windows 10.0.26200  
Docker OSDescription: Debian GNU/Linux 12 (bookworm)