namespace EKvarovi.Shared.Common;

public sealed class ApiErrorDto
{
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
    public object? Payload { get; set; }
}
