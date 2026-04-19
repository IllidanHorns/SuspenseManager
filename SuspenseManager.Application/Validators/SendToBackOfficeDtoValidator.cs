using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class SendToBackOfficeDtoValidator : AbstractValidator<SendToBackOfficeDto>
{
    public SendToBackOfficeDtoValidator()
    {
        RuleFor(x => x.ProblemDescription)
            .NotEmpty().WithMessage("Описание проблемы обязательно")
            .MaximumLength(2000).WithMessage("Описание проблемы слишком длинное: введено {TotalLength} симв., допустимо не более {MaxLength}");
    }
}
