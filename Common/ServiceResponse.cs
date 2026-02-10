using System.Text.Json.Serialization;

namespace ECommerce.Api.Common;


public class ServiceResponse<T>
{
    public bool Success { get; }
    public string Message { get; }
    public T? Data { get; }
    public int StatusCode { get; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorDetails? Error { get; }


    public ServiceResponse(bool success, string message, T? data, int statusCode, ErrorDetails? error = null)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
        Error = error;
    }

    public static ServiceResponse<T> Ok(string message, T data, int statusCode = 200)
    {
        return new ServiceResponse<T>(true, message, data, statusCode);
    }

    public static ServiceResponse<T> Fail(string message, int statusCode = 400, ErrorDetails? error = null)
    {
        return new ServiceResponse<T>(false, message, default, statusCode, error);
    }


}



public class ErrorDetails
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Field { get; set; }
}