using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
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
        var effectiveCode = dto.CompanyCode ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(effectiveCode))
        {
            var codeExists = await _db.Companies.AnyAsync(
                c => c.CompanyCode == effectiveCode && c.ArchiveLevel == 0, ct);
            if (codeExists)
                throw new BusinessException("Компания с таким кодом уже существует", "COMPANY_CODE_EXISTS", 409);
        }

        var legalNameExists = await _db.Companies.AnyAsync(
            c => c.LegalName == dto.LegalName && c.ArchiveLevel == 0, ct);
        if (legalNameExists)
            throw new BusinessException("Компания с таким полным наименованием уже существует", "COMPANY_LEGAL_NAME_EXISTS", 409);

        if (!string.IsNullOrWhiteSpace(dto.Inn))
        {
            var innExists = await _db.Companies.AnyAsync(
                c => c.Inn == dto.Inn && c.ArchiveLevel == 0, ct);
            if (innExists)
                throw new BusinessException("Компания с таким ИНН уже существует", "COMPANY_INN_EXISTS", 409);
        }

        if (!string.IsNullOrWhiteSpace(dto.Bic))
        {
            var bicExists = await _db.Companies.AnyAsync(
                c => c.Bic == dto.Bic && c.ArchiveLevel == 0, ct);
            if (bicExists)
                throw new BusinessException("Компания с таким БИК уже существует", "COMPANY_BIC_EXISTS", 409);
        }

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var phoneExists = await _db.Companies.AnyAsync(
                c => c.PhoneNumber == dto.PhoneNumber && c.ArchiveLevel == 0, ct);
            if (phoneExists)
                throw new BusinessException("Компания с таким номером телефона уже существует", "COMPANY_PHONE_EXISTS", 409);
        }

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

        if (dto.LegalName != null)
        {
            var legalNameExists = await _db.Companies.AnyAsync(
                c => c.LegalName == dto.LegalName && c.Id != id && c.ArchiveLevel == 0, ct);
            if (legalNameExists)
                throw new BusinessException("Компания с таким полным наименованием уже существует", "COMPANY_LEGAL_NAME_EXISTS", 409);
            company.LegalName = dto.LegalName;
        }
        if (dto.ShortName != null) company.ShortName = dto.ShortName;
        if (dto.CompanyCode != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.CompanyCode))
            {
                var codeExists = await _db.Companies.AnyAsync(
                    c => c.CompanyCode == dto.CompanyCode && c.Id != id && c.ArchiveLevel == 0, ct);
                if (codeExists)
                    throw new BusinessException("Компания с таким кодом уже существует", "COMPANY_CODE_EXISTS", 409);
            }
            company.CompanyCode = dto.CompanyCode;
        }
        if (dto.BankName != null) company.BankName = dto.BankName;
        if (dto.PhoneNumber != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var phoneExists = await _db.Companies.AnyAsync(
                    c => c.PhoneNumber == dto.PhoneNumber && c.Id != id && c.ArchiveLevel == 0, ct);
                if (phoneExists)
                    throw new BusinessException("Компания с таким номером телефона уже существует", "COMPANY_PHONE_EXISTS", 409);
            }
            company.PhoneNumber = dto.PhoneNumber;
        }
        if (dto.Country       != null) company.Country       = dto.Country;
        if (dto.LegalAddress  != null) company.LegalAddress  = dto.LegalAddress;
        if (dto.ActualAddress != null) company.ActualAddress = dto.ActualAddress;
        if (dto.Inn != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Inn))
            {
                var innExists = await _db.Companies.AnyAsync(
                    c => c.Inn == dto.Inn && c.Id != id && c.ArchiveLevel == 0, ct);
                if (innExists)
                    throw new BusinessException("Компания с таким ИНН уже существует", "COMPANY_INN_EXISTS", 409);
            }
            company.Inn = dto.Inn;
        }
        if (dto.Bic != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Bic))
            {
                var bicExists = await _db.Companies.AnyAsync(
                    c => c.Bic == dto.Bic && c.Id != id && c.ArchiveLevel == 0, ct);
                if (bicExists)
                    throw new BusinessException("Компания с таким БИК уже существует", "COMPANY_BIC_EXISTS", 409);
            }
            company.Bic = dto.Bic;
        }

        company.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return company;
    }
}
