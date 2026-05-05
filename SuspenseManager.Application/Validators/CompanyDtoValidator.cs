using Common.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateCompanyDtoValidator : AbstractValidator<CreateCompanyDto>
{
    private const string PhonePattern = @"^\+?[\d\s()\-]{7,20}$";
    // ИНН: 10 цифр (юр. лицо) или 12 цифр (физ. лицо)
    private const string InnPattern = @"^\d{10}$|^\d{12}$";
    // БИК: ровно 9 цифр (российский банковский идентификационный код)
    private const string BicPattern = @"^\d{9}$";

    public CreateCompanyDtoValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Полное наименование обязательно")
            .MaximumLength(255).WithMessage("Полное наименование не должно превышать 255 символов");

        RuleFor(x => x.ShortName)
            .NotEmpty().WithMessage("Краткое наименование обязательно")
            .MaximumLength(100).WithMessage("Краткое наименование не должно превышать 100 символов");

        RuleFor(x => x.CompanyCode)
            .MaximumLength(50).WithMessage("Код компании не должен превышать 50 символов")
            .When(x => x.CompanyCode != null);

        RuleFor(x => x.BankName)
            .MaximumLength(255).WithMessage("Наименование банка не должно превышать 255 символов")
            .When(x => x.BankName != null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Телефон не должен превышать 20 символов")
            .Matches(PhonePattern).WithMessage("Введите корректный номер телефона (например: +7 (900) 000-00-00)")
            .When(x => x.PhoneNumber != null);

        RuleFor(x => x.Country)
            .MaximumLength(10).WithMessage("Код страны не должен превышать 10 символов")
            .When(x => x.Country != null);

        RuleFor(x => x.LegalAddress)
            .MaximumLength(500).WithMessage("Юридический адрес не должен превышать 500 символов")
            .When(x => x.LegalAddress != null);

        RuleFor(x => x.ActualAddress)
            .MaximumLength(500).WithMessage("Фактический адрес не должен превышать 500 символов")
            .When(x => x.ActualAddress != null);

        RuleFor(x => x.Inn)
            .MaximumLength(12).WithMessage("ИНН не должен превышать 12 символов")
            .Matches(InnPattern).WithMessage("ИНН должен содержать ровно 10 цифр (юридическое лицо) или 12 цифр (физическое лицо)")
            .When(x => x.Inn != null);

        RuleFor(x => x.Bic)
            .Matches(BicPattern).WithMessage("БИК должен содержать ровно 9 цифр")
            .When(x => x.Bic != null);
    }
}

public class UpdateCompanyDtoValidator : AbstractValidator<UpdateCompanyDto>
{
    private const string PhonePattern = @"^\+?[\d\s()\-]{7,20}$";
    private const string InnPattern = @"^\d{10}$|^\d{12}$";
    private const string BicPattern = @"^\d{9}$";

    public UpdateCompanyDtoValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Полное наименование не может быть пустой строкой")
            .MaximumLength(255).WithMessage("Полное наименование не должно превышать 255 символов")
            .When(x => x.LegalName != null);

        RuleFor(x => x.ShortName)
            .NotEmpty().WithMessage("Краткое наименование не может быть пустой строкой")
            .MaximumLength(100).WithMessage("Краткое наименование не должно превышать 100 символов")
            .When(x => x.ShortName != null);

        RuleFor(x => x.CompanyCode)
            .MaximumLength(50).WithMessage("Код компании не должен превышать 50 символов")
            .When(x => x.CompanyCode != null);

        RuleFor(x => x.BankName)
            .MaximumLength(255).WithMessage("Наименование банка не должно превышать 255 символов")
            .When(x => x.BankName != null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Телефон не должен превышать 20 символов")
            .Matches(PhonePattern).WithMessage("Введите корректный номер телефона (например: +7 (900) 000-00-00)")
            .When(x => x.PhoneNumber != null);

        RuleFor(x => x.Country)
            .MaximumLength(10).WithMessage("Код страны не должен превышать 10 символов")
            .When(x => x.Country != null);

        RuleFor(x => x.LegalAddress)
            .MaximumLength(500).WithMessage("Юридический адрес не должен превышать 500 символов")
            .When(x => x.LegalAddress != null);

        RuleFor(x => x.ActualAddress)
            .MaximumLength(500).WithMessage("Фактический адрес не должен превышать 500 символов")
            .When(x => x.ActualAddress != null);

        RuleFor(x => x.Inn)
            .MaximumLength(12).WithMessage("ИНН не должен превышать 12 символов")
            .Matches(InnPattern).WithMessage("ИНН должен содержать ровно 10 цифр (юридическое лицо) или 12 цифр (физическое лицо)")
            .When(x => x.Inn != null);

        RuleFor(x => x.Bic)
            .Matches(BicPattern).WithMessage("БИК должен содержать ровно 9 цифр")
            .When(x => x.Bic != null);
    }
}
