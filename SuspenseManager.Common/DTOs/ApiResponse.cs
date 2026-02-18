using System.Text.Json.Serialization;

namespace Common.DTOs;

/// <summary>
/// Универсальный ответ API. Оборачивает все ответы в единый формат.
/// </summary>
/// <typeparam name="T">Тип данных в теле ответа</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// HTTP статус-код
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Бизнес-код операции (для программной обработки на клиенте).
    /// Например: "SUSPENSE_CREATED", "VALIDATION_ERROR", "NOT_FOUND"
    /// </summary>
    public string? BusinessCode { get; set; }

    /// <summary>
    /// Сообщение для отображения пользователю
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Тело ответа (объект или массив)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>
    /// Список ошибок (валидация, бизнес-ошибки и т.д.)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ApiError>? Errors { get; set; }

    /// <summary>
    /// Временная метка ответа
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data, string message = "Операция выполнена успешно", string? businessCode = null)
    {
        return new ApiResponse<T>
        {
            StatusCode = 200,
            BusinessCode = businessCode,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Created(T data, string message = "Запись создана", string? businessCode = null)
    {
        return new ApiResponse<T>
        {
            StatusCode = 201,
            BusinessCode = businessCode,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(int statusCode, string message, string? businessCode = null, List<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            StatusCode = statusCode,
            BusinessCode = businessCode,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// Описание одной ошибки
/// </summary>
public class ApiError
{
    /// <summary>
    /// Поле, к которому относится ошибка (null если общая ошибка)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Field { get; set; }

    /// <summary>
    /// Текст ошибки
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
