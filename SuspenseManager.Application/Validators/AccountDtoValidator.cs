using Application.Helpers;
using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountDtoValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин обязателен")
            .MaximumLength(100).WithMessage("Логин не должен превышать 100 символов");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .Must(p => p != null && PasswordPolicyHelper.GetValidationError(p) == null)
            .WithMessage(x => PasswordPolicyHelper.GetValidationError(x.Password) ?? "Пароль не соответствует требованиям");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Описание не должно превышать 1000 символов")
            .When(x => x.Description != null);

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Идентификатор пользователя должен быть положительным")
            .When(x => x.UserId.HasValue);
    }
}

public class UpdateAccountDtoValidator : AbstractValidator<UpdateAccountDto>
{
    public UpdateAccountDtoValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин не может быть пустой строкой")
            .MaximumLength(100).WithMessage("Логин не должен превышать 100 символов")
            .When(x => x.Login != null);

        RuleFor(x => x.Password)
            .Must(p => PasswordPolicyHelper.GetValidationError(p) == null)
            .WithMessage(x => PasswordPolicyHelper.GetValidationError(x.Password) ?? "Пароль не соответствует требованиям")
            .When(x => x.Password != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Описание не должно превышать 1000 символов")
            .When(x => x.Description != null);

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Идентификатор пользователя должен быть положительным")
            .When(x => x.UserId.HasValue);
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин обязателен")
            .MaximumLength(100).WithMessage("Логин не должен превышать 100 символов");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен");
    }
}
