using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookingApp.API.Filters;

public class AsyncValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;
    
    public AsyncValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cancellationToken = context.HttpContext.RequestAborted;
        
        foreach (var item in context.ActionArguments.Values)
        {
            if (item is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(item.GetType());
            var validator = _serviceProvider.GetService(validatorType) as IValidator;
            
            if (validator is null)
                continue;
            
            var validationResult = await validator.ValidateAsync(new ValidationContext<object>(item), cancellationToken);
            if (!validationResult.IsValid)
            {
                context.Result = new BadRequestObjectResult(validationResult.ToValidationProblemDetails(context.HttpContext.Request.Path));
                return;
            }
        }
        
        await next();
    }
}