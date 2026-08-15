using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace NovaShop.Api.Middleware;

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

        var json = JsonSerializer.Serialize(problem);
        context.Response.ContentType = "application/problem+json";
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
