namespace TuCita.Application.Common;

public sealed record ServiceResult<T>(
    bool Succeeded,
    ServiceResultStatus Status,
    T? Data,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<ValidationError> ValidationErrors)
{
    public static ServiceResult<T> Success(T data) => new(true, ServiceResultStatus.Success, data, [], []);

    public static ServiceResult<T> NotFound(string error) => new(false, ServiceResultStatus.NotFound, default, [error], []);

    public static ServiceResult<T> Forbidden(string error) => new(false, ServiceResultStatus.Forbidden, default, [error], []);

    public static ServiceResult<T> Validation(IEnumerable<string> errors)
    {
        var errorList = errors.ToArray();
        return new(false, ServiceResultStatus.Validation, default, errorList, errorList.Select(error => new ValidationError(string.Empty, error)).ToArray());
    }

    public static ServiceResult<T> Validation(IEnumerable<ValidationError> errors)
    {
        var errorList = errors.ToArray();
        return new(false, ServiceResultStatus.Validation, default, errorList.Select(error => error.Message).ToArray(), errorList);
    }
}
