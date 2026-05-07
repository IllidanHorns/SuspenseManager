namespace Common.DTOs;

/// <summary>
/// Сводная строка по оператору: агрегированная статистика по всем его активным группам.
/// </summary>
public class OperatorMonitorDto
{
    public int    AccountId           { get; set; }
    public string Login               { get; set; } = string.Empty;
    /// <summary>Фамилия Имя, если привязан пользователь</summary>
    public string? FullName           { get; set; }
    public int    ActiveGroupsCount   { get; set; }   // статусы 15, 16
    public int    PostponedGroupsCount { get; set; }  // статусы 30, 32
    public int    BackOfficeGroupsCount { get; set; } // статусы 120, 320
    public int    TotalSuspensesCount { get; set; }   // строк во всех группах
    public int    OldestGroupAgeDays  { get; set; }   // максимальный возраст группы (дней с CreateTime)
    public int    WarningGroupsCount  { get; set; }
    public int    CriticalGroupsCount { get; set; }
}

/// <summary>
/// Детальная строка по одной группе оператора.
/// </summary>
public class OperatorGroupDto
{
    public int    GroupId                { get; set; }
    public int    BusinessStatus         { get; set; }
    public int    SuspenseCount          { get; set; }
    public int    AgeDays                { get; set; }
    public int    DaysSinceLastActivity  { get; set; }
    public string LastActivityTime       { get; set; } = string.Empty;
    /// <summary>"none" | "warning" | "critical"</summary>
    public string FlagLevel              { get; set; } = "none";
    public string? FlagReason            { get; set; }
    public string? Artist                { get; set; }
    public string? Title                 { get; set; }
    public string? Isrc                  { get; set; }
}
