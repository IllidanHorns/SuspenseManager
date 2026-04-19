using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface IBackOfficeService
{
    /// <summary>Список активных заданий BO с пагинацией. Фильтр по типу (120/320) необязателен.</summary>
    Task<(List<BackOfficeTaskDto> Items, int TotalCount, int TotalPages)> GetTasksAsync(
        BackOfficeTasksRequest request, CancellationToken ct = default);

    /// <summary>Детальная карточка задания с полной информацией о группе.</summary>
    Task<BackOfficeTask> GetTaskAsync(int taskId, CancellationToken ct = default);

    /// <summary>
    /// Вернуть группу оператору: 120→15, 320→16.
    /// Задание архивируется (TaskStatus = ReturnedToOperator).
    /// </summary>
    Task<SuspenseGroup> ReturnGroupAsync(int taskId, CancellationToken ct = default);

    /// <summary>
    /// Удалить группу и все связанные данные (мягкое удаление).
    /// Строки суспенсов архивируются полностью — не возвращаются в очередь.
    /// Задание архивируется (TaskStatus = GroupDeleted).
    /// </summary>
    Task DeleteGroupAsync(int taskId, CancellationToken ct = default);

    /// <summary>
    /// Привязать продукт к группе 120 (нет продукта в BO).
    /// Если у продукта есть права → 88 (задание закрывается как Completed).
    /// Если прав нет → 320 (задание остаётся открытым).
    /// </summary>
    Task<SuspenseGroup> LinkProductAsync(int taskId, int productId, CancellationToken ct = default);

    /// <summary>
    /// Завершить задание BO по группе 320 (нет прав).
    /// Создаёт CatalogProductRights из GroupMetaRights (или использует уже найденные), переводит группу в 88.
    /// Задание закрывается (TaskStatus = Completed).
    /// </summary>
    Task<SuspenseGroup> CompleteTaskAsync(int taskId, CancellationToken ct = default);

    /// <summary>Поиск возможных продуктов для привязки (для задания по группе 120).</summary>
    Task<PagedResponse<CatalogProduct>> GetPossibleProductsAsync(
        int taskId, PagedRequest request, CancellationToken ct = default);

    /// <summary>Поиск прав в каталоге для копирования (для задания по группе 320).</summary>
    Task<List<CatalogProductRights>> SearchCatalogRightsAsync(
        int taskId,
        string? artist,
        string? isrc,
        string? productName,
        string? barcode,
        string? rightsTerritoryCode,
        string? rightsDocNumber,
        bool combineProductFieldsWithAnd,
        CancellationToken ct = default);

    /// <summary>
    /// Скопировать права с другого продукта каталога в продукт группы 320.
    /// При успехе группа переходит в 88, задание закрывается (TaskStatus = Completed).
    /// </summary>
    Task<SuspenseGroup> CopyRightsToProductAsync(int taskId, int rightsId, CancellationToken ct = default);
}
