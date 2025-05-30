using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class FriendlyValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .Select(e => new
                {
                    Field = e.Key,
                    Messages = e.Value.Errors.Select(er => er.ErrorMessage)
                });

            context.Result = new BadRequestObjectResult(new
            {
                message = "Dados inválidos. Corrija os campos e tente novamente.",
                errors
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
