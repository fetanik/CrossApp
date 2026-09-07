# CrossApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: Бібліотека.
Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам і їх повернень.

## Запуск

dotnet build

dotnet run --project src/Cli

## Середовище

.NET SDK 10.0.400  
Windows 11 x64  
RID: win-x64

## Додаткове завдання

### Self-contained publish

win-x64: 76,66 MB  
linux-x64: 78,79 MB

### JSON

Запуск програми у форматі JSON:

dotnet run --project src/Cli -- --json