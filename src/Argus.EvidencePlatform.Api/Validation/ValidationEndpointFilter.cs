using FluentValidation;
using FluentValidation.Results;

namespace Argus.EvidencePlatform.Api.Validation;

public sealed class ValidationEndpointFilter<TRequest> : IEndpointFilter
    where TRequest : notnull
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            return await next(context);
        }

        var argument = context.Arguments.OfType<TRequest>().SingleOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (validationResult.IsValid)
        {
            return await next(context);
        }

        return Results.ValidationProblem(ToDictionary(validationResult));
    }

    private static Dictionary<string, string[]> ToDictionary(ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());
    }
}
