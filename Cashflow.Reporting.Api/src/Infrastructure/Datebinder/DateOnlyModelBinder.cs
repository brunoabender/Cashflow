using Microsoft.AspNetCore.Mvc.ModelBinding;

public class DateOnlyModelBinder : IModelBinder
{
    private static readonly string[] SupportedFormats = { "dd/MM/yyyy", "yyyy-MM-dd" };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value))
            return Task.CompletedTask;

        foreach (var format in SupportedFormats)
        {
            if (DateOnly.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out var dateOnly))
            {
                bindingContext.Result = ModelBindingResult.Success(dateOnly);
                return Task.CompletedTask;
            }
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"A data deve estar no formato {string.Join(" ou ", SupportedFormats)}.");
        return Task.CompletedTask;
    }
}
