using Microsoft.AspNetCore.Http;

namespace NovaShop.Infrastructure.Services;

public interface IImageStorage
{
    Task<ImageUploadResult> UploadAsync(IFormFile file, string folder = "general", string? publicId = null);
    Task<bool> DeleteAsync(string publicId);
    string GetUrl(string publicId);
}