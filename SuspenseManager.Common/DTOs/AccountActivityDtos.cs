namespace Common.DTOs;

/// <summary>Запись из объединённой ленты: логи по группам и по строкам, где исполнитель — данный аккаунт.</summary>
public class AccountActivityItemDto
{
    /// <summary>group | line</summary>
    public string Kind { get; set; } = null!;

    public int LogId { get; set; }

    /// <summary>Id группы (для kind=group) или id строки (для kind=line).</summary>
    public int EntityId { get; set; }

    /// <summary>Для строки — группа на момент события (если была).</summary>
    public int? GroupId { get; set; }

    public int? StatusFrom { get; set; }
    public string? StatusFromName { get; set; }
    public int StatusTo { get; set; }
    public string? StatusToName { get; set; }
    public DateTime OperationTime { get; set; }
}
