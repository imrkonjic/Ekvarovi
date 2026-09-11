using EKvarovi.Shared.Common;

namespace EKvarovi.Shared.Dtos.Lookups;

public sealed class AllLookupsDto
{
    public List<LookupDto> LocationTypes { get; set; } = [];
    public List<LookupDto> FaultTypes { get; set; } = [];
    public List<PriorityLookupDto> FaultPriorities { get; set; } = [];
    public List<LookupDto> FaultStatuses { get; set; } = [];
    public List<LookupDto> InterventionStatuses { get; set; } = [];
    public List<LookupDto> MaterialUnits { get; set; } = [];
    public List<LookupDto> Roles { get; set; } = [];
}
