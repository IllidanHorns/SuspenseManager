namespace Common.DTOs;

public class CreateAccountDto
{
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Description { get; set; }
    public int? UserId { get; set; }
}

public class UpdateAccountDto
{
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Description { get; set; }
    public int? UserId { get; set; }
}

public class CreateUserDto
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Position { get; set; } = null!;
}

public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? MiddleName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
}

public class AccountRightsDto
{
    public List<int> RightIds { get; set; } = [];
}

public class LoginDto
{
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class TokenResponseDto
{
    public string AccessToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshExpiresAt { get; set; }
    public int AccountId { get; set; }
    public string Login { get; set; } = null!;
    public List<string> Permissions { get; set; } = [];
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = null!;
}
