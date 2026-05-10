namespace TuCita.Api.Storage;

public interface IFileStorageService
{
    Task<string?> SaveBusinessLogoAsync(IFormFile? file, CancellationToken cancellationToken);

    Task<string?> SaveUserAvatarAsync(IFormFile? file, CancellationToken cancellationToken);
}
