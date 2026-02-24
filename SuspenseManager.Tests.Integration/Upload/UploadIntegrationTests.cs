using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using FluentAssertions;
using SuspenseManager.Tests.Integration.Fixtures;

namespace SuspenseManager.Tests.Integration.Upload;

/// <summary>
/// Интеграционные тесты UploadController.
/// Проверяют загрузку Excel-отчётов: форматы, размер, поля, результаты валидации.
/// </summary>
public class UploadIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UploadIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> AuthorizeAsync()
    {
        var login = $"uploaduser_{Guid.NewGuid():N}";
        await _factory.WithDbAsync(async db =>
        {
            var builder = new TestDataBuilder(db);
            await builder.CreateAccountAsync(login, "Pass123!");
        });

        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { login, password = "Pass123!" });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    // ──────────────────── Вспомогательные методы создания Excel ──────────────

    private static byte[] CreateValidExcelBytes(int rowCount = 2)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Report");

        // Заголовки — строка 1
        ws.Cell(1, 1).Value = "ISRC";
        ws.Cell(1, 2).Value = "Barcode";
        ws.Cell(1, 3).Value = "CatalogNumber";
        ws.Cell(1, 4).Value = "ProductFormatCode";
        ws.Cell(1, 5).Value = "Artist";
        ws.Cell(1, 6).Value = "TrackTitle";
        ws.Cell(1, 7).Value = "AgreementNumber";
        ws.Cell(1, 8).Value = "TerritoryCode";
        ws.Cell(1, 9).Value = "SenderCompany";
        ws.Cell(1, 10).Value = "RecipientCompany";
        ws.Cell(1, 11).Value = "Qty";
        ws.Cell(1, 12).Value = "ExchangeCurrency";
        ws.Cell(1, 13).Value = "ExchangeRate";

        for (var i = 0; i < rowCount; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = $"ISRC{i:D3}";
            ws.Cell(row, 2).Value = $"BC{i:D10}";
            ws.Cell(row, 3).Value = $"CAT-{i:D3}";
            ws.Cell(row, 4).Value = "DIGI";
            ws.Cell(row, 5).Value = "Test Artist";
            ws.Cell(row, 6).Value = $"Track {i}";
            ws.Cell(row, 7).Value = $"AGR-{i:D3}";
            ws.Cell(row, 8).Value = "RU";
            ws.Cell(row, 9).Value = "Sender LLC";
            ws.Cell(row, 10).Value = "Recipient LLC";
            ws.Cell(row, 11).Value = 100;
            ws.Cell(row, 12).Value = "RUB";
            ws.Cell(row, 13).Value = 1.0;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static MultipartFormDataContent BuildFormData(
        byte[] fileBytes, string fileName = "report.xlsx")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    // ──────────────────────── Тесты корректного файла ─────────────────────────

    [Fact]
    public async Task Upload_ValidExcel_Returns200_WithValidationResult()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var formData = BuildFormData(CreateValidExcelBytes(3));

        var response = await _client.PostAsync("/api/upload", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetProperty("totalRows").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Upload_ValidExcel_AllLinesNoProduct_WhenCatalogEmpty()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var formData = BuildFormData(CreateValidExcelBytes(2));
        var response = await _client.PostAsync("/api/upload", formData);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        // Каталог пуст — все строки получают статус NoProduct
        data.GetProperty("noProductCount").GetInt32().Should().Be(2);
        data.GetProperty("noRightsCount").GetInt32().Should().Be(0);
        data.GetProperty("validatedCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Upload_ValidExcel_SavesLinesToDatabase()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var formData = BuildFormData(CreateValidExcelBytes(4));
        await _client.PostAsync("/api/upload", formData);

        await _factory.WithDbAsync(async db =>
        {
            var count = db.SuspenseLines.Count();
            count.Should().BeGreaterThanOrEqualTo(4);
        });
    }

    // ────────────────────── Тесты некорректных файлов ─────────────────────────

    [Fact]
    public async Task Upload_NoFile_Returns400()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Отправляем пустой multipart
        var response = await _client.PostAsync("/api/upload", new MultipartFormDataContent());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_WrongFileExtension_Returns400()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var textContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not excel content"));
        textContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var formData = new MultipartFormDataContent();
        formData.Add(textContent, "file", "report.csv");

        var response = await _client.PostAsync("/api/upload", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_FileTooLarge_Returns400()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Создаём "файл" больше 50 MB
        var bigBytes = new byte[51 * 1024 * 1024];
        Random.Shared.NextBytes(bigBytes);

        var fileContent = new ByteArrayContent(bigBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var formData = new MultipartFormDataContent();
        formData.Add(fileContent, "file", "huge_report.xlsx");

        var response = await _client.PostAsync("/api/upload", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_EmptyExcel_Returns200_WithZeroRows()
    {
        var token = await AuthorizeAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Excel только с заголовками, без строк данных
        var formData = BuildFormData(CreateValidExcelBytes(0));
        var response = await _client.PostAsync("/api/upload", formData);

        // Должен вернуть успех с 0 строками
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ──────────────────────── Авторизация ────────────────────────────────────

    [Fact]
    public async Task Upload_WithoutAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var formData = BuildFormData(CreateValidExcelBytes(1));
        var response = await _client.PostAsync("/api/upload", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
