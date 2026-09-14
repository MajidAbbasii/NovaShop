using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NovaShop.Domain.Exceptions;
using Serilog.Context;

namespace NovaShop.Api.Middleware;

// Serialize Persian/Arabic text as readable UTF-8 (not \uXXXX escapes) while
// keeping the response valid JSON. The browser decodes it correctly either way,
// but escaped sequences break some logging/display paths and are harder to read.
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            FluentValidation.ValidationException => (HttpStatusCode.BadRequest, "Validation failed."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "نام کاربری یا رمز عبور نادرست است"),
            ConflictException => (HttpStatusCode.Conflict, exception.Message),
            InsufficientStockException => (HttpStatusCode.Conflict, "موجودی کافی برای این محصول وجود ندارد."),
            InvalidOperationException => (HttpStatusCode.BadRequest, "درخواست نامعتبر است"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "موردی یافت نشد"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = exception.Message,
            Status = (int)status
        };

        var json = JsonSerializer.Serialize(problem, ApiJson.Options);
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.StatusCode = (int)status;
        return context.Response.WriteAsync(json);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Headers.TryGetValue(HeaderName, out var id);
        var correlationId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id.ToString();

        using var _ = LogContext.PushProperty("CorrelationId", correlationId);
        await _next(context);
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
