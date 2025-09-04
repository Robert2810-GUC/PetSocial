using System.Diagnostics;
using System.Text;
using System.Linq;
using System.IO;
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

        // Capture request details
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var headers = string.Join("; ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));

        string requestBody = string.Empty;
        context.Request.EnableBuffering();
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        _logger.LogInformation(
            "{Timestamp} Request: {Method} {Path} QueryString: {QueryString} Headers: {Headers} Body: {RequestBody}",
            DateTimeOffset.UtcNow.ToString("o"),
            context.Request.Method,
            context.Request.Path,
            queryString,
            headers,
            requestBody
        );

        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An unhandled exception occurred.");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var errorResponseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            sw.Stop();
            _logger.LogError(
                ex,
                "{Timestamp} Response: {Method} {Path} QueryString: {QueryString} Headers: {Headers} Body: {RequestBody} StatusCode: {StatusCode} in {ElapsedMilliseconds} ms Body: {ResponseBody}",
                DateTimeOffset.UtcNow.ToString("o"),
                context.Request.Method,
                context.Request.Path,
                queryString,
                headers,
                requestBody,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                errorResponseText
            );

            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
            return;
        }

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        sw.Stop();
        _logger.LogInformation(
            "{Timestamp} Response: {Method} {Path} QueryString: {QueryString} StatusCode: {StatusCode} in {ElapsedMilliseconds} ms Body: {ResponseBody}",
            DateTimeOffset.UtcNow.ToString("o"),
            context.Request.Method,
            context.Request.Path,
            queryString,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            responseText
        );

        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }
}
