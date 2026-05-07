using Application.Interfaces;
using ClosedXML.Excel;

namespace Application.Services;

/// <summary>
/// Генерирует демонстрационный Excel-файл для тестовой загрузки.
/// Файл содержит 12 строк: 5 валидных (статус 88), 4 без прав (статус 1), 3 без продукта (статус 0).
/// </summary>
public class SampleExcelService : ISampleExcelService
{
    public byte[] GenerateSampleUploadFile()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Отчёт");

        // Заголовки (должны совпадать с ColumnAliases в ExcelParsingService)
        var headers = new[]
        {
            "ISRC", "Баркод", "Каталожный номер", "TTkey",
            "Компания отправитель", "Компания получатель", "Оператор",
            "Артист", "Название", "Тип договора", "Номер договора",
            "Код территории", "Жанр", "Количество",
            "Цена за стрим", "Валюта", "Курс обмена"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Данные строк:
        // Колонки: ISRC | Баркод | КатНомер | TTkey | Отправитель | Получатель | Оператор |
        //          Артист | Название | ТипДог | НомерДог | Территория | Жанр | Кол-во | PPD | Валюта | Курс
        var rows = new object?[][]
        {
            // ── 5 строк → статус 88 (валидировано) ──────────────────────────────────────
            // Продукт + права есть, данные совпадают с сидером
            ["RU-A0A-25-00001", "4607012345678", "CAT-001", "DIGI", "Мелодия", "Звук",
             "Яндекс Музыка", "Артём Иванов", "Летний вечер",
             "Лицензионный", "DOC-2025-001", "RU", "Pop", 12500, 0.0042, "RUB", 1.0],

            ["RU-A0A-25-00006", "4607011110001", "CAT-006", "DIGI", "Мелодия", "Звук",
             "Яндекс Музыка", "Нева Рекордс", "Белые ночи",
             "Лицензионный", "DOC-2025-006", "RU", "Pop", 9800, 0.0038, "RUB", 1.0],

            ["RU-A0A-25-00007", "4607011110002", "CAT-007", "DIGI", "Союз", "ПервоеМуз",
             "Spotify", "Полина Сова", "Магнит",
             "Лицензионный", "DOC-2025-007", "RU", "Pop", 15200, 0.0051, "RUB", 1.0],

            ["RU-A0A-25-00009", "4607011110004", "CAT-009", "DIGI", "РекордПаб", "Звук",
             "Apple Music", "Лесной оркестр", "Шёпот листьев",
             "Лицензионный", "DOC-2025-009", "RU", "Jazz", 6700, 0.0045, "RUB", 1.0],

            ["US-A0B-25-00001", "0012345678905", "CAT-003", "DIGI", "UMG", "Sony Music",
             "Spotify", "John Smith", "Midnight Dreams",
             "License", "DOC-2025-003", "US", "Electronic", 48000, 0.0039, "USD", 91.5],

            // ── 4 строки → статус 1 (нет прав, продукт есть) ────────────────────────────
            // Продукты есть в каталоге (CAT-021..025), но прав нет
            ["RU-A0A-25-00021", "4607011110016", "CAT-021", "DIGI", "Мелодия", "Звук",
             "Яндекс Музыка", "Серая Зона", "Туман",
             "Лицензионный", "UNKNOWN-001", "RU", "Electronic", 3200, 0.003, "RUB", 1.0],

            ["RU-A0A-25-00022", "4607011110017", "CAT-022", "DIGI", "Мелодия", "Звук",
             "Spotify", "Вакуум", "Пустота",
             "Лицензионный", "UNKNOWN-002", "RU", "Ambient", 1500, 0.003, "RUB", 1.0],

            ["RU-A0A-25-00023", "4607011110018", "CAT-023", "DIGI", "НацМуз", "ЦифрЗвук",
             "Apple Music", "Точка Отсчёта", "Нулевой меридиан",
             "Лицензионный", "UNKNOWN-003", "RU", "Rock", 5100, 0.004, "RUB", 1.0],

            ["DE-A0D-25-00002", "4006001100002", "CAT-024", "DIGI", "BMG", "WMG",
             "Spotify", "Stadt Klang", "Leere Straßen",
             "License", "UNKNOWN-004", "DE", "Electronic", 7800, 0.0042, "EUR", 100.3],

            // ── 3 строки → статус 0 (нет продукта) ─────────────────────────────────────
            // ISRC не существует в каталоге
            ["XX-DEMO-00-00001", null, null, "DIGI", "Мелодия", "Звук",
             "Яндекс Музыка", "Незнакомый Артист", "Загадочный Трек",
             "Лицензионный", "NEW-001", "RU", "Pop", 2300, 0.003, "RUB", 1.0],

            ["XX-DEMO-00-00002", null, null, "DIGI", "Союз", "ПервоеМуз",
             "Spotify", "Новая Группа", "Дебютный Сингл",
             "Лицензионный", "NEW-002", "RU", "Rock", 4100, 0.004, "RUB", 1.0],

            ["XX-DEMO-00-00003", null, null, "DIGI", "UMG", "Sony Music",
             "Apple Music", "Unknown Artist", "First Track",
             "License", "NEW-003", "US", "Pop", 6000, 0.0038, "USD", 91.5],
        };

        for (var r = 0; r < rows.Length; r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Length; c++)
            {
                if (row[c] == null) continue;
                var cell = ws.Cell(r + 2, c + 1);
                switch (row[c])
                {
                    case string s:   cell.Value = s;         break;
                    case int i:      cell.Value = i;         break;
                    case double d:   cell.Value = d;         break;
                    case decimal dc: cell.Value = (double)dc; break;
                    default:         cell.Value = row[c]!.ToString(); break;
                }
            }

            // Цветовая маркировка для удобства проверяющего
            var color = r < 5
                ? XLColor.FromHtml("#E2EFDA") // зелёный = валидировано
                : r < 9
                    ? XLColor.FromHtml("#FFF2CC") // жёлтый = нет прав
                    : XLColor.FromHtml("#FCE4D6"); // оранжевый = нет продукта

            for (var c = 1; c <= headers.Length; c++)
                ws.Cell(r + 2, c).Style.Fill.BackgroundColor = color;
        }

        // Добавляем легенду на второй лист
        var legend = workbook.Worksheets.Add("Легенда");
        legend.Cell(1, 1).Value = "Цвет";
        legend.Cell(1, 2).Value = "Ожидаемый статус";
        legend.Cell(1, 3).Value = "Описание";
        legend.Row(1).Style.Font.Bold = true;

        legend.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
        legend.Cell(2, 1).Value = "Зелёный";
        legend.Cell(2, 2).Value = "88 — Валидировано";
        legend.Cell(2, 3).Value = "Продукт + права найдены → строка обработана, в суспенс не попадает";

        legend.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
        legend.Cell(3, 1).Value = "Жёлтый";
        legend.Cell(3, 2).Value = "1 — Нет прав";
        legend.Cell(3, 3).Value = "Продукт найден в каталоге, но права не определены";

        legend.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4D6");
        legend.Cell(4, 1).Value = "Оранжевый";
        legend.Cell(4, 2).Value = "0 — Нет продукта";
        legend.Cell(4, 3).Value = "ISRC отсутствует в каталоге продуктов";

        legend.Columns().AdjustToContents();
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
