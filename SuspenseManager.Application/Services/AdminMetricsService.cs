using Application.Interfaces;
using Common.DTOs;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class AdminMetricsService : IAdminMetricsService
{
    private readonly SuspenseManagerDbContext _db;

    public AdminMetricsService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<AdminMetricsDto> GetMetricsAsync(CancellationToken ct = default)
    {
        var usersTotal = await _db.Users.CountAsync(u => u.ArchiveLevel == 0, ct);
        var accountsTotal = await _db.Accounts.CountAsync(a => a.ArchiveLevel == 0, ct);
        var accountsWithoutUser = await _db.Accounts.CountAsync(a => a.ArchiveLevel == 0 && a.UserId == null, ct);
        var rightsTotal = await _db.Rights.CountAsync(r => r.ArchiveLevel == 0, ct);

        var usersWithoutAccount = await _db.Users
            .CountAsync(
                u => u.ArchiveLevel == 0
                     && !_db.Accounts.Any(a => a.ArchiveLevel == 0 && a.UserId == u.Id),
                ct);

        return new AdminMetricsDto
        {
            UsersTotal = usersTotal,
            AccountsTotal = accountsTotal,
            AccountsWithoutUserProfile = accountsWithoutUser,
            UsersWithoutAccount = usersWithoutAccount,
            RightsTotal = rightsTotal
        };
    }
}
