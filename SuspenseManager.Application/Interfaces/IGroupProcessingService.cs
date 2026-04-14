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

    // Валидация группы (16 → 88): проверяет права в каталоге, при отсутствии — создаёт из метаправ
    Task<SuspenseGroup> ValidateGroupAsync(int groupId, CancellationToken ct = default);

    // Поиск прав в каталоге по характеристикам продукта (для копирования в группу)
    Task<List<CatalogProductRights>> SearchCatalogRightsAsync(
        int groupId,
        string? artist,
        string? isrc,
        string? productName,
        string? barcode,
        CancellationToken ct = default);

    // Копирование прав из каталога в продукт группы + переход в статус 88
    Task<SuspenseGroup> CopyRightsToProductAsync(int groupId, int rightsId, CancellationToken ct = default);
}
