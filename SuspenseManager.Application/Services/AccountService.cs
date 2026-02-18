using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class AccountService : IAccountService
{
    private readonly SuspenseManagerDbContext _db;

    public AccountService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<Account>> GetAccountsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Accounts
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.ArchiveLevel == 0);

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<Account?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Accounts
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Rights)
            .FirstOrDefaultAsync(a => a.Id == id && a.ArchiveLevel == 0, ct);
    }

    public async Task<Account> CreateAsync(CreateAccountDto dto, CancellationToken ct = default)
    {
        var exists = await _db.Accounts.AnyAsync(a => a.Login == dto.Login && a.ArchiveLevel == 0, ct);
        if (exists)
        {
            throw new BusinessException("Аккаунт с таким логином уже существует", "ACCOUNT_EXISTS", 409);
        }

        var account = new Account
        {
            Login = dto.Login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Description = dto.Description,
            UserId = dto.UserId,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0,
            RightsLinks = [],
            Rights = []
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return account;
    }

    public async Task<Account> UpdateAsync(int id, UpdateAccountDto dto, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Аккаунт с ID {id} не найден");

        if (dto.Login != null)
        {
            var exists = await _db.Accounts.AnyAsync(a => a.Login == dto.Login && a.Id != id && a.ArchiveLevel == 0, ct);
            if (exists)
            {
                throw new BusinessException("Аккаунт с таким логином уже существует", "ACCOUNT_EXISTS", 409);
            }

            account.Login = dto.Login;
        }

        if (dto.Password != null)
        {
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        if (dto.Description != null)
        {
            account.Description = dto.Description;
        }

        if (dto.UserId.HasValue)
        {
            account.UserId = dto.UserId;
        }

        account.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return account;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Аккаунт с ID {id} не найден");

        account.ArchiveLevel = 1;
        account.ArchiveTime = DateTime.UtcNow;
        account.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRightsAsync(int accountId, List<int> rightIds, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Аккаунт с ID {accountId} не найден");

        var existingLinks = await _db.AccountRightsLinks
            .Where(l => l.AccountId == accountId && rightIds.Contains(l.RightId))
            .Select(l => l.RightId)
            .ToListAsync(ct);

        var newRightIds = rightIds.Except(existingLinks).ToList();

        foreach (var rightId in newRightIds)
        {
            _db.AccountRightsLinks.Add(new AccountRightsLink
            {
                AccountId = accountId,
                RightId = rightId,
                CreateTime = DateTime.UtcNow,
                ArchiveLevel = 0
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveRightsAsync(int accountId, List<int> rightIds, CancellationToken ct = default)
    {
        var links = await _db.AccountRightsLinks
            .Where(l => l.AccountId == accountId && rightIds.Contains(l.RightId) && l.ArchiveLevel == 0)
            .ToListAsync(ct);

        foreach (var link in links)
        {
            link.ArchiveLevel = 1;
            link.ArchiveTime = DateTime.UtcNow;
            link.ChangeTime = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Rights>> GetAccountRightsAsync(int accountId, CancellationToken ct = default)
    {
        return await _db.AccountRightsLinks
            .AsNoTracking()
            .Where(l => l.AccountId == accountId && l.ArchiveLevel == 0)
            .Include(l => l.Rights)
            .Select(l => l.Rights)
            .ToListAsync(ct);
    }
}
