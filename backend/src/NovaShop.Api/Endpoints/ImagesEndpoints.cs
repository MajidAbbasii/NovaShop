using MediatR;
using Microsoft.AspNetCore.Mvc;
using NovaShop.Application.Features.Images.Commands;
using System.Security.Claims;

namespace NovaShop.Api.Endpoints;

public static class ImagesEndpoints
{
    public static IEndpointRouteBuilder MapImagesEndpoints(this IEndpointRouteBuilder app)
    {
        // Upload image (multipart/form-data: file, folder, category)
        app.MapPost("/api/images/upload", [IgnoreAntiforgeryToken] async (
            [FromForm] IFormFile file,
            [FromForm] string? folder,
            [FromForm] string? category,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { message = "فایل تصویر الزامی است" });
            }

            var command = new UploadImageCommand(
                File: file,
                Folder: folder,
                Category: category,
                UploadedBy: user.Identity?.Name);

            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("UploadImage")
        .DisableAntiforgery()
        .RequireAuthorization();

        // Delete image
        app.MapDelete("/api/images/{**publicId}", async (
            string publicId,
            IMediator mediator) =>
        {
            var success = await mediator.Send(new DeleteImageCommand(publicId));
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteImage")
        .RequireAuthorization("AdminOnly");

        return app;
    }
}