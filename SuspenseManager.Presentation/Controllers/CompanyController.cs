using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuspenseManager.Middleware;

namespace SuspenseManager.Controllers;

/// <summary>
/// Контроллер компаний: отправители и получатели
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [RequirePermission(PermissionCodes.CatalogView)]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _companyService.GetCompaniesAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Models.Company>>.Success(result));
    }

    /// <summary>Компания по ID.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.CatalogView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var company = await _companyService.GetByIdAsync(id, ct);
        if (company == null)
            return NotFound(ApiResponse<object>.Fail(404, $"Компания с ID {id} не найдена", "NOT_FOUND"));

        return Ok(ApiResponse<Models.Company>.Success(company));
    }

    /// <summary>Создать компанию.</summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.CatalogCompaniesEdit)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyDto dto, CancellationToken ct)
    {
        var company = await _companyService.CreateAsync(dto, ct);
        return StatusCode(201, ApiResponse<Models.Company>.Created(company, "Компания создана", "COMPANY_CREATED"));
    }

    /// <summary>Обновить компанию (частичное обновление).</summary>
    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.CatalogCompaniesEdit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyDto dto, CancellationToken ct)
    {
        var company = await _companyService.UpdateAsync(id, dto, ct);
        return Ok(ApiResponse<Models.Company>.Success(company, "Компания обновлена", "COMPANY_UPDATED"));
    }
}
