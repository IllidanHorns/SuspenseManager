using System.Linq;
using System.Text.Json;
using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Application.Services;

public class MeService : IMeService
{
    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly int[] AllowedPageSizes = [2, 5, 10, 15, 20, 50, 100];

    private readonly SuspenseManagerDbContext _db;
    private readonly IUserService _userService;

    public MeService(SuspenseManagerDbContext db, IUserService userService)
    {
        _db = db;
        _userService = userService;
    }

    public async Task<MeSettingsResponseDto> GetSettingsAsync(int accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Аккаунт {accountId} не найден");

        return MapResponse(account);
    }

    public async Task<MeSettingsResponseDto> UpdateSettingsAsync(int accountId, UpdateMeSettingsDto dto, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.ArchiveLevel == 0, ct)
            ?? throw new KeyNotFoundException($"Аккаунт {accountId} не найден");

        if (dto.Description != null)
        {
            account.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            if (account.Description != null && account.Description.Length > 1000)
            {
                throw new BusinessException("Описание не длиннее 1000 символов", "VALIDATION", 400);
            }
        }

        if (dto.Preferences != null)
        {
            account.UiPreferencesJson = JsonSerializer.Serialize(Normalize(dto.Preferences), JsonWrite);
        }

        if (dto.User != null)
        {
            if (account.UserId == null)
            {
                throw new BusinessException("К аккаунту не привязана карточка пользователя — правки профиля недоступны", "NO_USER_PROFILE", 400);
            }

            await _userService.UpdateAsync(account.UserId.Value, dto.User, ct);
        }

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword))
            {
                throw new BusinessException("Укажите текущий пароль", "PASSWORD_REQUIRED", 400);
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, account.PasswordHash))
            {
                throw new BusinessException("Неверный текущий пароль", "PASSWORD_MISMATCH", 400);
            }

            if (dto.NewPassword.Length < 6)
            {
                throw new BusinessException("Новый пароль не короче 6 символов", "PASSWORD_WEAK", 400);
            }

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        }

        account.ChangeTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetSettingsAsync(accountId, ct);
    }

    private static MeSettingsResponseDto MapResponse(Account account)
    {
        MeUserProfileDto? userDto = null;
        if (account.User != null && account.User.ArchiveLevel == 0)
        {
            var u = account.User;
            userDto = new MeUserProfileDto
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                MiddleName = u.MiddleName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Position = u.Position,
            };
        }

        return new MeSettingsResponseDto
        {
            AccountId = account.Id,
            Login = account.Login,
            Description = account.Description,
            Preferences = ParsePreferences(account.UiPreferencesJson),
            User = userDto,
        };
    }

    private static UserUiPreferencesDto ParsePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Normalize(new UserUiPreferencesDto());
        }

        try
        {
            var p = JsonSerializer.Deserialize<UserUiPreferencesDto>(json, JsonRead);
            return Normalize(p ?? new UserUiPreferencesDto());
        }
        catch
        {
            return Normalize(new UserUiPreferencesDto());
        }
    }

    private static readonly string[] AllowedColorSchemes = ["light", "dark", "auto"];
    private static readonly string[] AllowedNotificationPositions =
        ["top-right", "top-left", "bottom-right", "bottom-left", "top-center", "bottom-center"];

    private static UserUiPreferencesDto Normalize(UserUiPreferencesDto p)
    {
        if (!AllowedPageSizes.Contains(p.DefaultTablePageSize))
            p.DefaultTablePageSize = 20;

        if (string.IsNullOrWhiteSpace(p.ColorScheme)
            || !AllowedColorSchemes.Any(x => string.Equals(x, p.ColorScheme, StringComparison.OrdinalIgnoreCase)))
            p.ColorScheme = "light";
        else
            p.ColorScheme = AllowedColorSchemes.First(x => string.Equals(x, p.ColorScheme, StringComparison.OrdinalIgnoreCase));

        var pos = (p.NotificationsPosition ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-');
        if (string.IsNullOrEmpty(pos) || !AllowedNotificationPositions.Contains(pos))
            p.NotificationsPosition = "top-right";
        else
            p.NotificationsPosition = pos;

        return p;
    }
}
