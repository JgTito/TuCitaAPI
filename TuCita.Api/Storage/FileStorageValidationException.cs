namespace TuCita.Api.Storage;

public sealed class FileStorageValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
