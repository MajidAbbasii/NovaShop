using MediatR;
using Microsoft.AspNetCore.Http;
using NovaShop.Infrastructure.Services;

namespace NovaShop.Application.Features.Images.Commands;

public record UploadImageCommand(
    IFormFile File,
    string? Folder = "general",
    string? PublicId = null,
    string? Category = "product",
    string? UploadedBy = null) : IRequest<ImageUploadResult>;