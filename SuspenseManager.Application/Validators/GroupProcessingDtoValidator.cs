using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class PostponeGroupDtoValidator : AbstractValidator<PostponeGroupDto>
{
    public PostponeGroupDtoValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Причина откладывания не должна превышать 500 символов")
            .When(x => x.Reason != null);
    }
}

public class LinkProductDtoValidator : AbstractValidator<LinkProductDto>
{
    public LinkProductDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Идентификатор продукта должен быть положительным");
    }
}

public class CopyRightsDtoValidator : AbstractValidator<CopyRightsDto>
{
    public CopyRightsDtoValidator()
    {
        RuleFor(x => x.RightsId)
            .GreaterThan(0).WithMessage("Идентификатор записи прав должен быть положительным");
    }
}
