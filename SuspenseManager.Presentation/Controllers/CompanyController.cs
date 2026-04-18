using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace SuspenseManager.Controllers;

/// <summary>
/// Контроллер компаний: отправители и получатели
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>
    /// Список компаний с пагинацией, фильтрацией, сортировкой — п.11, п.12
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _companyService.GetCompaniesAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Models.Company>>.Success(result));
    }

    /// <summary>Компания по ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var company = await _companyService.GetByIdAsync(id, ct);
        if (company == null)
            return NotFound(ApiResponse<object>.Fail(404, $"Компания с ID {id} не найдена", "NOT_FOUND"));

        return Ok(ApiResponse<Company>.Success(company));
    }

    /// <summary>Создать компанию.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyDto dto, CancellationToken ct)
    {
        var company = await _companyService.CreateAsync(dto, ct);
        return StatusCode(201, ApiResponse<Company>.Created(company, "Компания создана", "COMPANY_CREATED"));
    }

    /// <summary>Обновить компанию (частичное обновление).</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyDto dto, CancellationToken ct)
    {
        var company = await _companyService.UpdateAsync(id, dto, ct);
        return Ok(ApiResponse<Company>.Success(company, "Компания обновлена", "COMPANY_UPDATED"));
    }
}
