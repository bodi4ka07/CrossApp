# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.
Призначення: облік видач примірників книг читачам і повернень.

## Структура solution

    CrossApp/
    ├── CrossApp.slnx
    ├── README.md
    ├── .gitignore
    ├── data/                          # вхідні файли (sample.csv, sample.json, ...)
    └── src/
        ├── Core/                      # бібліотека класів (без точки входу)
        │   ├── Core.csproj
        │   ├── EnvironmentInfo.cs
        │   ├── SampleData.cs          # початкові дані для демо (16 примірників, 3 читачі)
        │   ├── Abstractions/          # ILibraryStore — контракт сховища
        │   ├── Domain/                # сутності з поведінкою
        │   ├── Dto/                   # record-DTO
        │   ├── Import/                # імпорт CSV/JSON
        │   ├── Services/              # LendingService — бізнес-операції
        │   └── Storage/               # InMemoryLibraryStore, FileLibraryStore
        └── Cli/                       # консольний застосунок, залежить від Core
            ├── Cli.csproj
            └── Program.cs             # composition root + сценарії

## Запуск

    dotnet build
    dotnet run --project src/Cli                      # лаб. 5: сховище в пам'яті
    dotnet run --project src/Cli -- --file            # лаб. 5: файлове сховище
    dotnet run --project src/Cli -- data/sample.csv   # лаб. 3: імпорт CSV/JSON
    dotnet run --project src/Cli -- --domain          # лаб. 4: демонстрація доменної моделі
    dotnet run --project src/Cli -- --mixed           # лаб. 3: змішаний імпорт

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

## Формат вхідного файлу (CSV)

Роздільник колонок — `;` (крапка з комою), кодування — UTF-8.
Колонки: `id;isbn;title;year`. Перший рядок-заголовок (`id;isbn;...`)
і порожні рядки пропускаються автоматично.

## Доменна модель (лабораторна 4)

Каталог `src/Core/Domain/` — сутності з поведінкою. Records з тижня 3
(`BookDto`, `ReaderDto`, `BookCopyDto`, `LoanDto`) лишаються DTO: вони переносять
дані, а сутність захищає правила. Зв'язок між ними — `ToDto()` / `FromDto(dto)`.

    src/Core/Domain/
    ├── BookCopy.cs            # примірник книги (виданий / на полиці)
    ├── Loan.cs                # видача примірника читачеві
    ├── LoanStatus.cs          # enum: Active / Returned / Cancelled
    ├── Reader.cs              # читач зі своїми видачами (захищена колекція)
    └── BookCopyAssembler.cs   # ImportResult<BookDto> → ImportResult<BookCopy>

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
| 8 | Дозволені лише переходи `Active → Returned` і `Active → Cancelled` | `InvalidOperationException` | `Loan.EnsureTransition` |
| 9 | Повертати можна лише той примірник, на який оформлено видачу | `InvalidOperationException` | `Loan.EnsureSameCopy` |
| 10 | Стан видачі узгоджений із датою повернення | `ArgumentException` | `Loan.Restore` |
| 11 | Стан у DTO має бути відомим значенням `LoanStatus` | `ArgumentException` | `Loan.FromDto` |
| 12 | Читач не може мати більше 5 відкритих видач | `InvalidOperationException` | `Reader.TakeLoan` |
| 13 | Закрити чужу видачу неможливо | `InvalidOperationException` | `Reader.ReturnLoan` |

`Argument*` — некоректний вхідний аргумент сам по собі;
`InvalidOperationException` — аргументи коректні, але операція заборонена
в поточному стані об'єкта.

### Демонстрація

    dotnet run --project src/Cli -- --domain

Виводить сценарій «успіх» (реєстрація читача, видача, повернення, мапінг
`ToDto`/`FromDto`) і сценарій «порушення інваріантів»: кожна спроба обгорнута
в `try/catch`, на екран іде лише `Message`, без stack trace. Після всіх відмов
стан об'єктів незмінний.

### Додаткове завдання (лабораторна 4)

`BookCopyAssembler.ToDomain(ImportResult<BookDto>)` перетворює результат імпорту
тижня 3 на сутності: повертає `ImportResult<BookCopy>` зі списком створених
примірників і переліком рядків, які не пройшли інваріанти (до помилок парсингу
додаються помилки доменних перевірок).

Інваріант «читач не може мати більше 5 відкритих видач» охоплює дві сутності
(`Reader` і `BookCopy`) і реалізований в агрегаті `Reader`, бо саме він володіє
списком видач. Правила, які потребують даних поза агрегатом (наприклад, перевірка
наявності примірника у сховищі), виносяться в сервіс рівня застосунку — сутність
не повинна ходити до сховища; це `LendingService` лабораторної 5.

Стан видачі описує `enum LoanStatus { Active, Returned, Cancelled }`, а не набір
булевих прапорців. Допустимі переходи перевіряє один switch expression у
`Loan.EnsureTransition`: з `Active` можна перейти в `Returned` (повернення)
або в `Cancelled` (помилково оформлену видачу скасовано), будь-який інший перехід —
`InvalidOperationException` із назвами обох станів у повідомленні. Той самий
підхід у `Loan.Restore` стежить, щоб стан узгоджувався з датою повернення:
`Returned` без дати (чи `Active` з датою) — зіпсований запис, а не коректна сутність.

## Лабораторна 5: сервісний шар, два сховища, ручний DI

Бізнес-операції бібліотеки винесено в `LendingService`, який залежить лише від
інтерфейсу `ILibraryStore`, а не від конкретного сховища. Конкретні класи
створюються в одному місці — `src/Cli/Program.cs` (composition root).
`Microsoft.Extensions.DependencyInjection` не підключено: залежності передаються
вручну через конструктор.

### 1. Домен і контракт

Головна сутність — `BookCopy` (примірник книги); `Reader` — агрегат, який володіє
своїми видачами `Loan`. Файли:

    src/Core/Abstractions/ILibraryStore.cs
    src/Core/Storage/InMemoryLibraryStore.cs
    src/Core/Storage/FileLibraryStore.cs
    src/Core/Services/LendingService.cs
    src/Core/SampleData.cs
    src/Core/Dto/LibraryFileDto.cs

Контракт `ILibraryStore` (7 методів, лише ті, що потрібні сервісу):

| Метод | Навіщо |
|-------|--------|
| `ListCopies()` | показати всі примірники; повертає `IReadOnlyList`, щоб зовнішній код не змінював сховище в обхід Add/Update |
| `GetCopy(id)` | знайти примірник; `BookCopy?` — запису може не бути |
| `AddCopy(copy)` | додати примірник; дубль id — `InvalidOperationException` |
| `UpdateCopy(copy)` | зберегти зміну стану примірника (виданий / на полиці) |
| `GetReader(id)` | знайти читача разом з його видачами; `Reader?` |
| `AddReader(reader)` | зареєструвати читача; дубль id — `InvalidOperationException` |
| `UpdateReader(reader)` | зберегти зміни читача (нова або закрита видача) |

Методу `Remove` у контракті немає: сервіс нічого не видаляє, а інтерфейс має
містити лише потрібні операції. Інваріанти предметної області перевіряють
сутності, сховище перевіряє тільки унікальність id.

Сервіс `LendingService`: `AddBook`, `RegisterReader`, `IssueCopy`, `ReturnCopy`,
`All`, `Find`. Відсутній читач чи примірник перетворюється на
`InvalidOperationException` через `?? throw`.

### 2. Дві реалізації

| | `InMemoryLibraryStore` | `FileLibraryStore` |
|---|---|---|
| Де дані | два `Dictionary` у пам'яті | кеш у пам'яті + JSON-файл |
| Початкові дані | через конструктор (`SampleData`) | з файлу при першому зверненні (`EnsureLoaded`) |
| Збереження | немає — після виходу дані зникають | `Flush()` після кожної зміни |
| Порівняння id | `OrdinalIgnoreCase` | `OrdinalIgnoreCase` |

Файл: `data/library.json` у каталозі застосунку (`AppContext.BaseDirectory`, тобто
`src/Cli/bin/.../data/library.json`). Каталог створюється автоматично.
Формат — JSON з відступами, об'єкт `LibraryFileDto`:

    {
      "Copies":  [ { "Id": "C-001", "Isbn": "...", "Title": "...", "IsIssued": false }, ... ],
      "Readers": [ { "Reader": { "Id": "R-001", "FullName": "...", "Email": "..." },
                     "Loans":  [ { "Id": "L-...", "CopyId": "...", "ReaderId": "...",
                                   "IssuedOn": "2026-10-10", "ReturnedOn": null,
                                   "Status": "Active" } ] } ]
    }

На диск ідуть DTO, а не сутності: сутність має приватні сетери, а формат файлу не
повинен диктувати форму домену. Мапінг — `ToDto()` / `FromDto()`; під час читання
дані проходять ті самі інваріанти, що й створення, тож зіпсований файл не дасть
некоректну сутність. Кирилиця в файлі може виглядати як `\u0410...` — це штатне
екранування `System.Text.Json`.

Кеш потрібен, щоб не читати й не розбирати файл на кожен виклик: файл читається
один раз, а на диск пишеться лише при зміні.

### 3. Схема залежностей

    Cli (Program.cs) → LendingService → ILibraryStore → InMemoryLibraryStore | FileLibraryStore

`LendingService` не знає слів «File» і «Dictionary». У `src/Core` назва
`FileLibraryStore` зустрічається лише у файлі самої реалізації, а `Console.` — ніде.

### 4. Composition root

```csharp
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");
ILibraryStore store = useFile
    ? new FileLibraryStore(dataPath)
    : new InMemoryLibraryStore(SampleData.Copies(), SampleData.Readers());
var service = new LendingService(store);
Console.WriteLine($"Сховище: {store.GetType().Name}");
```

Це єдине місце з конкретними класами сховищ.

### 5. Запуск і вивід

    dotnet run --project src/Cli
    dotnet run --project src/Cli -- --file

Приклад виводу (id видачі генеруються випадково, тому в кожному запуску різні):

    Сховище: InMemoryLibraryStore

    === Сценарій 1: успіх ===
    Додано: C-1a2b3c4d [978-0-00-000000-0] Нова книга — на полиці
    Зареєстровано: R-5e6f7a8b Тестовий Читач <reader@example.com> — відкритих видач: 0
    Видано: L-9c0d1e2f: примірник C-1a2b3c4d → читач R-5e6f7a8b, видано 2026-10-10, не повернено
    Знайдено за id: C-1a2b3c4d [978-0-00-000000-0] Нова книга — виданий
    Повернено: C-1a2b3c4d [978-0-00-000000-0] Нова книга — на полиці
    Усього примірників у сховищі: 17; останні 3: ...

    === Сценарій 2: відмови ===
      неіснуючий читач: InvalidOperationException — Немає читача з id=R-999.
      неіснуючий примірник: InvalidOperationException — Немає примірника з id=C-999.
      повернення без відкритої видачі: InvalidOperationException — У читача ... немає відкритої видачі примірника ...
      дубль id примірника: InvalidOperationException — Примірник з id=... уже існує.

З `--file` перший рядок: `Сховище: FileLibraryStore`, далі `Файл: <шлях до library.json>`.
Кожен запуск у файловому режимі додає нову книгу й читача, тому лічильник
«Усього примірників» зростає (накопичення, а не обнулення). У режимі пам'яті після
виходу дані зникають.
