using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateTerritoryDtoValidator : AbstractValidator<CreateTerritoryDto>
{
    public CreateTerritoryDtoValidator()
    {
        RuleFor(x => x.TerritoryCode)
            .NotEmpty().WithMessage("Код территории обязателен")
            .MaximumLength(10).WithMessage("Код территории не должен превышать 10 символов");

        RuleFor(x => x.TerritoryName)
            .MaximumLength(255).WithMessage("Название территории не должно превышать 255 символов")
            .When(x => x.TerritoryName != null);
    }
}

public class UpdateTerritoryDtoValidator : AbstractValidator<UpdateTerritoryDto>
{
    public UpdateTerritoryDtoValidator()
    {
        RuleFor(x => x.TerritoryCode)
            .NotEmpty().WithMessage("Код территории не может быть пустой строкой")
            .MaximumLength(10).WithMessage("Код территории не должен превышать 10 символов")
            .When(x => x.TerritoryCode != null);

        RuleFor(x => x.TerritoryName)
            .MaximumLength(255).WithMessage("Название территории не должно превышать 255 символов")
            .When(x => x.TerritoryName != null);
    }
}
