using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace NovaShop.Infrastructure.Services;

public class LocalImageStorage : IImageStorage
{
    private readonly ILogger<LocalImageStorage> _logger;
    private readonly IOptions<ImageStorageOptions> _options;
    private readonly string _basePath;

    public LocalImageStorage(ILogger<LocalImageStorage> logger, IOptions<ImageStorageOptions> options)
    {
        _logger = logger;
        _options = options;
        _basePath = Path.GetFullPath(options.Value.BasePath ?? "./wwwroot/images");

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created image storage directory: {BasePath}", _basePath);
        }
    }

    public async Task<ImageUploadResult> UploadAsync(IFormFile file, string folder = "general", string? publicId = null)
    {
        try
        {
            // Validate file type
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
            {
                throw new InvalidOperationException("نوع فایل مجاز نیست. فقط JPG, PNG, WEBP پشتیبانی می‌شوند.");
            }

            // Validate file size
            if (file.Length > _options.Value.MaxSizeBytes)
            {
                throw new InvalidOperationException(
                    $"حجم فایل بیش از حد مجاز است. حداکثر {_options.Value.MaxSizeBytes / (1024 * 1024)} مگابایت مجاز است.");
            }

            // Sanitize folder: allow letters/digits/underscore/hyphen only (prevents path traversal)
            var safeFolder = SanitizeFolder(folder);
            var folderPath = Path.Combine(_basePath, safeFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Generate filename
            var fileName = string.IsNullOrWhiteSpace(publicId)
                ? Guid.NewGuid().ToString("N") + extension
                : SanitizeFileName(publicId, extension);

            var filePath = Path.Combine(folderPath, fileName);

            // Save file
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var image = await Image.LoadAsync(stream);
            await image.SaveAsync(filePath);

            var result = new ImageUploadResult
            {
                Url = GetUrl(safeFolder, fileName),
                PublicId = $"{safeFolder}/{fileName}",
                FileName = file.FileName,
                Size = file.Length,
                Format = extension.TrimStart('.'),
                Folder = safeFolder
            };

            _logger.LogInformation("تصویر ذخیره شد: {FilePath}", filePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در آپلود تصویر");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string publicId)
    {
        try
        {
            var (folder, fileName) = SplitPublicId(publicId);
            var safeFolder = SanitizeFolder(folder);
            var safeFile = SanitizeFileName(fileName, null);
            var filePath = Path.Combine(_basePath, safeFolder, safeFile);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("تصویر حذف شد: {FilePath}", filePath);
                return true;
            }

            _logger.LogWarning("تصویر با PublicId {PublicId} پیدا نشد", publicId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف تصویر با PublicId {PublicId}", publicId);
            return false;
        }
    }

    public string GetUrl(string publicId)
    {
        // Backward compatible: publicId may or may not include the folder segment
        var (folder, fileName) = SplitPublicId(publicId);
        return GetUrl(folder, fileName);
    }

    private string GetUrl(string folder, string fileName)
        => $"/images/{folder}/{fileName}";

    private static (string Folder, string FileName) SplitPublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return ("general", string.Empty);
        }

        var parts = publicId.Replace('\\', '/').Split('/');
        if (parts.Length > 1)
        {
            return (string.Join("/", parts[..^1]), parts[^1]);
        }

        return ("general", parts[0]);
    }

    private static string SanitizeFolder(string folder)
    {
        var value = string.IsNullOrWhiteSpace(folder) ? "general" : folder.Trim();
        var sanitized = string.Concat(value.Select(c =>
            char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '-'));
        return string.IsNullOrWhiteSpace(sanitized) ? "general" : sanitized;
    }

    private static string SanitizeFileName(string name, string? extension)
    {
        var safe = string.Concat(name.Select(c =>
            char.IsLetterOrDigit(c) || c is '_' or '-' or '.' ? c : '_'));
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = Guid.NewGuid().ToString("N");
        }

        // Ensure an allowed extension is present
        var ext = Path.GetExtension(safe).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
        {
            safe += extension ?? ".png";
        }

        return safe;
    }
}

public class ImageStorageOptions
{
    public string BasePath { get; set; } = "./wwwroot/images";
    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
}