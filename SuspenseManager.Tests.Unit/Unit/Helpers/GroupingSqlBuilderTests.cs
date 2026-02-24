using Application.Helpers;
using Common.Exceptions;
using FluentAssertions;

namespace SuspenseManager.Tests.Unit.Unit.Helpers;

/// <summary>
/// Модульные тесты для GroupingSqlBuilder.
/// Покрывают: валидацию, построение SQL, защиту от SQL-injection, фильтры, сортировку, пагинацию.
/// </summary>
public class GroupingSqlBuilderTests
{
    // ───────────────────────────── GetAllowedColumns ─────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GetAllowedColumns_ValidStatus_ReturnsNonEmptyDictionary(int status)
    {
        var columns = GroupingSqlBuilder.GetAllowedColumns(status);
        columns.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAllowedColumns_Status0_ContainsAllNoProductColumns()
    {
        var columns = GroupingSqlBuilder.GetAllowedColumns(0);

        columns.Keys.Should().Contain(new[]
        {
            "Isrc", "Barcode", "CatalogNumber", "Artist", "TrackTitle", "Genre",
            "SenderCompany", "RecipientCompany", "Operator",
            "AgreementType", "AgreementNumber", "TerritoryCode"
        });
    }

    [Fact]
    public void GetAllowedColumns_Status0_DoesNotContainProductId()
    {
        var columns = GroupingSqlBuilder.GetAllowedColumns(0);
        columns.Should().NotContainKey("ProductId");
    }

    [Fact]
    public void GetAllowedColumns_Status1_ContainsProductId()
    {
        var columns = GroupingSqlBuilder.GetAllowedColumns(1);
        columns.Should().ContainKey("ProductId");
    }

    [Fact]
    public void GetAllowedColumns_Status1_ContainsProductNameFromCatalog()
    {
        var columns = GroupingSqlBuilder.GetAllowedColumns(1);
        columns.Should().ContainKey("ProductName");
    }

    [Fact]
    public void GetAllowedColumns_Status1_DoesNotContainTrackTitle()
    {
        // TrackTitle — поле суспенса, недоступно при статусе 1 (продукт из каталога)
        var columns = GroupingSqlBuilder.GetAllowedColumns(1);
        columns.Should().NotContainKey("TrackTitle");
    }

    [Fact]
    public void GetAllowedColumns_Status1_IsrcMapsToProductTable()
    {
        var columns = GroupingSqlBuilder.GetAllowedColumns(1);
        columns["Isrc"].Should().Contain("cp.", "поле Isrc при статусе 1 берётся из таблицы CatalogProducts");
    }

    [Fact]
    public void GetAllowedColumns_Status0_IsrcMapsToSuspenseTable()
    {
        var columns = GroupingSqlBuilder.GetAllowedColumns(0);
        columns["Isrc"].Should().Contain("s.", "поле Isrc при статусе 0 берётся из таблицы SuspenseLines");
    }

    // ───────────────────────────── ValidateRequest ───────────────────────────────

    [Fact]
    public void ValidateRequest_InvalidStatus2_ThrowsWithCode_INVALID_STATUS()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            GroupingSqlBuilder.ValidateRequest(2, ["Isrc"]));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void ValidateRequest_NegativeStatus_ThrowsWithCode_INVALID_STATUS()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            GroupingSqlBuilder.ValidateRequest(-1, ["Isrc"]));

        ex.BusinessCode.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public void ValidateRequest_EmptyColumns_ThrowsWithCode_NO_COLUMNS()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            GroupingSqlBuilder.ValidateRequest(0, []));

        ex.BusinessCode.Should().Be("NO_COLUMNS");
    }

    [Fact]
    public void ValidateRequest_UnknownColumn_ThrowsWithCode_INVALID_COLUMN()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            GroupingSqlBuilder.ValidateRequest(0, ["NonExistentColumn"]));

        ex.BusinessCode.Should().Be("INVALID_COLUMN");
    }

    /// <summary>SQL-injection через имя столбца — должен быть заблокирован whitelist'ом</summary>
    [Theory]
    [InlineData("'; DROP TABLE SuspenseLines;--")]
    [InlineData("1 OR 1=1")]
    [InlineData("Isrc; DELETE FROM SuspenseLines")]
    [InlineData("(SELECT version())")]
    [InlineData("UNION SELECT * FROM Accounts")]
    public void ValidateRequest_SqlInjectionColumnName_ThrowsWithCode_INVALID_COLUMN(string maliciousColumn)
    {
        var ex = Assert.Throws<BusinessException>(() =>
            GroupingSqlBuilder.ValidateRequest(0, [maliciousColumn]));

        ex.BusinessCode.Should().Be("INVALID_COLUMN");
    }

    [Fact]
    public void ValidateRequest_Status1_WithoutProductId_ThrowsWithCode_PRODUCT_ID_REQUIRED()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            GroupingSqlBuilder.ValidateRequest(1, ["Isrc"]));

        ex.BusinessCode.Should().Be("PRODUCT_ID_REQUIRED");
    }

    [Fact]
    public void ValidateRequest_Status1_WithProductIdAndOtherColumn_DoesNotThrow()
    {
        var act = () => GroupingSqlBuilder.ValidateRequest(1, ["ProductId", "Isrc"]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRequest_Status0_ValidColumns_DoesNotThrow()
    {
        var act = () => GroupingSqlBuilder.ValidateRequest(0, ["Isrc", "Artist", "TerritoryCode"]);
        act.Should().NotThrow();
    }

    // ───────────────────────────── BuildPreviewSql ───────────────────────────────

    [Fact]
    public void BuildPreviewSql_Status0_FromClause_NoJoin()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, null, "asc", 0, 10);

        sql.Should().Contain("FROM [SuspenseLines]");
        sql.Should().NotContain("INNER JOIN", "статус 0 не требует JOIN с CatalogProducts");
    }

    [Fact]
    public void BuildPreviewSql_Status1_FromClause_HasInnerJoin()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            1, ["ProductId"], null, null, "asc", 0, 10);

        sql.Should().Contain("INNER JOIN [CatalogProducts]");
        sql.Should().Contain("s.[ProductId] = cp.[Id]");
    }

    [Fact]
    public void BuildPreviewSql_ContainsBaseWhereConditions()
    {
        var (sql, _, parameters) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, null, "asc", 0, 10);

        sql.Should().Contain("[ArchiveLevel] = 0");
        sql.Should().Contain("[GroupId] IS NULL");
        sql.Should().Contain("[BusinessStatus]");
        parameters.Should().Contain(p => (int)p.Value! == 0, "статус должен быть передан как параметр");
    }

    [Fact]
    public void BuildPreviewSql_ContainsPaginationOffsetFetch()
    {
        var (sql, _, parameters) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, null, "asc", 20, 5);

        sql.Should().Contain("OFFSET @pOffset ROWS FETCH NEXT @pPageSize ROWS ONLY");
        parameters.First(p => p.ParameterName == "@pOffset").Value.Should().Be(20);
        parameters.First(p => p.ParameterName == "@pPageSize").Value.Should().Be(5);
    }

    [Fact]
    public void BuildPreviewSql_CountSql_IsSubquery()
    {
        var (_, countSql, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, null, "asc", 0, 10);

        countSql.Should().StartWith("SELECT COUNT(*) FROM (");
        countSql.Should().Contain(") AS grouped");
    }

    // ─────────────────── BuildPreviewSql — фильтры ────────────────────────────

    [Fact]
    public void BuildPreviewSql_Filter_Contains_GeneratesLike()
    {
        var (sql, _, parameters) = BuildWithFilters(
            ["Artist"], new Dictionary<string, string> { ["Artist_contains"] = "Beatles" });

        sql.Should().Contain("LIKE");
        // Значение должно быть обёрнуто в %...%
        parameters.Should().Contain(p => p.Value!.ToString() == "%Beatles%");
    }

    [Fact]
    public void BuildPreviewSql_Filter_Eq_GeneratesEquals()
    {
        var (sql, _, _) = BuildWithFilters(
            ["Artist"], new Dictionary<string, string> { ["Artist"] = "Beatles" });

        sql.Should().Contain("= @p");
    }

    [Theory]
    [InlineData("Artist_gt",  ">")]
    [InlineData("Artist_gte", ">=")]
    [InlineData("Artist_lt",  "<")]
    [InlineData("Artist_lte", "<=")]
    [InlineData("Artist_from", ">=")]
    [InlineData("Artist_to",   "<=")]
    public void BuildPreviewSql_FilterOperator_GeneratesCorrectSqlOperator(string filterKey, string expectedOp)
    {
        var (sql, _, _) = BuildWithFilters(
            ["Artist"], new Dictionary<string, string> { [filterKey] = "testvalue" });

        sql.Should().Contain(expectedOp);
    }

    [Fact]
    public void BuildPreviewSql_FilterValue_IsParameterized_NotInlinedInSQL()
    {
        // БЕЗОПАСНОСТЬ: значение фильтра НЕ должно появляться напрямую в SQL
        const string maliciousValue = "'; DROP TABLE SuspenseLines; --";
        var (sql, _, parameters) = BuildWithFilters(
            ["Artist"], new Dictionary<string, string> { ["Artist"] = maliciousValue });

        sql.Should().NotContain(maliciousValue, "значение должно быть параметром, а не литералом в SQL");
        parameters.Should().Contain(p => p.Value!.ToString() == maliciousValue,
            "значение должно присутствовать как параметр");
    }

    [Fact]
    public void BuildPreviewSql_FilterContains_IsParameterized_NotInlinedInSQL()
    {
        const string maliciousValue = "test' OR '1'='1";
        var (sql, _, parameters) = BuildWithFilters(
            ["Artist"], new Dictionary<string, string> { ["Artist_contains"] = maliciousValue });

        sql.Should().NotContain(maliciousValue);
        parameters.Should().Contain(p => p.Value!.ToString()!.Contains(maliciousValue));
    }

    [Fact]
    public void BuildPreviewSql_UnknownFilterColumn_IsIgnoredSilently()
    {
        // Неизвестные столбцы в фильтрах должны тихо игнорироваться
        var (sql, _, _) = BuildWithFilters(
            ["Isrc"], new Dictionary<string, string> { ["__UNKNOWN__"] = "value" });

        sql.Should().NotContain("__UNKNOWN__");
    }

    [Fact]
    public void BuildPreviewSql_EmptyFilterValue_IsSkipped()
    {
        var (sql, _, parameters) = BuildWithFilters(
            ["Isrc"], new Dictionary<string, string> { ["Isrc"] = "" });

        // Пустое значение не должно добавлять дополнительное условие
        var conditionsCount = sql.Split("AND").Length;
        // базовые условия: BusinessStatus, ArchiveLevel, GroupId IS NULL — 3 AND
        conditionsCount.Should().BeLessOrEqualTo(4);
    }

    // ─────────────────── BuildPreviewSql — сортировка ─────────────────────────

    [Fact]
    public void BuildPreviewSql_SortByCount_Asc()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, "Count", "asc", 0, 10);

        sql.Should().Contain("ORDER BY COUNT(*) ASC");
    }

    [Fact]
    public void BuildPreviewSql_SortByCount_Desc()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, "Count", "desc", 0, 10);

        sql.Should().Contain("ORDER BY COUNT(*) DESC");
    }

    [Fact]
    public void BuildPreviewSql_SortByAllowedColumn_Asc()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Artist"], null, "Artist", "asc", 0, 10);

        sql.Should().Contain("ORDER BY s.[Artist] ASC");
    }

    [Fact]
    public void BuildPreviewSql_SortByAllowedColumn_Desc()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Artist"], null, "Artist", "desc", 0, 10);

        sql.Should().Contain("ORDER BY s.[Artist] DESC");
    }

    [Fact]
    public void BuildPreviewSql_SortByUnknownColumn_DefaultsToCountDesc()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, "INJECTED_COLUMN", "asc", 0, 10);

        sql.Should().Contain("ORDER BY COUNT(*)",
            "неизвестный столбец сортировки должен заменяться дефолтным");
        sql.Should().NotContain("INJECTED_COLUMN", "имя неизвестного столбца не должно попасть в SQL");
    }

    [Fact]
    public void BuildPreviewSql_NoSort_DefaultsToCountDesc()
    {
        var (sql, _, _) = GroupingSqlBuilder.BuildPreviewSql(
            0, ["Isrc"], null, null, "asc", 0, 10);

        sql.Should().Contain("ORDER BY COUNT(*) DESC");
    }

    // ────────────────── BuildCommitWhereClause ────────────────────────────────

    [Fact]
    public void BuildCommitWhereClause_TwoColumns_BuildsCorrectAndClause()
    {
        var groupByColumns = new List<string> { "Artist", "TerritoryCode" };
        var keyValues = new Dictionary<string, string?>
        {
            ["Artist"] = "The Beatles",
            ["TerritoryCode"] = "RU"
        };

        var (whereClause, parameters) = GroupingSqlBuilder.BuildCommitWhereClause(0, groupByColumns, keyValues);

        whereClause.Should().Contain("s.[Artist]");
        whereClause.Should().Contain("s.[TerritoryCode]");
        whereClause.Should().Contain("AND");
        parameters.Should().HaveCount(2);
    }

    [Fact]
    public void BuildCommitWhereClause_NullValue_GeneratesIsNullCondition()
    {
        var groupByColumns = new List<string> { "Isrc" };
        var keyValues = new Dictionary<string, string?> { ["Isrc"] = null };

        var (whereClause, parameters) = GroupingSqlBuilder.BuildCommitWhereClause(0, groupByColumns, keyValues);

        whereClause.Should().Contain("IS NULL");
        parameters.Should().BeEmpty("для IS NULL параметр не нужен");
    }

    [Fact]
    public void BuildCommitWhereClause_MissingKeyValue_ThrowsWithCode_MISSING_KEY_VALUE()
    {
        var groupByColumns = new List<string> { "Artist" };
        var keyValues = new Dictionary<string, string?>(); // Значение для "Artist" не задано

        var ex = Assert.Throws<BusinessException>(() =>
            GroupingSqlBuilder.BuildCommitWhereClause(0, groupByColumns, keyValues));

        ex.BusinessCode.Should().Be("MISSING_KEY_VALUE");
    }

    [Fact]
    public void BuildCommitWhereClause_Values_AreParameterized()
    {
        const string sensitiveValue = "'; DROP TABLE--";
        var groupByColumns = new List<string> { "Artist" };
        var keyValues = new Dictionary<string, string?> { ["Artist"] = sensitiveValue };

        var (whereClause, parameters) = GroupingSqlBuilder.BuildCommitWhereClause(0, groupByColumns, keyValues);

        whereClause.Should().NotContain(sensitiveValue, "значение не должно быть вставлено в SQL напрямую");
        parameters.Should().Contain(p => p.Value!.ToString() == sensitiveValue);
    }

    [Fact]
    public void BuildCommitWhereClause_Status1_ProductId_MapsToSuspenseTable()
    {
        var groupByColumns = new List<string> { "ProductId" };
        var keyValues = new Dictionary<string, string?> { ["ProductId"] = "42" };

        var (whereClause, _) = GroupingSqlBuilder.BuildCommitWhereClause(1, groupByColumns, keyValues);

        whereClause.Should().Contain("s.[ProductId]");
    }

    // ────────────────── Вспомогательные методы ────────────────────────────────

    private static (string sql, string countSql, List<Microsoft.Data.SqlClient.SqlParameter> parameters)
        BuildWithFilters(List<string> columns, Dictionary<string, string>? filters)
    {
        return GroupingSqlBuilder.BuildPreviewSql(
            0, columns, filters, null, "asc", 0, 10);
    }
}
