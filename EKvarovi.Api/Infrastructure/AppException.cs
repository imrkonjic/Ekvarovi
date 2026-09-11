namespace EKvarovi.Api.Infrastructure;

public sealed class AppException : Exception
{
    public int StatusCode { get; }
    public object? Payload { get; }
    public IDictionary<string, string[]>? Errors { get; }

    private AppException(int statusCode, string message,
        object? payload = null,
        IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Payload = payload;
        Errors = errors;
    }

    public static AppException Validation(string message, IDictionary<string, string[]>? errors = null)
        => new(400, message, errors: errors);

    public static AppException Unauthorized(string message) => new(401, message);

    public static AppException Forbidden(string message) => new(403, message);

    public static AppException NotFound(string message) => new(404, message);

    public static AppException Conflict(string message, object? payload = null)
        => new(409, message, payload);

    public static AppException TooLarge(string message) => new(413, message);

    public static AppException UnsupportedMedia(string message) => new(415, message);
}
