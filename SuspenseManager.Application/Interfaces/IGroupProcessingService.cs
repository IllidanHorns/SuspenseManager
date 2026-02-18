using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface IGroupProcessingService
{
    // п.14 — обновление метаданных продукта (нет продукта)
    Task<GroupMetadata> UpdateMetadataAsync(int groupId, UpdateGroupMetadataDto dto, CancellationToken ct = default);

    // п.15 — обновление метаданных прав (нет прав)
    Task<GroupMetaRights> UpdateMetaRightsAsync(int groupId, UpdateGroupMetaRightsDto dto, CancellationToken ct = default);

    // п.16 — быстрая каталогизация
    Task<CatalogProduct> QuickCatalogAsync(int groupId, CancellationToken ct = default);

    // п.17 — возможные продукты
    Task<PagedResponse<CatalogProduct>> GetPossibleProductsAsync(int groupId, PagedRequest request, CancellationToken ct = default);

    // п.20 — отправка в бэк-офис
    Task<SuspenseGroup> SendToBackOfficeAsync(int groupId, SendToBackOfficeDto dto, CancellationToken ct = default);

    // п.21 — отложить
    Task<SuspenseGroup> PostponeAsync(int groupId, PostponeGroupDto dto, CancellationToken ct = default);

    // п.22 — выгрузка метаданных группы
    Task<GroupMetadata?> GetMetadataAsync(int groupId, CancellationToken ct = default);

    // п.23 — выгрузка метаправ группы
    Task<GroupMetaRights?> GetMetaRightsAsync(int groupId, CancellationToken ct = default);

    // п.24 — разгруппировка
    Task UngroupAsync(int groupId, CancellationToken ct = default);

    // п.25 — привязка группы к продукту
    Task<SuspenseGroup> LinkProductAsync(int groupId, LinkProductDto dto, CancellationToken ct = default);

    // Отложенные группы
    Task<PagedResponse<SuspenseGroup>> GetPostponedGroupsAsync(PagedRequest request, CancellationToken ct = default);

    // Возврат из отложенных
    Task<SuspenseGroup> ReturnFromPostponedAsync(int groupId, CancellationToken ct = default);

    // Фиксация группы — переводит группу и все суспенсы в статус Validated (88)
    Task<SuspenseGroup> ValidateGroupAsync(int groupId, CancellationToken ct = default);
}
