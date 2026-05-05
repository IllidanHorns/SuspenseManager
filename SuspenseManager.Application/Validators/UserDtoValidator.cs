using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    // +7 (900) 000-00-00, +79000000000, 8-800-555-35-35, etc.
    private const string PhonePattern = @"^\+?[\d\s()\-]{7,20}$";

    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно")
            .MaximumLength(100).WithMessage("Имя не должно превышать 100 символов");

        RuleFor(x => x.Surname)
            .NotEmpty().WithMessage("Фамилия обязательна")
            .MaximumLength(100).WithMessage("Фамилия не должна превышать 100 символов");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("Отчество не должно превышать 100 символов")
            .When(x => x.MiddleName != null);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .MaximumLength(255).WithMessage("Email не должен превышать 255 символов")
            .EmailAddress().WithMessage("Некорректный формат email");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Номер телефона обязателен")
            .MaximumLength(20).WithMessage("Номер телефона не должен превышать 20 символов")
            .Matches(PhonePattern).WithMessage("Введите корректный номер телефона (например: +7 (900) 000-00-00)");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Должность обязательна")
            .MaximumLength(255).WithMessage("Должность не должна превышать 255 символов");
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    private const string PhonePattern = @"^\+?[\d\s()\-]{7,20}$";

    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя не может быть пустой строкой")
            .MaximumLength(100).WithMessage("Имя не должно превышать 100 символов")
            .When(x => x.Name != null);

        RuleFor(x => x.Surname)
            .NotEmpty().WithMessage("Фамилия не может быть пустой строкой")
            .MaximumLength(100).WithMessage("Фамилия не должна превышать 100 символов")
            .When(x => x.Surname != null);

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("Отчество не должно превышать 100 символов")
            .When(x => x.MiddleName != null);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email не может быть пустой строкой")
            .MaximumLength(255).WithMessage("Email не должен превышать 255 символов")
            .EmailAddress().WithMessage("Некорректный формат email")
            .When(x => x.Email != null);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Номер телефона не может быть пустой строкой")
            .MaximumLength(20).WithMessage("Номер телефона не должен превышать 20 символов")
            .Matches(PhonePattern).WithMessage("Введите корректный номер телефона (например: +7 (900) 000-00-00)")
            .When(x => x.PhoneNumber != null);

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Должность не может быть пустой строкой")
            .MaximumLength(255).WithMessage("Должность не должна превышать 255 символов")
            .When(x => x.Position != null);
    }
}
