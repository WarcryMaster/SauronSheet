namespace SauronSheet.Infrastructure.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sentry;

/// <summary>
/// Global exception handling middleware.
/// Catches all unhandled exceptions in the HTTP pipeline, logs them with Sentry,
/// and redirects to the error page instead of exposing stack traces to the client.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception on {Method} {Path}", 
            context.Request.Method, context.Request.Path);

        SentrySdk.CaptureException(ex, scope =>
        {
            scope.SetTag("request.method", context.Request.Method);
            scope.SetTag("request.path", context.Request.Path.ToString());
            scope.SetTag("request.host", context.Request.Host.ToString());
            scope.Level = SentryLevel.Error;

            var userId = context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
                scope.User = new SentryUser { Id = userId };
        });

        // Avoid modifying a response that has already started streaming
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // For AJAX/JSON requests return JSON error, otherwise redirect to error page
        if (IsJsonRequest(context))
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"An unexpected error occurred. Please try again later.\"}");
        }
        else
        {
            context.Response.Redirect("/Error");
        }
    }

    private static bool IsJsonRequest(HttpContext context)
    {
        var acceptHeader = context.Request.Headers.Accept.ToString();
        var contentType = context.Request.ContentType ?? string.Empty;
        return acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
