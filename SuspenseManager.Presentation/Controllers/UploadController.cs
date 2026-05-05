using Application.Interfaces;
using Common.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IExcelParsingService _excelParsingService;
    private readonly IValidationService _validationService;
    private readonly IValidator<SuspenseLineDto> _suspenseLineValidator;
    private readonly ILogger<UploadController> _logger;

    private static readonly string[] AllowedExtensions = [".xlsx", ".xls"];
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB

    public UploadController(
        IExcelParsingService excelParsingService,
        IValidationService validationService,
        IValidator<SuspenseLineDto> suspenseLineValidator,
        ILogger<UploadController> logger)
    {
        _excelParsingService = excelParsingService;
        _validationService = validationService;
        _suspenseLineValidator = suspenseLineValidator;
        _logger = logger;
    }

    /// <summary>
    /// Ручной ввод одной строки суспенса.
    /// </summary>
    [HttpPost("manual")]
    public async Task<IActionResult> UploadManual([FromBody] SuspenseLineDto dto)
    {
        if (dto == null)
            return BadRequest(ApiResponse<object>.Fail(400, "Данные не переданы", "BODY_EMPTY"));

        var fv = await _suspenseLineValidator.ValidateAsync(dto);
        if (!fv.IsValid)
        {
            var errors = fv.Errors
                .Select(e => new ApiError { Field = e.PropertyName, Message = e.ErrorMessage })
                .ToList();
            return BadRequest(ApiResponse<object>.Fail(
                400, "Ошибки валидации входных данных", "VALIDATION_ERROR", errors));
        }

        if (string.IsNullOrWhiteSpace(dto.Isrc) &&
            string.IsNullOrWhiteSpace(dto.Barcode) &&
            string.IsNullOrWhiteSpace(dto.CatalogNumber))
        {
            return BadRequest(ApiResponse<object>.Fail(400,
                "Необходимо указать хотя бы одно из полей: ISRC, Баркод, Каталожный номер",
                "IDENTIFIER_REQUIRED"));
        }

        var result = await _validationService.ValidateBatchAsync([dto], numberRowsAsInExcel: false);
        return Ok(ApiResponse<ValidationResultDto>.Success(result, "Строка добавлена", "UPLOAD_COMPLETED"));
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
        {
            return BadRequest(ApiResponse<object>.Fail(400, "Файл не передан", "FILE_EMPTY"));
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(ApiResponse<object>.Fail(400, "Файл слишком большой. Максимум 50 МБ", "FILE_TOO_LARGE"));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<object>.Fail(400, "Допустимые форматы: .xlsx, .xls", "FILE_INVALID_FORMAT"));
        }

        _logger.LogInformation("Загрузка файла: {FileName}, размер: {FileSize} байт", file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        ExcelParseResult parseResult;
        try
        {
            parseResult = _excelParsingService.ParseExcel(stream);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось прочитать Excel-файл: {FileName}", file.FileName);
            return BadRequest(ApiResponse<object>.Fail(400,
                "Файл повреждён или не является корректным Excel-документом.",
                "FILE_CORRUPT"));
        }

        if (!parseResult.HeadersValid)
        {
            var detail = string.Join(" | ", parseResult.MissingRequiredHeaders);
            return BadRequest(ApiResponse<object>.Fail(400,
                "В первой строке файла должны быть заголовки всех столбцов шаблона. Отсутствуют колонки (допустимые подписи): "
                + detail,
                "EXCEL_MISSING_HEADERS"));
        }

        if (parseResult.Lines.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail(400, "Файл не содержит данных", "FILE_NO_DATA"));
        }

        var validLines = new List<SuspenseLineDto>();
        var rowFormatErrors = new List<RowFormatError>();
        for (var i = 0; i < parseResult.Lines.Count; i++)
        {
            var fv = await _suspenseLineValidator.ValidateAsync(parseResult.Lines[i]);
            if (fv.IsValid)
            {
                validLines.Add(parseResult.Lines[i]);
            }
            else
            {
                rowFormatErrors.Add(new RowFormatError
                {
                    RowNumber = i + 2,
                    Isrc = parseResult.Lines[i].Isrc,
                    Errors = fv.Errors.Select(e => e.ErrorMessage).ToList(),
                });
            }
        }

        if (validLines.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail(400,
                "Все строки файла содержат ошибки формата данных.",
                "ALL_ROWS_INVALID"));
        }

        var result = await _validationService.ValidateBatchAsync(validLines);
        result.RowFormatErrors = rowFormatErrors;

        _logger.LogInformation(
            "Файл обработан: {FileName}, всего: {Total}, валидных: {Validated}, нет продукта: {NoProduct}, нет прав: {NoRights}, ошибки формата: {FormatErrors}",
            file.FileName, result.TotalRows, result.ValidatedCount, result.NoProductCount, result.NoRightsCount, rowFormatErrors.Count);

        var message = rowFormatErrors.Count > 0
            ? $"Файл обработан: {result.TotalRows} строк, пропущено (ошибки формата): {rowFormatErrors.Count}"
            : $"Файл обработан: {result.TotalRows} строк";

        return Ok(ApiResponse<ValidationResultDto>.Success(result, message, "UPLOAD_COMPLETED"));
    }
}
