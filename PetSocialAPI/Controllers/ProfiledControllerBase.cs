using Microsoft.AspNetCore.Mvc;
using StackExchange.Profiling;

namespace PetSocialAPI.Controllers
{
    public class ProfiledControllerBase : ControllerBase
    {
        protected async Task<IActionResult> ProfileAsync(string stepName, Func<Task<IActionResult>> action)
        {
            using (MiniProfiler.Current.Step(stepName))
            {
                return await action();
            }
        }
    }

}
