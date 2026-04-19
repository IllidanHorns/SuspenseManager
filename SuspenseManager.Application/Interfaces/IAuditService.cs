using Common.DTOs;

namespace Application.Interfaces;

public interface IAuditService
{
    // ── Запись событий ──────────────────────────────────────────────────────

    /// <summary>
    /// Логирует изменение статуса группы (или её создание при statusFrom = null)
    /// </summary>
    Task LogGroupAsync(int groupId, int? statusFrom, int statusTo, CancellationToken ct = default);

    /// <summary>
    /// Логирует изменение статуса всех активных строк в группе.
    /// Вызывается одновременно с LogGroupAsync при операциях над группой.
    /// </summary>
    Task LogGroupLinesAsync(int groupId, int? statusFrom, int statusTo, CancellationToken ct = default);

    /// <summary>
    /// Логирует изменение статуса одной строки суспенса.
    /// groupId — группа на момент события (может быть null при первичной загрузке).
    /// </summary>
    Task LogLineAsync(int lineId, int? groupId, int? statusFrom, int statusTo, CancellationToken ct = default);

    // ── Чтение аудита ───────────────────────────────────────────────────────

    /// <summary>
    /// Список активных групп с инфо о создателе и последнем изменении.
    /// currentAccountId нужен для фильтр�� OnlyMine.
    /// </summary>
    Task<PagedResponse<AuditGroupDto>> GetGroupsAsync(
        AuditGroupsRequest request, int currentAccountId, CancellationToken ct = default);

    /// <summary>
    /// История переходов статусов конкретной группы (от новых к старым)
    /// </summary>
    Task<PagedResponse<AuditLogEntryDto>> GetGroupLogsAsync(
        int groupId, PagedRequest request, CancellationToken ct = default);

    /// <summary>
    /// Список строк суспенсов в активных группах с последним изменением.
    /// currentAccountId нужен для фильтра OnlyMine.
    /// </summary>
    Task<PagedResponse<AuditLineDto>> GetLinesAsync(
        AuditLinesRequest request, int currentAccountId, CancellationToken ct = default);

    /// <summary>
    /// История переходов статусов конкретной строки (от новых к старым)
    /// </summary>
    Task<PagedResponse<AuditLogEntryDto>> GetLineLogsAsync(
        int lineId, PagedRequest request, CancellationToken ct = default);

    /// <summary>
    /// Хронологическая лента действий аккаунта: переходы статусов по группам и строкам (из логов аудита).
    /// </summary>
    Task<PagedResponse<AccountActivityItemDto>> GetAccountActivityAsync(
        int accountId, PagedRequest request, CancellationToken ct = default);
}
