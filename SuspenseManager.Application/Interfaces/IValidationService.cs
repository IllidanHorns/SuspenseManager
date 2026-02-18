using System.Collections.Generic;
using System.Threading.Tasks;
using Common.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Сервис валидации строк из отчётов стриминговых платформ
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Валидация пакета строк (из Excel-отчёта)
    /// </summary>
    Task<ValidationResultDto> ValidateBatchAsync(List<SuspenseLineDto> lines);

    /// <summary>
    /// Валидация одной строки (из формы ручного ввода)
    /// </summary>
    Task<ValidationLineResultDto> ValidateSingleAsync(SuspenseLineDto line);
}
