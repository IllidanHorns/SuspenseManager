using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface ICompanyService
{
    Task<PagedResponse<Company>> GetCompaniesAsync(PagedRequest request, CancellationToken ct = default);
    Task<Company?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Company> CreateAsync(CreateCompanyDto dto, CancellationToken ct = default);
    Task<Company> UpdateAsync(int id, UpdateCompanyDto dto, CancellationToken ct = default);
}
