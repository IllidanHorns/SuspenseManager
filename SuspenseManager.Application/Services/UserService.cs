using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Common.Extensions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly SuspenseManagerDbContext _db;

    public UserService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<User>> GetUsersAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.Account)
            .Where(u => u.ArchiveLevel == 0);

        return await query.ToPagedResponseAsync(request, ct);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Include(u => u.Account)
            .FirstOrDefaultAsync(u => u.Id == id && u.ArchiveLevel == 0, ct);
    }

    public async Task<User> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.ArchiveLevel == 0, ct);
        if (exists)
            throw new BusinessException("Пользователь с таким email уже существует", "USER_EXISTS", 409);

        var user = new User
        {
            Name = dto.Name,
            Surname = dto.Surname,
            MiddleName = dto.MiddleName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Position = dto.Position,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Пользователь с ID {id} не найден");

        if (dto.Name != null) user.Name = dto.Name;
        if (dto.Surname != null) user.Surname = dto.Surname;
        if (dto.MiddleName != null) user.MiddleName = dto.MiddleName;
        if (dto.Email != null)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id && u.ArchiveLevel == 0, ct);
            if (exists)
                throw new BusinessException("Пользователь с таким email уже существует", "USER_EXISTS", 409);
            user.Email = dto.Email;
        }
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.Position != null) user.Position = dto.Position;

        user.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Пользователь с ID {id} не найден");

        user.ArchiveLevel = 1;
        user.ArchiveTime = DateTime.UtcNow;
        user.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
