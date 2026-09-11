using EKvarovi.Shared.Common;

namespace EKvarovi.App.Services;

public sealed class ApiClientException : Exception
{
    public int StatusCode { get; }
    public ApiErrorDto? Error { get; }

    public ApiClientException(string message, int statusCode, ApiErrorDto? error = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Error = error;
    }

    public static ApiClientException FromResponse(int statusCode, ApiErrorDto? error)
        => new(FormatMessage(error, statusCode), statusCode, error);

    public static string FormatMessage(ApiErrorDto? error, int statusCode)
    {
        if (error is null)
            return DefaultMessage(statusCode);

        if (error.Errors is { Count: > 0 })
        {
            var details = string.Join(" ", error.Errors.SelectMany(e => e.Value));
            if (!string.IsNullOrWhiteSpace(details))
                return string.IsNullOrWhiteSpace(error.Message) ? details : $"{error.Message} {details}";
        }

        return string.IsNullOrWhiteSpace(error.Message) ? DefaultMessage(statusCode) : error.Message;
    }

    private static string DefaultMessage(int statusCode) => statusCode switch
    {
        400 => "Neispravan zahtjev.",
        401 => "Niste prijavljeni.",
        403 => "Nemate dozvolu za tu radnju.",
        404 => "Traženi resurs nije pronađen.",
        409 => "Radnja nije moguća zbog sukoba podataka.",
        _ => "Greška pri pozivu API-ja."
    };
}
