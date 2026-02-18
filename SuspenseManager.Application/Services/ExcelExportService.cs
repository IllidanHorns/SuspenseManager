using Application.Interfaces;
using ClosedXML.Excel;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly SuspenseManagerDbContext _db;

    public ExcelExportService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportGroupSuspensesAsync(int groupId, CancellationToken ct = default)
    {
        var suspenses = await _db.SuspenseLines
            .AsNoTracking()
            .Where(s => s.GroupId == groupId && s.ArchiveLevel == 0)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Суспенсы");

        // Заголовки
        var headers = new[]
        {
            "ID", "Статус", "ISRC", "Баркод", "Каталожный номер", "Артист",
            "Название", "Оператор", "Компания отправитель", "Компания получатель",
            "Тип договора", "Номер договора", "Территория", "Кол-во стримов",
            "PPD", "Валюта", "Курс обмена", "Жанр", "Причина", "Дата создания",
            "ID продукта", "ID группы"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        // Данные
        for (int row = 0; row < suspenses.Count; row++)
        {
            var s = suspenses[row];
            var r = row + 2;
            ws.Cell(r, 1).Value = s.Id;
            ws.Cell(r, 2).Value = s.BusinessStatus;
            ws.Cell(r, 3).Value = s.Isrc;
            ws.Cell(r, 4).Value = s.Barcode;
            ws.Cell(r, 5).Value = s.CatalogNumber;
            ws.Cell(r, 6).Value = s.Artist;
            ws.Cell(r, 7).Value = s.TrackTitle;
            ws.Cell(r, 8).Value = s.Operator;
            ws.Cell(r, 9).Value = s.SenderCompany;
            ws.Cell(r, 10).Value = s.RecipientCompany;
            ws.Cell(r, 11).Value = s.AgreementType;
            ws.Cell(r, 12).Value = s.AgreementNumber;
            ws.Cell(r, 13).Value = s.TerritoryCode;
            ws.Cell(r, 14).Value = s.Qty;
            ws.Cell(r, 15).Value = s.Ppd;
            ws.Cell(r, 16).Value = s.ExchangeCurrency;
            ws.Cell(r, 17).Value = s.ExchangeRate;
            ws.Cell(r, 18).Value = s.Genre;
            ws.Cell(r, 19).Value = s.CauseSuspense;
            ws.Cell(r, 20).Value = s.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(r, 21).Value = s.ProductId;
            ws.Cell(r, 22).Value = s.GroupId;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportGroupsAsync(int businessStatus, CancellationToken ct = default)
    {
        var groups = await _db.SuspenseGroups
            .AsNoTracking()
            .Include(g => g.Account)
            .Include(g => g.GroupMetaData)
            .Where(g => g.ArchiveLevel == 0 && g.BusinessStatus == businessStatus)
            .ToListAsync(ct);

        // Подсчитываем суспенсы для каждой группы
        var groupIds = groups.Select(g => g.Id).ToList();
        var suspenseCounts = await _db.SuspenseLines
            .Where(s => s.GroupId != null && groupIds.Contains(s.GroupId.Value) && s.ArchiveLevel == 0)
            .GroupBy(s => s.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count(), TotalRevenue = g.Sum(s => s.ExchangeCurrency) })
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Группы");

        var headers = new[]
        {
            "ID группы", "Статус", "ISRC", "Название", "Артист", "Баркод",
            "Каталожный номер", "Кол-во суспенсов", "Общая выручка",
            "Дата создания", "ID продукта"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        for (int row = 0; row < groups.Count; row++)
        {
            var g = groups[row];
            var stats = suspenseCounts.FirstOrDefault(sc => sc.GroupId == g.Id);
            var r = row + 2;

            ws.Cell(r, 1).Value = g.Id;
            ws.Cell(r, 2).Value = g.BusinessStatus;
            ws.Cell(r, 3).Value = g.GroupMetaData?.Isrc;
            ws.Cell(r, 4).Value = g.GroupMetaData?.Title;
            ws.Cell(r, 5).Value = g.GroupMetaData?.Artist;
            ws.Cell(r, 6).Value = g.GroupMetaData?.Barcode;
            ws.Cell(r, 7).Value = g.GroupMetaData?.CatalogNumber;
            ws.Cell(r, 8).Value = stats?.Count ?? 0;
            ws.Cell(r, 9).Value = stats?.TotalRevenue ?? 0;
            ws.Cell(r, 10).Value = g.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(r, 11).Value = g.CatalogProductId;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    private static byte[] WorkbookToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
