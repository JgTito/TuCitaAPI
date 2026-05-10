namespace TuCita.Api.Storage;

public sealed class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    private const long MaxLogoBytes = 2 * 1024 * 1024;
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedLogoExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedAvatarExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<string?> SaveBusinessLogoAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        return await SaveImageAsync(
            file,
            "Logo",
            "El logo no puede superar los 2 MB.",
            "El logo debe ser un archivo JPG o PNG.",
            "El archivo indicado no es una imagen válida.",
            MaxLogoBytes,
            AllowedLogoExtensions,
            Path.Combine("uploads", "negocios"),
            "/uploads/negocios",
            cancellationToken);
    }

    public async Task<string?> SaveUserAvatarAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        return await SaveImageAsync(
            file,
            "Avatar",
            "El avatar no puede superar los 2 MB.",
            "El avatar debe ser un archivo JPG, PNG o WEBP.",
            "El archivo indicado no es una imagen válida.",
            MaxAvatarBytes,
            AllowedAvatarExtensions,
            Path.Combine("uploads", "usuarios", "avatars"),
            "/uploads/usuarios/avatars",
            cancellationToken);
    }

    private async Task<string?> SaveImageAsync(
        IFormFile? file,
        string field,
        string maxSizeMessage,
        string extensionMessage,
        string contentTypeMessage,
        long maxBytes,
        IReadOnlyCollection<string> allowedExtensions,
        string relativeDirectory,
        string publicDirectory,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > maxBytes)
        {
            throw new FileStorageValidationException(field, maxSizeMessage);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new FileStorageValidationException(field, extensionMessage);
        }

        var contentType = file.ContentType.ToLowerInvariant();
        if (!contentType.StartsWith("image/"))
        {
            throw new FileStorageValidationException(field, contentTypeMessage);
        }

        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var absoluteDirectory = Path.Combine(webRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteDirectory, fileName);

        await using var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write);
        await file.CopyToAsync(stream, cancellationToken);

        return $"{publicDirectory}/{fileName}";
    }
}
