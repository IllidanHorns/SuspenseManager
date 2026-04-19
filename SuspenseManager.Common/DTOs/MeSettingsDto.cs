namespace Common.DTOs;

/// <summary>Настройки интерфейса, хранятся в Account.UiPreferencesJson (JSON).</summary>
public class UserUiPreferencesDto
{
    /// <summary>Размер страницы таблиц по умолчанию (как в PageSizeSelect).</summary>
    public int DefaultTablePageSize { get; set; } = 20;

    /// <summary>Раскрывать фильтры по умолчанию на всех страницах.</summary>
    public bool FiltersExpandedByDefault { get; set; }

    /// <summary>Тема интерфейса: light, dark или auto (системная).</summary>
    public string ColorScheme { get; set; } = "light";

    /// <summary>Позиция всплывающих уведомлений (как у Mantine Notifications).</summary>
    public string NotificationsPosition { get; set; } = "top-right";
}

public class MeUserProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Position { get; set; } = null!;
}

public class MeSettingsResponseDto
{
    public int AccountId { get; set; }
    public string Login { get; set; } = null!;
    public string? Description { get; set; }
    public UserUiPreferencesDto Preferences { get; set; } = new();
    public MeUserProfileDto? User { get; set; }
}

public class UpdateMeSettingsDto
{
    public string? Description { get; set; }
    public UserUiPreferencesDto? Preferences { get; set; }
    public UpdateUserDto? User { get; set; }

    /// <summary>Смена пароля: укажите текущий и новый.</summary>
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
