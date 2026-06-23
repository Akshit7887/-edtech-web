namespace EdTechApi.Services;

public class AppException : Exception
{
    public int StatusCode { get; }
    public object? Details { get; }

    public AppException(int statusCode, string message, object? details = null) : base(message)
    {
        StatusCode = statusCode;
        Details = details;
    }
}
