using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Lookups;

namespace EKvarovi.App.Services;

public sealed class LookupCache(ApiClient api)
{
    private AllLookupsDto? _cache;
    private Task? _loadTask;

    public bool IsLoaded => _cache is not null;

    public IReadOnlyList<LookupDto> LocationTypes => _cache?.LocationTypes ?? [];
    public IReadOnlyList<LookupDto> FaultTypes => _cache?.FaultTypes ?? [];
    public IReadOnlyList<PriorityLookupDto> Priorities => _cache?.FaultPriorities ?? [];
    public IReadOnlyList<LookupDto> FaultStatuses => _cache?.FaultStatuses ?? [];
    public IReadOnlyList<LookupDto> InterventionStatuses => _cache?.InterventionStatuses ?? [];
    public IReadOnlyList<LookupDto> MaterialUnits => _cache?.MaterialUnits ?? [];
    public IReadOnlyList<LookupDto> Roles => _cache?.Roles ?? [];

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_cache is not null)
            return;

        _loadTask ??= LoadCoreAsync(ct);
        await _loadTask;
    }

    public void Clear()
    {
        _cache = null;
        _loadTask = null;
    }

    private async Task LoadCoreAsync(CancellationToken ct)
    {
        _cache = await api.GetAllLookupsAsync(ct);
    }
}
