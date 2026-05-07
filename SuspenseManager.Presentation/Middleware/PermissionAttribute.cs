using Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SuspenseManager.Middleware;

/// <summary>
/// Атрибут для проверки наличия права доступа у текущего пользователя.
/// При нескольких правах — доступ открыт, если есть ХОТЯ БЫ ОДНО из перечисленных.
/// Использование: [RequirePermission("groups.no_product.postpone", "groups.no_rights.postpone")]
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _permissions;

    public RequirePermissionAttribute(params string[] permissions)
    {
        _permissions = permissions;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.Fail(401, "Требуется авторизация", "UNAUTHORIZED"));
            return;
        }

        var userPermissions = user.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToHashSet();

        if (!_permissions.Any(p => userPermissions.Contains(p)))
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Fail(403, "Недостаточно прав для выполнения этого действия", "FORBIDDEN"))
            {
                StatusCode = 403
            };
        }
    }
}
