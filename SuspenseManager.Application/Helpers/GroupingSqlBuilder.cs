using Common.Exceptions;
using Microsoft.Data.SqlClient;

namespace Application.Helpers;

/// <summary>
/// Строит параметризованные SQL-запросы для динамической группировки суспенсов.
/// Использует whitelist столбцов для защиты от SQL injection.
/// </summary>
public static class GroupingSqlBuilder
{
    /// <summary>
    /// Маппинг столбцов для статуса 0 (NoProduct): все из SuspenseLine
    /// </summary>
    private static readonly Dictionary<string, string> NoProductColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Isrc"] = "s.[Isrc]",
        ["Barcode"] = "s.[Barcode]",
        ["CatalogNumber"] = "s.[CatalogNumber]",
        ["Artist"] = "s.[Artist]",
        ["TrackTitle"] = "s.[TrackTitle]",
        ["Genre"] = "s.[Genre]",
        ["SenderCompany"] = "s.[SenderCompany]",
        ["RecipientCompany"] = "s.[RecipientCompany]",
        ["Operator"] = "s.[Operator]",
        ["AgreementType"] = "s.[AgreementType]",
        ["AgreementNumber"] = "s.[AgreementNumber]",
        ["TerritoryCode"] = "s.[TerritoryCode]",
    };

    /// <summary>
    /// Маппинг столбцов для статуса 1 (NoRights): продуктовые из CatalogProduct, остальные из SuspenseLine
    /// </summary>
    private static readonly Dictionary<string, string> NoRightsColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProductId"] = "s.[ProductId]",
        // Продуктовые поля из каталога
        ["Isrc"] = "cp.[Isrc]",
        ["Barcode"] = "cp.[Barcode]",
        ["CatalogNumber"] = "cp.[CatalogNumber]",
        ["ProductName"] = "cp.[ProductName]",
        ["Artist"] = "cp.[Artist]",
        // Поля из суспенса
        ["SenderCompany"] = "s.[SenderCompany]",
        ["RecipientCompany"] = "s.[RecipientCompany]",
        ["Operator"] = "s.[Operator]",
        ["AgreementType"] = "s.[AgreementType]",
        ["AgreementNumber"] = "s.[AgreementNumber]",
        ["TerritoryCode"] = "s.[TerritoryCode]",
    };

    /// <summary>
    /// Возвращает допустимые столбцы для данного статуса
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetAllowedColumns(int businessStatus)
    {
        return businessStatus == 0 ? NoProductColumns : NoRightsColumns;
    }

    /// <summary>
    /// Валидация запроса группировки
    /// </summary>
    public static void ValidateRequest(int businessStatus, List<string> groupByColumns)
    {
        if (businessStatus is not (0 or 1))
        {
            throw new BusinessException("BusinessStatus должен быть 0 или 1", "INVALID_STATUS");
        }

        if (groupByColumns.Count == 0)
        {
            throw new BusinessException("Необходимо указать хотя бы один столбец для группировки", "NO_COLUMNS");
        }

        var allowed = GetAllowedColumns(businessStatus);

        foreach (var col in groupByColumns)
        {
            if (!allowed.ContainsKey(col))
            {
                throw new BusinessException(
                    $"Столбец '{col}' не допустим для группировки при статусе {businessStatus}. " +
                    $"Допустимые: {string.Join(", ", allowed.Keys)}",
                    "INVALID_COLUMN");
            }
        }

        if (businessStatus == 1 && !groupByColumns.Any(c => c.Equals("ProductId", StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessException(
                "Для статуса 1 (нет прав) группировка по ProductId обязательна",
                "PRODUCT_ID_REQUIRED");
        }
    }

    /// <summary>
    /// Строит SQL для предпросмотра группировки (SELECT ... GROUP BY ... с пагинацией)
    /// </summary>
    public static (string sql, string countSql, List<SqlParameter> parameters) BuildPreviewSql(
        int businessStatus,
        List<string> groupByColumns,
        Dictionary<string, string>? filters,
        string? sortBy,
        string sortDirection,
        int offset,
        int pageSize)
    {
        var allowed = GetAllowedColumns(businessStatus);
        var parameters = new List<SqlParameter>();
        var paramIndex = 0;

        // SELECT columns
        var selectColumns = groupByColumns
            .Select(col => $"{allowed[col]} AS [{col}]")
            .ToList();
        selectColumns.Add("COUNT(*) AS [Count]");

        var selectClause = string.Join(", ", selectColumns);

        // FROM + JOIN
        var fromClause = businessStatus == 0
            ? "FROM [SuspenseLines] s"
            : "FROM [SuspenseLines] s INNER JOIN [CatalogProducts] cp ON s.[ProductId] = cp.[Id]";

        // WHERE — базовые условия
        var whereConditions = new List<string>
        {
            $"s.[BusinessStatus] = @p{paramIndex}",
            $"s.[ArchiveLevel] = 0",
            $"s.[GroupId] IS NULL"
        };
        parameters.Add(new SqlParameter($"@p{paramIndex}", businessStatus));
        paramIndex++;

        // WHERE — динамические фильтры
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Value))
                {
                    continue;
                }

                var (propName, op) = ParseFilterKey(filter.Key);
                if (!allowed.ContainsKey(propName))
                {
                    continue;
                }

                var sqlCol = allowed[propName];
                var paramName = $"@p{paramIndex}";

                var condition = BuildFilterCondition(sqlCol, paramName, op, filter.Value, parameters, ref paramIndex);
                if (condition != null)
                {
                    whereConditions.Add(condition);
                }
            }
        }

        var whereClause = "WHERE " + string.Join(" AND ", whereConditions);

        // GROUP BY
        var groupByExpressions = groupByColumns
            .Select(col => allowed[col])
            .ToList();
        var groupByClause = "GROUP BY " + string.Join(", ", groupByExpressions);

        // ORDER BY
        string orderByClause;
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            if (sortBy.Equals("Count", StringComparison.OrdinalIgnoreCase))
            {
                orderByClause = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? "ORDER BY COUNT(*) DESC"
                    : "ORDER BY COUNT(*) ASC";
            }
            else if (allowed.ContainsKey(sortBy))
            {
                var dir = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderByClause = $"ORDER BY {allowed[sortBy]} {dir}";
            }
            else
            {
                orderByClause = "ORDER BY COUNT(*) DESC";
            }
        }
        else
        {
            orderByClause = "ORDER BY COUNT(*) DESC";
        }

        // Основной запрос с пагинацией
        var sql = $"""
            SELECT {selectClause}
            {fromClause}
            {whereClause}
            {groupByClause}
            {orderByClause}
            OFFSET @pOffset ROWS FETCH NEXT @pPageSize ROWS ONLY
            """;

        parameters.Add(new SqlParameter("@pOffset", offset));
        parameters.Add(new SqlParameter("@pPageSize", pageSize));

        // COUNT запрос (количество групп)
        var countSql = $"""
            SELECT COUNT(*) FROM (
                SELECT {groupByExpressions[0]}
                {fromClause}
                {whereClause}
                {groupByClause}
            ) AS grouped
            """;

        return (sql, countSql, parameters);
    }

    /// <summary>
    /// Строит WHERE-условия для поиска суспенсов конкретной группы (при коммите)
    /// </summary>
    public static (string whereClause, List<SqlParameter> parameters) BuildCommitWhereClause(
        int businessStatus,
        List<string> groupByColumns,
        Dictionary<string, string?> keyValues)
    {
        var allowed = GetAllowedColumns(businessStatus);
        var parameters = new List<SqlParameter>();
        var conditions = new List<string>();
        var paramIndex = 0;

        foreach (var col in groupByColumns)
        {
            if (!keyValues.TryGetValue(col, out var value))
            {
                throw new BusinessException(
                    $"Значение для столбца '{col}' не указано в KeyValues",
                    "MISSING_KEY_VALUE");
            }

            var sqlCol = allowed[col];
            var paramName = $"@p{paramIndex}";

            if (value == null)
            {
                conditions.Add($"{sqlCol} IS NULL");
            }
            else
            {
                conditions.Add($"{sqlCol} = {paramName}");
                parameters.Add(new SqlParameter(paramName, value));
            }
            paramIndex++;
        }

        var whereClause = string.Join(" AND ", conditions);
        return (whereClause, parameters);
    }

    private static (string propertyName, string operation) ParseFilterKey(string key)
    {
        if (key.EndsWith("_contains", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^9], "contains");
        }

        if (key.EndsWith("_gte", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^4], "gte");
        }

        if (key.EndsWith("_gt", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^3], "gt");
        }

        if (key.EndsWith("_lte", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^4], "lte");
        }

        if (key.EndsWith("_lt", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^3], "lt");
        }

        if (key.EndsWith("_from", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^5], "gte");
        }

        if (key.EndsWith("_to", StringComparison.OrdinalIgnoreCase))
        {
            return (key[..^3], "lte");
        }

        return (key, "eq");
    }

    private static string? BuildFilterCondition(
        string sqlColumn,
        string paramName,
        string operation,
        string value,
        List<SqlParameter> parameters,
        ref int paramIndex)
    {
        switch (operation)
        {
            case "contains":
                parameters.Add(new SqlParameter(paramName, $"%{value}%"));
                paramIndex++;
                return $"{sqlColumn} LIKE {paramName}";

            case "eq":
                parameters.Add(new SqlParameter(paramName, value));
                paramIndex++;
                return $"{sqlColumn} = {paramName}";

            case "gt":
                parameters.Add(new SqlParameter(paramName, value));
                paramIndex++;
                return $"{sqlColumn} > {paramName}";

            case "gte":
                parameters.Add(new SqlParameter(paramName, value));
                paramIndex++;
                return $"{sqlColumn} >= {paramName}";

            case "lt":
                parameters.Add(new SqlParameter(paramName, value));
                paramIndex++;
                return $"{sqlColumn} < {paramName}";

            case "lte":
                parameters.Add(new SqlParameter(paramName, value));
                paramIndex++;
                return $"{sqlColumn} <= {paramName}";

            default:
                return null;
        }
    }
}
