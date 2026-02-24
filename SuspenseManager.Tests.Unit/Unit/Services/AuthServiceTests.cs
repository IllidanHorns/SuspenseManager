using Application.Services;
using Common.DTOs;
using Common.Exceptions;
using Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models;

namespace SuspenseManager.Tests.Unit.Unit.Services;

/// <summary>
/// Модульные тесты AuthService.
/// Покрывают: логин, генерацию JWT, ротацию refresh-токенов, отзыв токенов, безопасность.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly SuspenseManagerDbContext _db;
    private readonly AuthService _service;
    private const string TestLogin = "testuser";
    private const string TestPassword = "Secret123!";

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<SuspenseManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new SuspenseManagerDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "SuperSecretKeyForTestingPurposes_32chars!!",
                ["Jwt:Issuer"] = "SuspenseManager",
                ["Jwt:Audience"] = "SuspenseManagerClient",
                ["Jwt:AccessExpirationMinutes"] = "15",
                ["Jwt:RefreshExpirationDays"] = "7"
            })
            .Build();

        _service = new AuthService(_db, config);
    }

    public void Dispose() => _db.Dispose();

    // ──────────────────────── Helpers ────────────────────────────────────────

    private async Task<Account> CreateAccountAsync(
        string login = TestLogin,
        string password = TestPassword,
        bool archived = false)
    {
        var account = new Account
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            ArchiveLevel = archived ? 1 : 0,
            CreateTime = DateTime.UtcNow
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    private async Task<string> LoginAndGetRefreshTokenAsync()
    {
        await CreateAccountAsync();
        var response = await _service.LoginAsync(new LoginDto
        {
            Login = TestLogin,
            Password = TestPassword
        });
        return response.RefreshToken;
    }

    // ──────────────────────── LoginAsync ─────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenResponse()
    {
        await CreateAccountAsync();

        var result = await _service.LoginAsync(new LoginDto
        {
            Login = TestLogin,
            Password = TestPassword
        });

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ValidCredentials_AccessTokenIsJwtFormat()
    {
        await CreateAccountAsync();

        var result = await _service.LoginAsync(new LoginDto
        {
            Login = TestLogin,
            Password = TestPassword
        });

        // JWT состоит из трёх частей, разделённых точками
        result.AccessToken.Split('.').Should().HaveCount(3,
            "JWT должен иметь формат header.payload.signature");
    }

    [Fact]
    public async Task Login_ValidCredentials_AccessTokenExpiresInFuture()
    {
        await CreateAccountAsync();

        var result = await _service.LoginAsync(new LoginDto
        {
            Login = TestLogin,
            Password = TestPassword
        });

        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ValidCredentials_RefreshTokenStoredInDb()
    {
        await CreateAccountAsync();

        await _service.LoginAsync(new LoginDto
        {
            Login = TestLogin,
            Password = TestPassword
        });

        (await _db.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws_INVALID_CREDENTIALS()
    {
        await CreateAccountAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LoginAsync(new LoginDto
            {
                Login = TestLogin,
                Password = "WrongPassword!"
            }));

        ex.BusinessCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WrongLogin_Throws_INVALID_CREDENTIALS()
    {
        await CreateAccountAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LoginAsync(new LoginDto
            {
                Login = "nonexistent",
                Password = TestPassword
            }));

        ex.BusinessCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_ArchivedAccount_Throws_INVALID_CREDENTIALS()
    {
        // Архивированный аккаунт должен возвращать ту же ошибку — не раскрываем информацию
        await CreateAccountAsync(archived: true);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.LoginAsync(new LoginDto
            {
                Login = TestLogin,
                Password = TestPassword
            }));

        ex.BusinessCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_ReturnsAccountId_And_Login()
    {
        var account = await CreateAccountAsync();

        var result = await _service.LoginAsync(new LoginDto
        {
            Login = TestLogin,
            Password = TestPassword
        });

        result.AccountId.Should().Be(account.Id);
        result.Login.Should().Be(TestLogin);
    }

    [Fact]
    public async Task Login_WithPermissions_ReturnsPermissionsInToken()
    {
        var account = await CreateAccountAsync();
        var right = new Rights
        {
            Code = "uploads.view",
            Name = "View Uploads",
            CreateTime = DateTime.UtcNow
        };
        _db.Rights.Add(right);
        await _db.SaveChangesAsync();

        _db.AccountRightsLinks.Add(new AccountRightsLink
        {
            AccountId = account.Id,
            RightId = right.Id,
            ArchiveLevel = 0,
            CreateTime = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.LoginAsync(new LoginDto
        {
            Login = TestLogin,
            Password = TestPassword
        });

        result.Permissions.Should().Contain("uploads.view");
    }

    // ──────────────────────── RefreshAsync ───────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokenPair()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        var result = await _service.RefreshAsync(refreshToken);

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBe(refreshToken, "токен должен быть ротирован");
    }

    [Fact]
    public async Task Refresh_ValidToken_OldTokenRevoked()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        await _service.RefreshAsync(refreshToken);

        var oldToken = await _db.RefreshTokens.FirstAsync(t => t.Token == refreshToken);
        oldToken.IsRevoked.Should().BeTrue("старый токен должен быть отозван при ротации");
        oldToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_ValidToken_OldTokenHasReplacedByToken()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        var result = await _service.RefreshAsync(refreshToken);

        var oldToken = await _db.RefreshTokens.FirstAsync(t => t.Token == refreshToken);
        oldToken.ReplacedByToken.Should().Be(result.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Throws_EXPIRED_REFRESH_TOKEN()
    {
        var account = await CreateAccountAsync();

        var expiredToken = new RefreshToken
        {
            Token = "expired_token_value",
            AccountId = account.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // уже истёк
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        _db.RefreshTokens.Add(expiredToken);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.RefreshAsync("expired_token_value"));

        ex.BusinessCode.Should().Be("EXPIRED_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_RevokedToken_Throws_REVOKED_REFRESH_TOKEN()
    {
        var account = await CreateAccountAsync();

        var revokedToken = new RefreshToken
        {
            Token = "revoked_token_value",
            AccountId = account.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddHours(-1) // отозван
        };
        _db.RefreshTokens.Add(revokedToken);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.RefreshAsync("revoked_token_value"));

        ex.BusinessCode.Should().Be("REVOKED_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_InvalidToken_Throws_INVALID_REFRESH_TOKEN()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.RefreshAsync("completely_fake_token"));

        ex.BusinessCode.Should().Be("INVALID_REFRESH_TOKEN");
    }

    // ──────────────────────── RevokeAsync ────────────────────────────────────

    [Fact]
    public async Task Revoke_ActiveToken_RevokesIt()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        await _service.RevokeAsync(refreshToken);

        var token = await _db.RefreshTokens.FirstAsync(t => t.Token == refreshToken);
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Revoke_AlreadyRevokedToken_DoesNotThrow()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();
        await _service.RevokeAsync(refreshToken);

        // Повторный отзыв не должен бросать исключение
        var act = () => _service.RevokeAsync(refreshToken);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Revoke_InvalidToken_Throws_INVALID_REFRESH_TOKEN()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _service.RevokeAsync("nonexistent_token"));

        ex.BusinessCode.Should().Be("INVALID_REFRESH_TOKEN");
    }
}
