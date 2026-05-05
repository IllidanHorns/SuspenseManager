using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateCatalogProductRightsDtoValidator : AbstractValidator<CreateCatalogProductRightsDto>
{
    public CreateCatalogProductRightsDtoValidator()
    {
        RuleFor(x => x.CatalogProductId)
            .GreaterThan(0).WithMessage("Идентификатор продукта должен быть положительным");

        RuleFor(x => x.CompanySenderId)
            .GreaterThan(0).WithMessage("Идентификатор компании-отправителя должен быть положительным");

        RuleFor(x => x.CompanyReceiverId)
            .GreaterThan(0).WithMessage("Идентификатор компании-получателя должен быть положительным")
            .NotEqual(x => x.CompanySenderId).WithMessage("Компания-отправитель и компания-получатель не могут совпадать");

        RuleFor(x => x.TerritoryId)
            .GreaterThan(0).WithMessage("Идентификатор территории должен быть положительным");

        RuleFor(x => x.DocNumber)
            .MaximumLength(100).WithMessage("Номер договора не должен превышать 100 символов")
            .When(x => x.DocNumber != null);

        RuleFor(x => x.Share)
            .InclusiveBetween(1, 100).WithMessage("Доля (%) должна быть в диапазоне от 1 до 100");

        RuleFor(x => x.DocStart)
            .NotEqual(default(DateOnly)).WithMessage("Дата начала договора должна быть указана");

        RuleFor(x => x.DocEnd)
            .NotEqual(default(DateOnly)).WithMessage("Дата окончания договора должна быть указана")
            .GreaterThan(x => x.DocStart)
            .WithMessage("Дата окончания договора должна быть позже даты начала");
    }
}

public class UpdateCatalogProductRightsDtoValidator : AbstractValidator<UpdateCatalogProductRightsDto>
{
    public UpdateCatalogProductRightsDtoValidator()
    {
        RuleFor(x => x.CompanySenderId)
            .GreaterThan(0).WithMessage("Идентификатор компании-отправителя должен быть положительным")
            .When(x => x.CompanySenderId.HasValue);

        RuleFor(x => x.CompanyReceiverId)
            .GreaterThan(0).WithMessage("Идентификатор компании-получателя должен быть положительным")
            .When(x => x.CompanyReceiverId.HasValue);

        RuleFor(x => x.CompanyReceiverId)
            .Must((dto, id) => id!.Value != dto.CompanySenderId!.Value)
            .WithMessage("Компания-отправитель и компания-получатель не могут совпадать")
            .When(x => x.CompanyReceiverId.HasValue && x.CompanySenderId.HasValue);

        RuleFor(x => x.TerritoryId)
            .GreaterThan(0).WithMessage("Идентификатор территории должен быть положительным")
            .When(x => x.TerritoryId.HasValue);

        RuleFor(x => x.DocNumber)
            .MaximumLength(100).WithMessage("Номер договора не должен превышать 100 символов")
            .When(x => x.DocNumber != null);

        RuleFor(x => x.Share)
            .InclusiveBetween(1, 100).WithMessage("Доля (%) должна быть в диапазоне от 1 до 100")
            .When(x => x.Share.HasValue);

        RuleFor(x => x.DocStart)
            .NotEqual(default(DateOnly)).WithMessage("Дата начала договора не может быть нулевой")
            .When(x => x.DocStart.HasValue);

        RuleFor(x => x.DocEnd)
            .NotEqual(default(DateOnly)).WithMessage("Дата окончания договора не может быть нулевой")
            .GreaterThan(x => x.DocStart!.Value)
            .WithMessage("Дата окончания договора должна быть позже даты начала")
            .When(x => x.DocEnd.HasValue && x.DocStart.HasValue);
    }
}
