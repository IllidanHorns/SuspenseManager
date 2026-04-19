namespace Models;

/// <summary>
/// Задание в бэк-офисе. Создаётся при отправке группы в BO.
/// Одна группа — не более одного активного задания (ArchiveLevel == 0).
/// </summary>
public class BackOfficeTask
{
    public int Id { get; set; }

    /// <summary>
    /// Группа, к которой относится задание
    /// </summary>
    public int GroupId { get; set; }
    public SuspenseGroup Group { get; set; } = null!;

    /// <summary>
    /// Оператор, отправивший задание в BO
    /// </summary>
    public int CreatedByAccountId { get; set; }
    public Account CreatedBy { get; set; } = null!;

    /// <summary>
    /// Описание проблемы от оператора (обязательно при отправке)
    /// </summary>
    public string ProblemDescription { get; set; } = string.Empty;

    /// <summary>
    /// Статус задания: 0=Open, 1=ReturnedToOperator, 2=GroupDeleted, 3=Completed
    /// </summary>
    public int TaskStatus { get; set; }

    public DateTime CreateTime { get; set; }
    public DateTime? ChangeTime { get; set; }
    public int ArchiveLevel { get; set; }
    public DateTime? ArchiveTime { get; set; }
}
