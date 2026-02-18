using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IExcelParsingService _excelParsingService;
    private readonly IValidationService _validationService;
    private readonly ILogger<UploadController> _logger;

    private static readonly string[] AllowedExtensions = [".xlsx", ".xls"];
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB

    public UploadController(
        IExcelParsingService excelParsingService,
        IValidationService validationService,
        ILogger<UploadController> logger)
    {
        _excelParsingService = excelParsingService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Загрузка Excel-файла с отчётом стриминговой платформы.
    /// Парсит файл, проводит валидацию каждой строки, сохраняет в БД.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail(400, "Файл не передан", "FILE_EMPTY"));

        if (file.Length > MaxFileSize)
            return BadRequest(ApiResponse<object>.Fail(400, "Файл слишком большой. Максимум 50 МБ", "FILE_TOO_LARGE"));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(ApiResponse<object>.Fail(400, "Допустимые форматы: .xlsx, .xls", "FILE_INVALID_FORMAT"));

        _logger.LogInformation("Загрузка файла: {FileName}, размер: {FileSize} байт", file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        var lines = _excelParsingService.ParseExcel(stream);

        if (lines.Count == 0)
            return BadRequest(ApiResponse<object>.Fail(400, "Файл не содержит данных", "FILE_NO_DATA"));

        var result = await _validationService.ValidateBatchAsync(lines);

        _logger.LogInformation(
            "Файл обработан: {FileName}, всего: {Total}, валидных: {Validated}, нет продукта: {NoProduct}, нет прав: {NoRights}",
            file.FileName, result.TotalRows, result.ValidatedCount, result.NoProductCount, result.NoRightsCount);

        return Ok(ApiResponse<ValidationResultDto>.Success(
            result,
            $"Файл обработан: {result.TotalRows} строк",
            "UPLOAD_COMPLETED"));
    }
}
