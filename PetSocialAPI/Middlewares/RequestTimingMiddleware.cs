using Serilog;
using System.Diagnostics;
namespace PetSocialAPI.Middlewares;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
            sw.Stop();

            _logger.LogInformation(
                "Request {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds
            );

        }
        catch (Exception ex)
        {
            sw.Stop();
            // Log as error
            _logger.LogError(
                ex,
                "Exception for {Method} {Path}: {Message} (responded {StatusCode} in {ElapsedMilliseconds} ms)",
                context.Request.Method,
                context.Request.Path,
                ex.Message,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds
            );

            // Optional: rethrow or set status code as 500
            context.Response.StatusCode = 500;
            // Write error response if desired
            await context.Response.WriteAsync("An unhandled exception occurred.");

            // Do not rethrow, as we've handled the response
            // If you want to let ASP.NET Core's error page handle it, rethrow:
            // throw;
        }
    }
}
