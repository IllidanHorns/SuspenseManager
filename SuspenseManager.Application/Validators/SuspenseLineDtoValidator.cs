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

        RuleFor(x => x.Isrc)
            .MaximumLength(15).WithMessage("ISRC не должен превышать 15 символов")
            .When(x => x.Isrc != null);

        RuleFor(x => x.Barcode)
            .MaximumLength(20).WithMessage("Баркод не должен превышать 20 символов")
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

        // ExchangeCurrency / ExchangeRate — decimal(18,6) в конфигурации EF (в отчётах в колонке «валюта» часто курс с 4 знаками)
        RuleFor(x => x.ExchangeCurrency)
            .PrecisionScale(18, 6, false)
            .WithMessage("Валюта (числовое поле): не более 18 знаков, из них 6 после запятой");

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
