using System;
using System.IO;
using ClosedXML.Excel;

// Генерация тестового Excel-файла для проверки загрузки и валидации суспенсов.
// Содержит 6 строк, покрывающих все три сценария:
//   - Строки 1-3: продукт найден + права найдены → статус 88 (Validated)
//   - Строка 4-5: продукт найден, но прав нет → статус 1 (NoRights)
//   - Строка 6: продукт не найден → статус 0 (NoProduct)

using var workbook = new XLWorkbook();
var ws = workbook.AddWorksheet("Report");

// Заголовки
var headers = new[]
{
    "ISRC", "Barcode", "CatalogNumber", "ProductFormatCode",
    "SenderCompany", "RecipientCompany", "Operator",
    "Artist", "TrackTitle", "AgreementType", "AgreementNumber",
    "TerritoryCode", "Qty", "Ppd", "ExchangeCurrency", "ExchangeRate", "Genre"
};

for (var i = 0; i < headers.Length; i++)
    ws.Cell(1, i + 1).Value = headers[i];

// Строка 1: "Летний вечер" — полное совпадение продукта + прав → статус 88
AddRow(ws, 2,
    "RU1234567890", "4607012345678", "CAT-001", "DIGI",
    "Мелодия", "Звук", "Spotify",
    "Артём Иванов", "Летний вечер", "License", "DOC-2025-001",
    "RU", 1500, 0.003, 1, 1, "Pop");

// Строка 2: "Ночной город" — полное совпадение продукта + прав → статус 881
AddRow(ws, 3,
    "RU0987654321", "4607098765432", "CAT-002", "DIGI",
    "ПервоеМуз", "Звук", "Apple Music",
    "Мария Петрова", "Ночной город", "License", "DOC-2025-002",
    "RU", 2300, 0.004, 1, 1, "Rock");

// Строка 3: "Midnight Dreams" — полное совпадение продукта + прав → статус 88
AddRow(ws, 4,
    "US1234567890", "0012345678905", "CAT-003", "DIGI",
    "UMG", "Sony Music", "YouTube Music",
    "John Smith", "Midnight Dreams", "License", "DOC-2025-003",
    "US", 5000, 0.005, 0.85m, 1.18m, "Electronic");

// Строка 4: "Rainy Day" — продукт есть, но прав НЕТ → статус 1
AddRow(ws, 5,
    "GB9876543210", "5012345678901", "CAT-004", "CD",
    "SomeLabel", "SomeDist", "Deezer",
    "Emma Wilson", "Rainy Day", "License", "DOC-NONE-001",
    "GB", 800, 0.002, 0.73m, 1.37m, "Jazz");

// Строка 5: "Дорога домой" — продукт есть, но прав НЕТ → статус 1
AddRow(ws, 6,
    "RU5555555555", "4607055555555", "CAT-005", "DIGI",
    "НеизвестнаяКомпания", "ДругаяКомпания", "Яндекс Музыка",
    "Группа Путь", "Дорога домой", "License", "DOC-NONE-002",
    "RU", 3200, 0.0025, 1, 1, "Rock");

// Строка 6: несуществующий продукт → статус 0
AddRow(ws, 7,
    "XX0000000000", "0000000000000", "CAT-999", "DIGI",
    "FakeLabel", "FakeDist", "TikTok",
    "Unknown Artist", "Ghost Track", "License", "DOC-FAKE-001",
    "WW", 100, 0.001, 1, 1, "Unknown");

// Авторазмер колонок
ws.Columns().AdjustToContents();

// Сохраняем в папку TestData в корне решения
var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var outputPath = Path.Combine(solutionDir, "TestData");
Directory.CreateDirectory(outputPath);

var filePath = Path.Combine(outputPath, "test_report.xlsx");
workbook.SaveAs(filePath);

Console.WriteLine($"Тестовый файл создан: {Path.GetFullPath(filePath)}");
Console.WriteLine();
Console.WriteLine("Содержание:");
Console.WriteLine("  Строка 1: Летний вечер      -> ожидается статус 88 (Validated)");
Console.WriteLine("  Строка 2: Ночной город      -> ожидается статус 88 (Validated)");
Console.WriteLine("  Строка 3: Midnight Dreams    -> ожидается статус 88 (Validated)");
Console.WriteLine("  Строка 4: Rainy Day          -> ожидается статус 1  (NoRights)");
Console.WriteLine("  Строка 5: Дорога домой       -> ожидается статус 1  (NoRights)");
Console.WriteLine("  Строка 6: Ghost Track        -> ожидается статус 0  (NoProduct)");

static void AddRow(IXLWorksheet ws, int row,
    string isrc, string barcode, string catalogNumber, string productFormatCode,
    string sender, string recipient, string operatorName,
    string artist, string trackTitle, string agreementType, string agreementNumber,
    string territoryCode, int qty, double ppd, decimal exchangeCurrency, decimal exchangeRate,
    string genre)
{
    ws.Cell(row, 1).Value = isrc;
    ws.Cell(row, 2).Value = barcode;
    ws.Cell(row, 3).Value = catalogNumber;
    ws.Cell(row, 4).Value = productFormatCode;
    ws.Cell(row, 5).Value = sender;
    ws.Cell(row, 6).Value = recipient;
    ws.Cell(row, 7).Value = operatorName;
    ws.Cell(row, 8).Value = artist;
    ws.Cell(row, 9).Value = trackTitle;
    ws.Cell(row, 10).Value = agreementType;
    ws.Cell(row, 11).Value = agreementNumber;
    ws.Cell(row, 12).Value = territoryCode;
    ws.Cell(row, 13).Value = qty;
    ws.Cell(row, 14).Value = ppd;
    ws.Cell(row, 15).Value = (double)exchangeCurrency;
    ws.Cell(row, 16).Value = (double)exchangeRate;
    ws.Cell(row, 17).Value = genre;
}
