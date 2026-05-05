namespace SuspenseManager.Middleware;

public static class PermissionCodes
{
    public static readonly string[] AnyWorkAccess =
    [
        UploadsView,
        GroupingView,
        GroupsNoProductView,
        GroupsNoRightsView,
        PostponedView,
        BackofficeView,
        CatalogView,
        AdminUsersManage,
        AdminPermissionsManage
    ];

    public const string UploadsView = "uploads.view";
    public const string UploadsCreate = "uploads.create";

    public const string GroupingView = "grouping.view";
    public const string GroupingCreate = "grouping.create";

    public const string GroupsNoProductView = "groups.no_product.view";
    public const string GroupsNoProductCatalogFast = "groups.no_product.catalog_fast";
    public const string GroupsNoProductPossibleProducts = "groups.no_product.possible_products";
    public const string GroupsNoProductSendBackoffice = "groups.no_product.send_backoffice";
    public const string GroupsNoProductPostpone = "groups.no_product.postpone";
    public const string GroupsNoProductUngroup = "groups.no_product.ungroup";

    public const string GroupsNoRightsView = "groups.no_rights.view";
    public const string GroupsNoRightsCorrectRights = "groups.no_rights.correct_rights";
    public const string GroupsNoRightsSendBackoffice = "groups.no_rights.send_backoffice";
    public const string GroupsNoRightsPostpone = "groups.no_rights.postpone";
    public const string GroupsNoRightsUngroup = "groups.no_rights.ungroup";

    public const string PostponedView = "postponed.view";
    public const string PostponedReturn = "postponed.return";

    public const string BackofficeView = "backoffice.view";
    public const string BackofficeReturn = "backoffice.return";
    public const string BackofficeValidate = "backoffice.validate";
    public const string BackofficeDelete = "backoffice.delete";
    public const string BackofficeLinkProduct = "backoffice.link_product";
    public const string BackofficeCopyRights = "backoffice.copy_rights";

    public const string CatalogView = "catalog.view";
    public const string CatalogProductsEdit = "catalog.products.edit";
    public const string CatalogRightsEdit = "catalog.rights.edit";
    public const string CatalogCompaniesEdit = "catalog.companies.edit";
    public const string CatalogTerritoriesEdit = "catalog.territories.edit";

    public const string GroupsExport = "groups.export";

    public const string AdminUsersManage = "admin.users.manage";
    public const string AdminPermissionsManage = "admin.permissions.manage";
}
