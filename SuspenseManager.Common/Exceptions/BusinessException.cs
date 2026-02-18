using Common.DTOs;

namespace Common.Exceptions;

/// <summary>
/// Бизнес-исключение с кодом и статусом. Используется когда нужно вернуть
/// конкретный бизнес-код и HTTP-статус из сервисного слоя.
/// </summary>
public class BusinessException : Exception
{
    public int StatusCode { get; }
    public string BusinessCode { get; }

    public BusinessException(string message, string businessCode = "BUSINESS_ERROR", int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
        BusinessCode = businessCode;
    }
}

/// <summary>
/// Исключение валидации. Содержит список ошибок по полям.
/// </summary>
public class ValidationException : Exception
{
    public List<ApiError> Errors { get; }

    public ValidationException(string message, List<ApiError> errors)
        : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string field, string errorMessage)
        : base($"Ошибка валидации: {errorMessage}")
    {
        Errors = [new ApiError { Field = field, Message = errorMessage }];
    }
}
