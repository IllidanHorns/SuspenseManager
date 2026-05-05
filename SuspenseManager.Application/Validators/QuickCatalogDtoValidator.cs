using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class QuickCatalogDtoValidator : AbstractValidator<QuickCatalogDto>
{
    public QuickCatalogDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название продукта обязательно")
            .MaximumLength(255).WithMessage("Название продукта не должно превышать 255 символов");

        RuleFor(x => x.Artist)
            .NotEmpty().WithMessage("Исполнитель обязателен")
            .MaximumLength(255).WithMessage("Исполнитель не должен превышать 255 символов");

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

        RuleFor(x => x.ProductFormatCode)
            .MaximumLength(50).WithMessage("Код формата не должен превышать 50 символов")
            .When(x => x.ProductFormatCode != null);

        RuleFor(x => x.Genre)
            .MaximumLength(100).WithMessage("Жанр не должен превышать 100 символов")
            .When(x => x.Genre != null);

        RuleFor(x => x.AlbumName)
            .MaximumLength(255).WithMessage("Название альбома не должно превышать 255 символов")
            .When(x => x.AlbumName != null);

        RuleFor(x => x.Composer)
            .MaximumLength(255).WithMessage("Композитор не должен превышать 255 символов")
            .When(x => x.Composer != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Описание не должно превышать 1000 символов")
            .When(x => x.Description != null);

        RuleFor(x => x.ProductTypeId)
            .GreaterThan(0).WithMessage("Идентификатор типа продукта должен быть положительным")
            .When(x => x.ProductTypeId.HasValue);
    }
}
