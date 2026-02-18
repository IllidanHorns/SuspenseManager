using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Common.DTOs;
using Common.Exceptions;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Models;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly SuspenseManagerDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(SuspenseManagerDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .Include(a => a.RightsLinks)
                .ThenInclude(rl => rl.Rights)
            .FirstOrDefaultAsync(a => a.Login == dto.Login && a.ArchiveLevel == 0, ct)
            ?? throw new BusinessException("Неверный логин или пароль", "INVALID_CREDENTIALS", 401);

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
        {
            throw new BusinessException("Неверный логин или пароль", "INVALID_CREDENTIALS", 401);
        }

        var permissions = account.RightsLinks
            .Where(rl => rl.ArchiveLevel == 0)
            .Select(rl => rl.Rights.Code)
            .ToList();

        var accessToken = GenerateJwtToken(account, permissions);
        var refreshToken = await GenerateRefreshTokenAsync(account.Id, ct);

        return new TokenResponseDto
        {
            AccessToken = accessToken.Token,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshExpiresAt = refreshToken.ExpiresAt,
            AccountId = account.Id,
            Login = account.Login,
            Permissions = permissions
        };
    }

    public async Task<TokenResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await _db.RefreshTokens
            .Include(rt => rt.Account)
                .ThenInclude(a => a.RightsLinks)
                    .ThenInclude(rl => rl.Rights)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct)
            ?? throw new BusinessException("Невалидный refresh токен", "INVALID_REFRESH_TOKEN", 401);

        if (storedToken.IsRevoked)
        {
            throw new BusinessException("Refresh токен был отозван", "REVOKED_REFRESH_TOKEN", 401);
        }

        if (storedToken.IsExpired)
        {
            throw new BusinessException("Refresh токен истёк", "EXPIRED_REFRESH_TOKEN", 401);
        }

        var account = storedToken.Account;
        if (account.ArchiveLevel != 0)
        {
            throw new BusinessException("Аккаунт деактивирован", "ACCOUNT_DISABLED", 401);
        }

        var permissions = account.RightsLinks
            .Where(rl => rl.ArchiveLevel == 0)
            .Select(rl => rl.Rights.Code)
            .ToList();

        // Rotation: отзываем старый, создаём новый
        var newRefreshToken = await RotateRefreshTokenAsync(storedToken, ct);
        var accessToken = GenerateJwtToken(account, permissions);

        return new TokenResponseDto
        {
            AccessToken = accessToken.Token,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = newRefreshToken.Token,
            RefreshExpiresAt = newRefreshToken.ExpiresAt,
            AccountId = account.Id,
            Login = account.Login,
            Permissions = permissions
        };
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct)
            ?? throw new BusinessException("Невалидный refresh токен", "INVALID_REFRESH_TOKEN", 400);

        if (!storedToken.IsActive)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(Account account, List<string> permissions)
    {
        var secretKey = _config["Jwt:SecretKey"] ?? "SuspenseManagerDefaultSecretKey_ChangeInProduction_32chars!";
        var issuer = _config["Jwt:Issuer"] ?? "SuspenseManager";
        var audience = _config["Jwt:Audience"] ?? "SuspenseManagerClient";
        var expirationMinutes = int.TryParse(_config["Jwt:AccessExpirationMinutes"], out var mins) ? mins : 15;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Login),
            new("account_id", account.Id.ToString())
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(int accountId, CancellationToken ct)
    {
        var refreshDays = int.TryParse(_config["Jwt:RefreshExpirationDays"], out var days) ? days : 7;

        var refreshToken = new RefreshToken
        {
            Token = GenerateSecureToken(),
            AccountId = accountId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        return refreshToken;
    }

    private async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, CancellationToken ct)
    {
        var refreshDays = int.TryParse(_config["Jwt:RefreshExpirationDays"], out var days) ? days : 7;
        var newTokenValue = GenerateSecureToken();

        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.ReplacedByToken = newTokenValue;

        var newToken = new RefreshToken
        {
            Token = newTokenValue,
            AccountId = oldToken.AccountId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(ct);

        return newToken;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
