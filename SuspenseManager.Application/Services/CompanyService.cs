using Application.Interfaces;
using Common.DTOs;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class CompanyService : ICompanyService
{
    private readonly SuspenseManagerDbContext _db;

    public CompanyService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<Company>> GetCompaniesAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Companies
            .AsNoTracking()
            .Where(c => c.ArchiveLevel == 0);

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<Company?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.ArchiveLevel == 0, ct);
    }

    public async Task<Company> CreateAsync(CreateCompanyDto dto, CancellationToken ct = default)
    {
        var company = new Company
        {
            LegalName     = dto.LegalName,
            ShortName     = dto.ShortName,
            CompanyCode   = dto.CompanyCode ?? string.Empty,
            BankName      = dto.BankName ?? string.Empty,
            PhoneNumber   = dto.PhoneNumber ?? string.Empty,
            Country       = dto.Country ?? string.Empty,
            LegalAddress  = dto.LegalAddress ?? string.Empty,
            ActualAddress = dto.ActualAddress ?? string.Empty,
            Inn           = dto.Inn ?? string.Empty,
            Bic           = dto.Bic ?? string.Empty,
            CreateTime    = DateTime.UtcNow,
            ArchiveLevel  = 0,
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
        return company;
    }

    public async Task<Company> UpdateAsync(int id, UpdateCompanyDto dto, CancellationToken ct = default)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == id && c.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Компания #{id} не найдена");

        if (dto.LegalName     != null) company.LegalName     = dto.LegalName;
        if (dto.ShortName     != null) company.ShortName     = dto.ShortName;
        if (dto.CompanyCode   != null) company.CompanyCode   = dto.CompanyCode;
        if (dto.BankName      != null) company.BankName      = dto.BankName;
        if (dto.PhoneNumber   != null) company.PhoneNumber   = dto.PhoneNumber;
        if (dto.Country       != null) company.Country       = dto.Country;
        if (dto.LegalAddress  != null) company.LegalAddress  = dto.LegalAddress;
        if (dto.ActualAddress != null) company.ActualAddress = dto.ActualAddress;
        if (dto.Inn           != null) company.Inn           = dto.Inn;
        if (dto.Bic           != null) company.Bic           = dto.Bic;

        company.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return company;
    }
}
