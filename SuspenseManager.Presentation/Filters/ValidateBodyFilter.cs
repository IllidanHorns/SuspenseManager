using Common.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Presentation.Filters;

public class ValidateBodyFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidateBodyFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, value) in context.ActionArguments)
        {
            if (value == null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
            if (_services.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(value);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .Select(e => new ApiError { Field = e.PropertyName, Message = e.ErrorMessage })
                    .ToList();

                var response = ApiResponse<object>.Fail(400, "Ошибка валидации входных данных", "VALIDATION_ERROR", errors);
                context.Result = new BadRequestObjectResult(response);
                return;
            }
        }

        await next();
    }
}
