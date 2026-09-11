namespace EKvarovi.Shared.Common;

public sealed class PriorityLookupDto : LookupDto
{
    public int DefaultResolutionHours { get; set; }
    public string ColorHex { get; set; } = string.Empty;
}
