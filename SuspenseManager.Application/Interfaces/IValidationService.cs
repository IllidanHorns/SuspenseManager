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
    /// Валидация пакета строк (из Excel-отчёта).
    /// </summary>
    /// <param name="numberRowsAsInExcel">
    /// Если true, в текстах ошибок номер строки считается как в Excel: первая строка данных = 2 (под заголовком).
    /// Для ручного ввода одной строки передайте false — префикс «Строка N» не добавляется.
    /// </param>
    Task<ValidationResultDto> ValidateBatchAsync(List<SuspenseLineDto> lines, bool numberRowsAsInExcel = true);

    /// <summary>
    /// Валидация одной строки (из формы ручного ввода)
    /// </summary>
    Task<ValidationLineResultDto> ValidateSingleAsync(SuspenseLineDto line);
}
