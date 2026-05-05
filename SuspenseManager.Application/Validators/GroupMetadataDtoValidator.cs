using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class UpdateGroupMetadataDtoValidator : AbstractValidator<UpdateGroupMetadataDto>
{
    public UpdateGroupMetadataDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название не может быть пустой строкой")
            .MaximumLength(255).WithMessage("Название не должно превышать 255 символов")
            .When(x => x.Title != null);

        RuleFor(x => x.Artist)
            .NotEmpty().WithMessage("Исполнитель не может быть пустой строкой")
            .MaximumLength(255).WithMessage("Исполнитель не должен превышать 255 символов")
            .When(x => x.Artist != null);

        RuleFor(x => x.Isrc)
            .MaximumLength(15).WithMessage("ISRC не должен превышать 15 символов")
            .Matches(@"^[A-Za-z]{2}-?[A-Za-z0-9]{3}-?\d{2}-?\d{5}$")
            .WithMessage("Некорректный формат ISRC. Ожидается: CCXXXYYNNNN или CC-XXX-YY-NNNNN")
            .When(x => x.Isrc != null);

        RuleFor(x => x.Barcode)
            .MaximumLength(20).WithMessage("Баркод не должен превышать 20 символов")
            .Matches(@"^\d{8}$|^\d{12}$|^\d{13}$")
            .WithMessage("Баркод должен содержать 8, 12 или 13 цифр (EAN-8, UPC-A, EAN-13)")
            .When(x => x.Barcode != null);

        RuleFor(x => x.CatalogNumber)
            .MaximumLength(100).WithMessage("Каталожный номер не должен превышать 100 символов")
            .When(x => x.CatalogNumber != null);

        RuleFor(x => x.Genre)
            .MaximumLength(100).WithMessage("Жанр не должен превышать 100 символов")
            .When(x => x.Genre != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Описание не должно превышать 1000 символов")
            .When(x => x.Description != null);

        RuleFor(x => x.ProductTypeCode)
            .MaximumLength(50).WithMessage("Код типа продукта не должен превышать 50 символов")
            .When(x => x.ProductTypeCode != null);

        RuleFor(x => x.ProductTypeDesc)
            .MaximumLength(500).WithMessage("Описание типа продукта не должно превышать 500 символов")
            .When(x => x.ProductTypeDesc != null);

        RuleFor(x => x.ProductTypeId)
            .GreaterThan(0).WithMessage("Идентификатор типа продукта должен быть положительным")
            .When(x => x.ProductTypeId.HasValue);

        RuleFor(x => x.CatalogProductId)
            .GreaterThan(0).WithMessage("Идентификатор продукта должен быть положительным")
            .When(x => x.CatalogProductId.HasValue);
    }
}

public class UpdateGroupMetaRightsDtoValidator : AbstractValidator<UpdateGroupMetaRightsDto>
{
    public UpdateGroupMetaRightsDtoValidator()
    {
        RuleFor(x => x.DocNumber)
            .MaximumLength(100).WithMessage("Номер договора не должен превышать 100 символов")
            .When(x => x.DocNumber != null);

        RuleFor(x => x.DocType)
            .MaximumLength(100).WithMessage("Тип договора не должен превышать 100 символов")
            .When(x => x.DocType != null);

        RuleFor(x => x.TerritoryCode)
            .MaximumLength(10).WithMessage("Код территории не должен превышать 10 символов")
            .When(x => x.TerritoryCode != null);

        RuleFor(x => x.TerritoryDesc)
            .MaximumLength(255).WithMessage("Описание территории не должно превышать 255 символов")
            .When(x => x.TerritoryDesc != null);

        RuleFor(x => x.TerritoryId)
            .GreaterThan(0).WithMessage("Идентификатор территории должен быть положительным")
            .When(x => x.TerritoryId.HasValue);

        RuleFor(x => x.SenderCompanyId)
            .GreaterThan(0).WithMessage("Идентификатор компании-отправителя должен быть положительным")
            .When(x => x.SenderCompanyId.HasValue);

        RuleFor(x => x.ReceiverCompanyId)
            .GreaterThan(0).WithMessage("Идентификатор компании-получателя должен быть положительным")
            .When(x => x.ReceiverCompanyId.HasValue);

        // Отправитель и получатель не могут быть одной компанией (проверяем только если оба указаны в запросе)
        RuleFor(x => x.ReceiverCompanyId)
            .Must((dto, id) => id!.Value != dto.SenderCompanyId!.Value)
            .WithMessage("Компания-отправитель и компания-получатель не могут совпадать")
            .When(x => x.ReceiverCompanyId.HasValue && x.SenderCompanyId.HasValue);

        RuleFor(x => x.Share)
            .InclusiveBetween(0, 100).WithMessage("Доля (%) должна быть в диапазоне от 0 до 100")
            .When(x => x.Share.HasValue);

        RuleFor(x => x.DocEnd)
            .GreaterThan(x => x.DocStart!.Value)
            .WithMessage("Дата окончания договора должна быть позже даты начала")
            .When(x => x.DocEnd.HasValue && x.DocStart.HasValue);
    }
}
