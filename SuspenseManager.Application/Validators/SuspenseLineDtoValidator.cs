using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Валидация <see cref="SuspenseLineDto"/> в соответствии с ограничениями колонок
/// <c>SuspenseLines</c> (см. <c>SuspenseLineConfiguration</c>).
/// </summary>
public class SuspenseLineDtoValidator : AbstractValidator<SuspenseLineDto>
{
    public SuspenseLineDtoValidator()
    {
        RuleFor(x => x.Qty)
            .GreaterThanOrEqualTo(0).WithMessage("Количество стримов не может быть отрицательным");

        // ISRC: CC-XXX-YY-NNNNN (два буквенных + три алфанумерных + 2 цифры года + 5 цифр), дефисы опциональны
        RuleFor(x => x.Isrc)
            .MaximumLength(15).WithMessage("ISRC не должен превышать 15 символов")
            .Matches(@"^[A-Za-z]{2}-?[A-Za-z0-9]{3}-?\d{2}-?\d{5}$")
            .WithMessage("Некорректный формат ISRC. Ожидается: CCXXXYYNNNN или CC-XXX-YY-NNNNN (например: RU-S1Z-24-00001)")
            .When(x => x.Isrc != null);

        // Barcode: EAN-8 (8 цифр), UPC-A (12 цифр) или EAN-13 (13 цифр)
        RuleFor(x => x.Barcode)
            .MaximumLength(20).WithMessage("Баркод не должен превышать 20 символов")
            .Matches(@"^\d{8}$|^\d{12}$|^\d{13}$")
            .WithMessage("Баркод должен содержать 8 цифр (EAN-8), 12 цифр (UPC-A) или 13 цифр (EAN-13)")
            .When(x => x.Barcode != null);

        RuleFor(x => x.CatalogNumber)
            .MaximumLength(100).WithMessage("Каталожный номер не должен превышать 100 символов")
            .When(x => x.CatalogNumber != null);

        RuleFor(x => x.ProductFormatCode)
            .MaximumLength(50).WithMessage("Код формата не должен превышать 50 символов")
            .When(x => x.ProductFormatCode != null);

        RuleFor(x => x.SenderCompany)
            .MaximumLength(255).WithMessage("Компания-отправитель не должна превышать 255 символов")
            .When(x => x.SenderCompany != null);

        RuleFor(x => x.RecipientCompany)
            .MaximumLength(255).WithMessage("Компания-получатель не должна превышать 255 символов")
            .When(x => x.RecipientCompany != null);

        RuleFor(x => x.Operator)
            .MaximumLength(255).WithMessage("Оператор не должен превышать 255 символов")
            .When(x => x.Operator != null);

        RuleFor(x => x.Artist)
            .MaximumLength(255).WithMessage("Артист не должен превышать 255 символов")
            .When(x => x.Artist != null);

        RuleFor(x => x.TrackTitle)
            .MaximumLength(255).WithMessage("Название трека не должно превышать 255 символов")
            .When(x => x.TrackTitle != null);

        RuleFor(x => x.AgreementType)
            .MaximumLength(100).WithMessage("Тип договора не должен превышать 100 символов")
            .When(x => x.AgreementType != null);

        RuleFor(x => x.AgreementNumber)
            .MaximumLength(100).WithMessage("Номер договора не должен превышать 100 символов")
            .When(x => x.AgreementNumber != null);

        RuleFor(x => x.TerritoryCode)
            .MaximumLength(10).WithMessage("Код территории не должен превышать 10 символов")
            .When(x => x.TerritoryCode != null);

        RuleFor(x => x.Genre)
            .MaximumLength(100).WithMessage("Жанр не должен превышать 100 символов")
            .When(x => x.Genre != null);

        RuleFor(x => x.ExchangeCurrency)
            .MaximumLength(10).WithMessage("Код валюты не должен превышать 10 символов")
            .When(x => x.ExchangeCurrency != null);

        RuleFor(x => x.ExchangeRate)
            .PrecisionScale(18, 6, false)
            .WithMessage("Курс обмена: не более 18 знаков, из них 6 после запятой");

        RuleFor(x => x.Ppd)
            .Must(p => !p.HasValue || (!double.IsNaN(p.Value) && !double.IsInfinity(p.Value) && p.Value >= 0))
            .WithMessage("Цена за стрим (PPD) должна быть неотрицательным конечным числом");

        RuleFor(x => x.SenderCompanyId)
            .GreaterThan(0)
            .WithMessage("Идентификатор компании-отправителя должен быть положительным")
            .When(x => x.SenderCompanyId.HasValue);

        RuleFor(x => x.RecipientCompanyId)
            .GreaterThan(0)
            .WithMessage("Идентификатор компании-получателя должен быть положительным")
            .When(x => x.RecipientCompanyId.HasValue);
    }
}
