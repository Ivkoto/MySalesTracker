namespace MySalesTracker.Application.DTOs;

public sealed record ServiceResult<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }
    public string? SuccessMessage { get; }

    private ServiceResult(bool success, T? data, string? errorMessage, string? successMessage)
    {
          Success = success;
          Data = data;
          ErrorMessage = errorMessage;
          SuccessMessage = successMessage;
    }

    public static ServiceResult<T> SuccessResult(T? data)
        => new (true, data, null, null);

    public static ServiceResult<T> SuccessResult(T? data, string message)
        => new (true, data, null, message);

    public static ServiceResult<T> FailureResult(string? message)
        => new(false, default, message, null);

    public static ServiceResult<T> FailureResult(T? data, string? message)
        => new(false, data, message, null);
}
