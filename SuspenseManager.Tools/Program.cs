using System;
using System.IO;
using ClosedXML.Excel;

// ── Генератор тестового отчёта (1000 строк) ──────────────────────────────────
// Запуск: dotnet run --project SuspenseManager.Tools
// Файл сохраняется в папку TestData/ в корне решения.
//
// Распределение строк:
//   650 × статус 0 (NoProduct) — ISRC не существует в каталоге
//   350 × статус 1 (NoRights)  — ISRC есть в каталоге, но права не совпадают:
//       ~100 — продукт без прав вообще (products[20..24])
//       ~80  — неверная территория
//       ~70  — неверный номер договора
//       ~60  — неверная компания-отправитель
//       ~40  — пустой номер договора (FindRightsAsync → false)
// ─────────────────────────────────────────────────────────────────────────────

var rng = new Random(42); // фиксированный seed → воспроизводимый файл

// ── Справочные данные (должны совпадать с DatabaseSeeder) ─────────────────────

var operators = new[] { "Яндекс Музыка", "Apple Music", "Spotify", "YouTube Music", "Deezer" };

var allTerritories = new[] { "RU", "DE", "FR", "GB", "US", "SE", "IT", "NL", "PL", "BE", "KZ", "UA", "BY", "AT", "CH" };

var agreementTypes = new[] { "License", "Distribution", "SubLicense", "Exclusive" };

// Продукты С правами в каталоге — полные данные их прав
// Формат: (ISRC, Barcode, Artist, Title, Genre, DocNumber, Territory, SenderCompany, ReceiverCompany)
var productsWithRights = new[]
{
    ("RU-A0A-25-00001","4607012345678","Артём Иванов",     "Летний вечер",    "Pop",       "DOC-2025-001","RU","Мелодия",  "Звук"),
    ("RU-A0A-25-00002","4607098765432","Мария Петрова",    "Ночной город",    "Rock",      "DOC-2025-002","RU","ПервоеМуз","Звук"),
    ("US-A0B-25-00001","0012345678905","John Smith",        "Midnight Dreams", "Electronic","DOC-2025-003","US","UMG",      "Sony Music"),
    ("RU-A0A-25-00006","4607011110001","Нева Рекордс",     "Белые ночи",      "Pop",       "DOC-2025-006","RU","Мелодия",  "Звук"),
    ("RU-A0A-25-00007","4607011110002","Полина Сова",      "Магнит",          "Pop",       "DOC-2025-007","RU","Союз",     "ПервоеМуз"),
    ("RU-A0A-25-00008","4607011110003","Арктика",          "Холодный фронт",  "Rock",      "DOC-2025-008","RU","НацМуз",  "ЦифрЗвук"),
    ("RU-A0A-25-00009","4607011110004","Лесной оркестр",   "Шёпот листьев",   "Jazz",      "DOC-2025-009","RU","РекордПаб","Звук"),
    ("RU-A0A-25-00010","4607011110005","Фотон",            "Скорость света",  "Electronic","DOC-2025-010","RU","ЦифрЗвук", "ПервоеМуз"),
    ("DE-A0D-25-00001","4006001100001","Klaus Weber",       "Herbstlicht",     "Classical", "DOC-2025-011","DE","BMG",      "Warner"),
    ("FR-A0E-25-00001","3700000100001","Jean Dubois",       "Minuit à Paris",  "Jazz",      "DOC-2025-012","FR","Believe",  "UMG"),
    ("SE-A0F-25-00001","7320000100001","Erik Strand",       "Nordlys",         "Folk",      "DOC-2025-013","SE","Warner",   "Sony Music"),
    ("RU-A0A-25-00014","4607011110009","Волга Бэнд",       "Город и река",    "Pop",       "DOC-2025-014","RU","Мелодия",  "Союз"),
    ("RU-A0A-25-00015","4607011110010","Ритм Коллектив",   "Пульс",           "Hip-Hop",   "DOC-2025-015","RU","НацМуз",   "Звук"),
    ("GB-A0C-25-00002","5012345679001","The Wanderers",     "Silver Rain",     "Rock",      "DOC-2025-016","GB","UMG",      "Warner"),
    ("US-A0B-25-00002","0012345679001","Marcus Cole",       "Deep Blue",       "Electronic","DOC-2025-017","US","Sony Music","UMG"),
    ("RU-A0A-25-00018","4607011110013","Соня Громова",      "Тихий час",       "Pop",       "DOC-2025-018","RU","ПервоеМуз","Мелодия"),
    ("RU-A0A-25-00019","4607011110014","Контраст",          "Огонь и лёд",     "Rock",      "DOC-2025-019","RU","Союз",     "НацМуз"),
    ("IT-A0G-25-00001","8030000100001","Marco Rossi",       "Sole d'Estate",   "Pop",       "DOC-2025-020","IT","UMG",      "Sony Music"),
};

// Продукты БЕЗ прав в каталоге — ISRC есть, но ни одной записи прав нет
var productsWithoutRights = new[]
{
    ("RU-A0A-25-00021","4607011110016","Серая Зона",       "Туман",            "Electronic"),
    ("RU-A0A-25-00022","4607011110017","Вакуум",           "Пустота",          "Ambient"),
    ("RU-A0A-25-00023","4607011110018","Точка Отсчёта",    "Нулевой меридиан", "Rock"),
    ("DE-A0D-25-00002","4006001100002","Stadt Klang",       "Leere Straßen",    "Electronic"),
    ("FR-A0E-25-00002","3700000100002","Claire Morin",      "L'Absence",        "Pop"),
};

// Фиктивные артисты и названия для статуса 0 (не существуют в каталоге)
var fakeArtists = new[]
{
    "DJ Phantom","The Lost Keys","Группа Экватор","Zero Gravity","Синяя Птица",
    "Midnight Echo","Северный Ветер","Anna Frost","The Circuit","Красный Квадрат",
    "Luke Stone","Тёмная Вода","Electric Soul","Квантовый Кот","Maria del Sol",
    "Broken Compass","Осколки","Binary Sunset","Городской Шаман","The Hollow",
    "Цифровой Мираж","Neon Wolves","Пустое Зеркало","Cascade","Речной Туман",
};

var fakeTitles = new[]
{
    "Горизонт","Static Noise","Бесконечность","Last Signal","Серый день",
    "Interference","Потерянная волна","Echo Chamber","Без следа","Flashback",
    "Тёмный угол","Frequency","Пустое небо","Hidden Path","Белый шум",
    "Parallel Lines","Смещение","Low Orbit","Забытый сигнал","Ghost Protocol",
    "Темп","Raw Signal","Невидимый фронт","Distance","Осколок времени",
};

var fakeGenres = new[] { "Pop", "Rock", "Electronic", "Hip-Hop", "Jazz", "Folk", "Ambient", "Classical", "R&B", "Soul" };

// Компании для неверных совпадений
var wrongSenders = new[] { "UnknownLabel", "FakeRecords", "SomeDistributor", "GlobalMusic", "IndieSound", "ArtHouse" };
var wrongReceivers = new[] { "UnknownDist", "FakePub", "SomeReceiver", "LocalRights", "MediaGroup", "SoundBridge" };

// Счётчики для каждого сценария
int noProductCount = 0;     // цель: 650
int noRights_noRec = 0;     // продукт без прав вообще, цель: 100
int noRights_wrongTerr = 0; // неверная территория, цель: 80
int noRights_wrongDoc = 0;  // неверный номер договора, цель: 70
int noRights_wrongComp = 0; // неверная компания, цель: 60
int noRights_emptyDoc = 0;  // пустой номер договора, цель: 40

// ── Создание Excel ────────────────────────────────────────────────────────────
using var workbook = new XLWorkbook();
var ws = workbook.AddWorksheet("Report");

string[] headers = ["ISRC","Barcode","CatalogNumber","ProductFormatCode",
    "SenderCompany","RecipientCompany","Operator","Artist","TrackTitle",
    "AgreementType","AgreementNumber","TerritoryCode","Qty","Ppd",
    "ExchangeCurrency","ExchangeRate","Genre"];

for (var i = 0; i < headers.Length; i++)
    ws.Cell(1, i + 1).Value = headers[i];

int dataRow = 2;

// ── Сценарий A: продукт в каталоге, прав вообще нет (~100 строк) ─────────────
for (int i = 0; i < 100 && noRights_noRec < 100; i++, noRights_noRec++)
{
    var p = productsWithoutRights[i % productsWithoutRights.Length];
    WriteRow(ws, dataRow++,
        isrc: p.Item1, barcode: p.Item2, catalogNum: $"CAT-{999 - i:000}", format: "DIGI",
        sender: RandomPick(wrongSenders), receiver: RandomPick(wrongReceivers),
        op: RandomOp(), artist: p.Item3, title: p.Item4,
        agType: RandomPick(agreementTypes), agNum: $"DOC-NOREC-{i+1:000}",
        territory: "RU", qty: Qty(), ppd: Ppd(), cur: Cur(), rate: Rate(), genre: p.Item5);
}

// ── Сценарий B: неверная территория (~80 строк) ───────────────────────────────
// ISRC совпадает, но TerritoryCode — другая страна
var wrongTerrs = new[] { "DE", "FR", "GB", "IT", "SE", "NL", "PL", "KZ", "UA", "AT" };
for (int i = 0; i < 80 && noRights_wrongTerr < 80; i++, noRights_wrongTerr++)
{
    var p = productsWithRights[i % productsWithRights.Length];
    // берём любую территорию кроме правильной
    var wrongTerr = wrongTerrs.First(t => t != p.Item7);
    WriteRow(ws, dataRow++,
        isrc: p.Item1, barcode: p.Item2, catalogNum: $"CAT-WTERR-{i+1:000}", format: "DIGI",
        sender: p.Item8, receiver: p.Item9,
        op: RandomOp(), artist: p.Item3, title: p.Item4,
        agType: RandomPick(agreementTypes), agNum: p.Item6,
        territory: wrongTerr, qty: Qty(), ppd: Ppd(), cur: Cur(), rate: Rate(), genre: p.Item5);
}

// ── Сценарий C: неверный номер договора (~70 строк) ──────────────────────────
for (int i = 0; i < 70 && noRights_wrongDoc < 70; i++, noRights_wrongDoc++)
{
    var p = productsWithRights[i % productsWithRights.Length];
    WriteRow(ws, dataRow++,
        isrc: p.Item1, barcode: p.Item2, catalogNum: $"CAT-WDOC-{i+1:000}", format: "DIGI",
        sender: p.Item8, receiver: p.Item9,
        op: RandomOp(), artist: p.Item3, title: p.Item4,
        agType: RandomPick(agreementTypes), agNum: $"WRONG-DOC-{i+1:000}",
        territory: p.Item7, qty: Qty(), ppd: Ppd(), cur: Cur(), rate: Rate(), genre: p.Item5);
}

// ── Сценарий D: неверная компания-отправитель (~60 строк) ────────────────────
for (int i = 0; i < 60 && noRights_wrongComp < 60; i++, noRights_wrongComp++)
{
    var p = productsWithRights[i % productsWithRights.Length];
    WriteRow(ws, dataRow++,
        isrc: p.Item1, barcode: p.Item2, catalogNum: $"CAT-WCOMP-{i+1:000}", format: "DIGI",
        sender: RandomPick(wrongSenders), receiver: p.Item9,
        op: RandomOp(), artist: p.Item3, title: p.Item4,
        agType: RandomPick(agreementTypes), agNum: p.Item6,
        territory: p.Item7, qty: Qty(), ppd: Ppd(), cur: Cur(), rate: Rate(), genre: p.Item5);
}

// ── Сценарий E: пустой номер договора (~40 строк) ────────────────────────────
for (int i = 0; i < 40 && noRights_emptyDoc < 40; i++, noRights_emptyDoc++)
{
    var p = productsWithRights[i % productsWithRights.Length];
    WriteRow(ws, dataRow++,
        isrc: p.Item1, barcode: p.Item2, catalogNum: $"CAT-NODOC-{i+1:000}", format: "DIGI",
        sender: p.Item8, receiver: p.Item9,
        op: RandomOp(), artist: p.Item3, title: p.Item4,
        agType: RandomPick(agreementTypes), agNum: "", // пустой → FindRightsAsync вернёт false
        territory: p.Item7, qty: Qty(), ppd: Ppd(), cur: Cur(), rate: Rate(), genre: p.Item5);
}

// ── Статус 0: несуществующие продукты (~650 строк) ───────────────────────────
// Генерируем ISRC с префиксом XX — их точно нет в каталоге
while (noProductCount < 650)
{
    int n = noProductCount + 1;
    var artist = fakeArtists[n % fakeArtists.Length];
    var title  = fakeTitles[n  % fakeTitles.Length];
    var genre  = fakeGenres[n  % fakeGenres.Length];
    var terr   = allTerritories[n % allTerritories.Length];

    WriteRow(ws, dataRow++,
        isrc: $"XX-ZZZ-25-{n:00000}", barcode: $"999{n:0000000000}", catalogNum: $"XX-CAT-{n:000}",
        format: "DIGI",
        sender: RandomPick(wrongSenders), receiver: RandomPick(wrongReceivers),
        op: RandomOp(), artist: artist, title: title,
        agType: RandomPick(agreementTypes), agNum: $"NODOC-{n:0000}",
        territory: terr, qty: Qty(), ppd: Ppd(), cur: Cur(), rate: Rate(), genre: genre);

    noProductCount++;
}

ws.Columns().AdjustToContents();

var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var outputDir   = Path.Combine(solutionDir, "TestData");
Directory.CreateDirectory(outputDir);

var filePath = Path.Combine(outputDir, "suspense_report_1000.xlsx");
workbook.SaveAs(filePath);

int total = noProductCount + noRights_noRec + noRights_wrongTerr + noRights_wrongDoc + noRights_wrongComp + noRights_emptyDoc;

Console.WriteLine($"Файл создан: {Path.GetFullPath(filePath)}");
Console.WriteLine($"Всего строк: {total}");
Console.WriteLine();
Console.WriteLine($"  Статус 0 (нет продукта):               {noProductCount}");
Console.WriteLine($"  Статус 1 — продукт без прав в каталоге: {noRights_noRec}");
Console.WriteLine($"  Статус 1 — неверная территория:         {noRights_wrongTerr}");
Console.WriteLine($"  Статус 1 — неверный номер договора:     {noRights_wrongDoc}");
Console.WriteLine($"  Статус 1 — неверная компания:           {noRights_wrongComp}");
Console.WriteLine($"  Статус 1 — пустой номер договора:       {noRights_emptyDoc}");
Console.WriteLine();
Console.WriteLine("Загрузите файл через POST /api/upload для заполнения БД тестовыми суспенсами.");

// ── Helpers ───────────────────────────────────────────────────────────────────

void WriteRow(IXLWorksheet sheet, int row,
    string isrc, string barcode, string catalogNum, string format,
    string sender, string receiver, string op,
    string artist, string title, string agType, string agNum,
    string territory, int qty, double ppd, double cur, double rate, string genre)
{
    sheet.Cell(row,  1).Value = isrc;
    sheet.Cell(row,  2).Value = barcode;
    sheet.Cell(row,  3).Value = catalogNum;
    sheet.Cell(row,  4).Value = format;
    sheet.Cell(row,  5).Value = sender;
    sheet.Cell(row,  6).Value = receiver;
    sheet.Cell(row,  7).Value = op;
    sheet.Cell(row,  8).Value = artist;
    sheet.Cell(row,  9).Value = title;
    sheet.Cell(row, 10).Value = agType;
    sheet.Cell(row, 11).Value = agNum;
    sheet.Cell(row, 12).Value = territory;
    sheet.Cell(row, 13).Value = qty;
    sheet.Cell(row, 14).Value = ppd;
    sheet.Cell(row, 15).Value = cur;
    sheet.Cell(row, 16).Value = rate;
    sheet.Cell(row, 17).Value = genre;
}

string RandomOp()  => operators[rng.Next(operators.Length)];
string RandomPick(string[] arr) => arr[rng.Next(arr.Length)];
int    Qty()  => rng.Next(100, 50_000);
double Ppd()  => Math.Round(rng.NextDouble() * 0.009 + 0.001, 5);
double Cur()  => rng.Next(2) == 0 ? 1.0 : Math.Round(rng.NextDouble() * 0.3 + 0.7, 4);
double Rate() => rng.Next(2) == 0 ? 1.0 : Math.Round(rng.NextDouble() * 30 + 80, 2);
