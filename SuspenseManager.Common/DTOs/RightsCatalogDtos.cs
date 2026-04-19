namespace Common.DTOs;

/// <summary>Элемент справочника прав для UI админки.</summary>
public class RightCatalogItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Module { get; set; }
}

/// <summary>Готовый пресет роли: набор идентификаторов прав из БД.</summary>
public class RolePresetDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public IReadOnlyList<int> RightIds { get; set; } = [];
}

/// <summary>Полная замена набора прав аккаунта.</summary>
public class ReplaceAccountRightsDto
{
    public List<int> RightIds { get; set; } = [];
}
