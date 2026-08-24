using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NovaShop.Domain.Exceptions;

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

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
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
