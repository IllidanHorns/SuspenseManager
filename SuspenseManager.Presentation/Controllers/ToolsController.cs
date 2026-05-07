using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Presentation.Controllers;

[ApiController]
[Route("api/tools")]
[Authorize]
public class ToolsController : ControllerBase
{
    private readonly ISampleExcelService _sampleExcel;

    public ToolsController(ISampleExcelService sampleExcel)
    {
        _sampleExcel = sampleExcel;
    }

    /// <summary>
    /// Скачать демонстрационный Excel-файл для тестовой загрузки.
    /// Содержит 5 валидных строк (→88), 4 без прав (→1), 3 без продукта (→0).
    /// Цветовая маркировка объясняет ожидаемый статус каждой строки.
    /// </summary>
    [HttpGet("sample-excel")]
    public IActionResult DownloadSampleExcel()
    {
        var bytes = _sampleExcel.GenerateSampleUploadFile();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "sample_upload.xlsx");
    }
}
