using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Application.Settings;

public class NullableLongBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var val = ctx.ValueProvider.GetValue(ctx.ModelName).FirstValue;
        if (string.IsNullOrWhiteSpace(val) || val.Equals("null", StringComparison.OrdinalIgnoreCase)
            || val.Equals("undefined", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }
        if (long.TryParse(val, out var num))
            ctx.Result = ModelBindingResult.Success((long?)num);
        else
            ctx.ModelState.AddModelError(ctx.ModelName, $"The value '{val}' is not valid for {ctx.ModelName}.");
        return Task.CompletedTask;
    }
}
