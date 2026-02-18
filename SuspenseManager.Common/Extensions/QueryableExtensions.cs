using System.Linq.Expressions;
using System.Reflection;
using Common.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Common.Extensions;

/// <summary>
/// Extension-методы для IQueryable: динамическая фильтрация, сортировка, пагинация.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Применяет пагинацию, сортировку и фильтрацию из PagedRequest.
    /// Возвращает PagedResponse с данными.
    /// </summary>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        PagedRequest request,
        CancellationToken ct = default) where T : class
    {
        query = query.ApplyFilters(request.Filters);
        query = query.ApplySorting(request.SortBy, request.SortDirection);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Динамическая сортировка по имени свойства
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        string? sortBy,
        string sortDirection = "asc") where T : class
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query;
        }

        var property = typeof(T).GetProperty(sortBy,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? "OrderByDescending"
            : "OrderBy";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), property.PropertyType],
            query.Expression,
            Expression.Quote(lambda));

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    /// <summary>
    /// Динамическая фильтрация по словарю фильтров.
    /// Поддерживает суффиксы: _contains, _gt, _lt, _gte, _lte, _from, _to
    /// Без суффикса — точное совпадение (для строк — contains по умолчанию)
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> query,
        Dictionary<string, string>? filters) where T : class
    {
        if (filters == null || filters.Count == 0)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");

        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Value))
            {
                continue;
            }

            var (propertyName, operation) = ParseFilterKey(filter.Key);

            var property = typeof(T).GetProperty(propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                continue;
            }

            var expression = BuildFilterExpression(parameter, property, operation, filter.Value);
            if (expression == null)
            {
                continue;
            }

            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private static (string propertyName, FilterOperation operation) ParseFilterKey(string key)
    {
        if (key.EndsWith("_contains", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^9], FilterOperation.Contains);
        }

        if (key.EndsWith("_gt", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^3], FilterOperation.GreaterThan);
        }

        if (key.EndsWith("_gte", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^4], FilterOperation.GreaterThanOrEqual);
        }

        if (key.EndsWith("_lt", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^3], FilterOperation.LessThan);
        }

        if (key.EndsWith("_lte", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^4], FilterOperation.LessThanOrEqual);
        }

        if (key.EndsWith("_from", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^5], FilterOperation.GreaterThanOrEqual);
        }

        if (key.EndsWith("_to", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^3], FilterOperation.LessThanOrEqual);
        }

        return (key, FilterOperation.Equals);
    }

    private static Expression? BuildFilterExpression(
        ParameterExpression parameter,
        PropertyInfo property,
        FilterOperation operation,
        string value)
    {
        var propertyAccess = Expression.Property(parameter, property);
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        // Для строковых полей
        if (propertyType == typeof(string))
        {
            var constant = Expression.Constant(value);

            if (operation == FilterOperation.Contains || operation == FilterOperation.Equals)
            {
                // string.Contains — поиск по подстроке
                var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;

                // Обработка nullable: если null — false
                if (property.PropertyType == typeof(string))
                {
                    var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));
                    var contains = Expression.Call(propertyAccess, containsMethod, constant);
                    return Expression.AndAlso(notNull, contains);
                }

                return Expression.Call(propertyAccess, containsMethod, constant);
            }

            return null;
        }

        // Для числовых и дат — парсим значение
        object? parsedValue = TryParseValue(propertyType, value);
        if (parsedValue == null)
        {
            return null;
        }

        var typedConstant = Expression.Constant(parsedValue, property.PropertyType.IsGenericType
            ? property.PropertyType
            : propertyType);

        // Для nullable типов нужно привести к нужному типу
        Expression left = propertyAccess;
        Expression right = typedConstant;

        if (property.PropertyType.IsGenericType &&
            property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            left = Expression.Property(propertyAccess, "Value");
            right = Expression.Constant(parsedValue, propertyType);

            var hasValue = Expression.Property(propertyAccess, "HasValue");
            var comparison = BuildComparisonExpression(left, right, operation);
            if (comparison == null)
            {
                return null;
            }

            return Expression.AndAlso(hasValue, comparison);
        }

        return BuildComparisonExpression(left, right, operation);
    }

    private static Expression? BuildComparisonExpression(
        Expression left, Expression right, FilterOperation operation)
    {
        return operation switch
        {
            FilterOperation.Equals => Expression.Equal(left, right),
            FilterOperation.GreaterThan => Expression.GreaterThan(left, right),
            FilterOperation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
            FilterOperation.LessThan => Expression.LessThan(left, right),
            FilterOperation.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
            _ => null
        };
    }

    private static object? TryParseValue(Type type, string value)
    {
        try
        {
            if (type == typeof(int) && int.TryParse(value, out var intVal))
            {
                return intVal;
            }

            if (type == typeof(long) && long.TryParse(value, out var longVal))
            {
                return longVal;
            }

            if (type == typeof(decimal) && decimal.TryParse(value, out var decVal))
            {
                return decVal;
            }

            if (type == typeof(double) && double.TryParse(value, out var dblVal))
            {
                return dblVal;
            }

            if (type == typeof(float) && float.TryParse(value, out var fltVal))
            {
                return fltVal;
            }

            if (type == typeof(bool) && bool.TryParse(value, out var boolVal))
            {
                return boolVal;
            }

            if (type == typeof(DateTime) && DateTime.TryParse(value, out var dtVal))
            {
                return dtVal;
            }

            if (type == typeof(DateOnly) && DateOnly.TryParse(value, out var doVal))
            {
                return doVal;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private enum FilterOperation
    {
        Equals,
        Contains,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }
}
