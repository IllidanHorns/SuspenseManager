namespace Models;

public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>
    /// Значение токена (уникальная строка)
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// FK на аккаунт
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Навигация на аккаунт
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Время истечения
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Время создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Время отзыва (null = активен)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Токен, который заменил данный (rotation)
    /// </summary>
    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
