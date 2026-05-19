namespace LoanSuperMarket.Shared.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public T? Data { get; init; }

    public List<string> Errors { get; init; } = [];

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = [error]
        };
    }

    public static ApiResponse<T> Fail(List<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors
        };
    }
}