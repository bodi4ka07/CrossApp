# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам і повернень.

## Структура solution

    CrossApp/
    ├── CrossApp.slnx
    ├── README.md
    ├── .gitignore
    └── src/
        ├── Core/          # бібліотека класів (без точки входу)
        │   ├── Core.csproj
        │   └── EnvironmentInfo.cs
        └── Cli/           # консольний застосунок, залежить від Core
            ├── Cli.csproj
            └── Program.cs

## Запуск

    dotnet build
    dotnet run --project src/Cli
    dotnet run --project src/Cli -- --json

## Публікація

    dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net10.0 -o publish-sc
    dotnet publish src/Cli -c Release -r win-x64 --self-contained false -f net10.0 -o publish-fd

## Порівняння режимів публікації

| RID     | Режим               | Розмір publish | Потрібен встановлений runtime |
|---------|---------------------|----------------|--------------------------------|
| win-x64 | self-contained      | 76,68 МБ       | ні                              |
| win-x64 | framework-dependent | 0,19 МБ        | так (.NET 10)                   |

Self-contained публікація містить копію .NET runtime, тому працює на машині
без попередньо встановленого .NET, але займає значно більше місця.
Framework-dependent публікація містить лише код застосунку та залежності —
каталог маленький, але на цільовій машині обов'язково має бути встановлений
сумісний .NET 10 runtime.

## Multi-targeting

Core.csproj зібрано під `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>` —
бібліотека компілюється окремо для кожного TFM (у bin/ з'являються підкаталоги
net8.0 і net10.0).

## Середовище

.NET SDK 10.0.400, Windows, автор: Жегістовський Богдан, ФЕІ-33

## Додаткове завдання (лабораторна 1)

Розмір self-contained публікації (папка publish):
- win-x64: 76,66 МБ
- linux-x64: 78,79 МБ

Прапорець командного рядка `--json`: якщо запустити з аргументом `--json`
(`dotnet run --project src/Cli -- --json`), програма виводить ту саму
інформацію одним JSON-рядком (System.Text.Json) замість таблиці.

## Формат вхідного файлу (CSV)

Роздільник колонок — `;` (крапка з комою), кодування — UTF-8.
Колонки: `id;isbn;title;year`. Перший рядок-заголовок (`id;isbn;...`)
і порожні рядки пропускаються автоматично.
## Доменна модель (лабораторна 4)

Каталог `src/Core/Domain/` — сутності з поведінкою. Records з тижня 3
(`BookDto`, `ReaderDto`, `BookCopyDto`, `LoanDto`) лишаються DTO: вони переносять
дані, а сутність захищає правила. Зв'язок між ними — `ToDto()` / `FromDto(dto)`.

    src/Core/Domain/
    ├── BookCopy.cs   # примірник книги (виданий / на полиці)
    ├── Loan.cs       # видача примірника читачеві
    └── Reader.cs     # читач зі своїми видачами (захищена колекція)

Стан інкапсульовано: публічних сетерів немає, конструктори приватні,
створення — лише через фабричні методи `BookCopy.Create`, `Reader.Register`,
`Loan.Open`. Назовні список видач віддається як `IReadOnlyList<Loan>`
(`_loans.AsReadOnly()`).

### Інваріанти

| # | Правило | Тип винятку | Де перевіряється |
|---|---------|-------------|------------------|
| 1 | Ідентифікатор примірника / читача / видачі не порожній | `ArgumentException` | `BookCopy.Create`, `Reader.Register`, `Loan.Restore` |
| 2 | ISBN не може бути порожнім | `ArgumentException` | `BookCopy.Create` |
| 3 | Назва книги не може бути порожньою | `ArgumentException` | `BookCopy.Create` |
| 4 | Електронна адреса читача містить `@` | `ArgumentException` | `Reader.Register` |
| 5 | Виданий примірник не можна видати повторно | `InvalidOperationException` | `BookCopy.Issue` |
| 6 | Невиданий примірник не можна повернути | `InvalidOperationException` | `BookCopy.Return` |
| 7 | Дата повернення не раніше дати видачі | `ArgumentOutOfRangeException` | `Loan.Close`, `Loan.Restore` |
| 8 | Закриту видачу не можна закрити вдруге | `InvalidOperationException` | `Loan.Close` |
| 9 | Повертати можна лише той примірник, на який оформлено видачу | `InvalidOperationException` | `Loan.Close` |
| 10 | Закрити чужу видачу неможливо | `InvalidOperationException` | `Reader.ReturnLoan` |

`Argument*` — некоректний вхідний аргумент сам по собі;
`InvalidOperationException` — аргументи коректні, але операція заборонена
в поточному стані об'єкта.

### Демонстрація

    dotnet run --project src/Cli -- --domain

Виводить сценарій «успіх» (реєстрація читача, видача, повернення, мапінг
`ToDto`/`FromDto`) і сценарій «порушення інваріантів»: кожна спроба обгорнута
в `try/catch`, на екран іде лише `Message`, без stack trace. Після всіх відмов
стан об'єктів незмінний.
