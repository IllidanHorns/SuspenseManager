using FluentAssertions;
using Models;

namespace SuspenseManager.Tests.Unit.Unit.Models;

/// <summary>
/// Модульные тесты модели RefreshToken.
/// Проверяют вычисляемые свойства: IsExpired, IsRevoked, IsActive.
/// </summary>
public class RefreshTokenTests
{
    [Fact]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_PastExpiry_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_NullRevokedAt_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WithRevokedAt_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ValidToken_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ExpiredToken_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_RevokedToken_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ExpiredAndRevokedToken_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            RevokedAt = DateTime.UtcNow.AddDays(-2)
        };

        token.IsActive.Should().BeFalse();
    }
}
