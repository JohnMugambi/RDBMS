namespace RDBMS.WebApi.Models;

/// <summary>
/// Standardized API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int? RowsAffected { get; set; }
    public string? Error { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null, int? rowsAffected = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            RowsAffected = rowsAffected
        };
    }

    public static ApiResponse<T> ErrorResponse(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error
        };
    }
}