using System.Text.Json;
using Common.DTOs;
using Common.Exceptions;

namespace SuspenseManager.Middleware;

/// <summary>
/// Глобальная обработка исключений. Перехватывает все необработанные исключения,
/// логирует их и возвращает клиенту стандартизированный ApiResponse.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, businessCode, message) = exception switch
        {
            BusinessException bex => (bex.StatusCode, bex.BusinessCode, bex.Message),
            Common.Exceptions.ValidationException vex => (400, "VALIDATION_ERROR", vex.Message),
            KeyNotFoundException => (404, "NOT_FOUND", "Запрашиваемый ресурс не найден"),
            UnauthorizedAccessException => (401, "UNAUTHORIZED", "Требуется авторизация"),
            OperationCanceledException => (499, "REQUEST_CANCELLED", "Запрос отменён клиентом"),
            _ => (500, "INTERNAL_ERROR", "Произошла внутренняя ошибка сервера")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception,
                "Необработанное исключение: {ExceptionType} | Path: {Path} | Message: {Message}",
                exception.GetType().Name, context.Request.Path, exception.Message);
        }
        else
        {
            _logger.LogWarning(
                "Клиентская ошибка: {StatusCode} {BusinessCode} | Path: {Path} | Message: {Message}",
                statusCode, businessCode, context.Request.Path, exception.Message);
        }

        var errors = exception is Common.Exceptions.ValidationException validationEx
            ? validationEx.Errors
            : null;

        var response = ApiResponse<object>.Fail(statusCode, message, businessCode, errors);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }
}
