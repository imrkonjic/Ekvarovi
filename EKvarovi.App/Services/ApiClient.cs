using System.Net.Http.Json;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Assignments;
using EKvarovi.Shared.Dtos.Attachments;
using EKvarovi.Shared.Dtos.Auth;
using EKvarovi.Shared.Dtos.Dashboard;
using EKvarovi.Shared.Dtos.FaultReports;
using EKvarovi.Shared.Dtos.Interventions;
using EKvarovi.Shared.Dtos.Locations;
using EKvarovi.Shared.Dtos.Lookups;
using EKvarovi.Shared.Dtos.Materials;
using EKvarovi.Shared.Dtos.Users;
using EKvarovi.Shared.Enums;

namespace EKvarovi.App.Services;

public sealed class ApiClient(HttpClient http)
{
    public Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
        => SendAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/login", dto, ct);

    public Task<CurrentUserDto> GetMeAsync(CancellationToken ct = default)
        => SendAsync<CurrentUserDto>(HttpMethod.Get, "api/auth/me", ct);

    public Task<AllLookupsDto> GetAllLookupsAsync(CancellationToken ct = default)
        => SendAsync<AllLookupsDto>(HttpMethod.Get, "api/lookups/all", ct);

    public Task<List<LookupDto>> GetLocationTypesAsync(CancellationToken ct = default)
        => SendAsync<List<LookupDto>>(HttpMethod.Get, "api/lookups/location-types", ct);

    public Task<List<LookupDto>> GetFaultTypesAsync(CancellationToken ct = default)
        => SendAsync<List<LookupDto>>(HttpMethod.Get, "api/lookups/fault-types", ct);

    public Task<List<PriorityLookupDto>> GetFaultPrioritiesAsync(CancellationToken ct = default)
        => SendAsync<List<PriorityLookupDto>>(HttpMethod.Get, "api/lookups/fault-priorities", ct);

    public Task<List<LookupDto>> GetFaultStatusesAsync(CancellationToken ct = default)
        => SendAsync<List<LookupDto>>(HttpMethod.Get, "api/lookups/fault-statuses", ct);

    public Task<List<LookupDto>> GetInterventionStatusesAsync(CancellationToken ct = default)
        => SendAsync<List<LookupDto>>(HttpMethod.Get, "api/lookups/intervention-statuses", ct);

    public Task<List<LookupDto>> GetMaterialUnitsAsync(CancellationToken ct = default)
        => SendAsync<List<LookupDto>>(HttpMethod.Get, "api/lookups/material-units", ct);

    public Task<List<LookupDto>> GetRolesAsync(CancellationToken ct = default)
        => SendAsync<List<LookupDto>>(HttpMethod.Get, "api/lookups/roles", ct);

    public Task<PagedResult<FaultReportListDto>> GetFaultReportsAsync(
        FaultReportFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<FaultReportListDto>>(
            HttpMethod.Get, $"api/faultreports{BuildQuery(filter)}", ct);

    public Task<PagedResult<FaultReportListDto>> GetMyFaultReportsAsync(
        FaultReportFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<FaultReportListDto>>(
            HttpMethod.Get, $"api/faultreports/mine{BuildQuery(filter)}", ct);

    public Task<PagedResult<LocationListDto>> GetLocationsAsync(
        LocationFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<LocationListDto>>(
            HttpMethod.Get, $"api/locations{BuildQuery(filter)}", ct);

    public Task<LocationDetailDto> GetLocationAsync(int id, CancellationToken ct = default)
        => SendAsync<LocationDetailDto>(HttpMethod.Get, $"api/locations/{id}", ct);

    public Task<LocationDetailDto> CreateLocationAsync(
        LocationSaveDto dto, CancellationToken ct = default)
        => SendAsync<LocationDetailDto>(HttpMethod.Post, "api/locations", dto, ct);

    public Task<LocationDetailDto> UpdateLocationAsync(
        int id, LocationSaveDto dto, CancellationToken ct = default)
        => SendAsync<LocationDetailDto>(HttpMethod.Put, $"api/locations/{id}", dto, ct);

    public async Task DeleteLocationAsync(int id, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/locations/{id}");
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public Task<List<LookupDto>> GetTechniciansAsync(string? search = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/lookups/technicians"
            : $"api/lookups/technicians?search={Uri.EscapeDataString(search)}";
        return SendAsync<List<LookupDto>>(HttpMethod.Get, url, ct);
    }

    public Task<PagedResult<AssignmentListDto>> GetAssignmentsAsync(
        AssignmentFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<AssignmentListDto>>(
            HttpMethod.Get, $"api/workassignments{BuildQuery(filter)}", ct);

    public Task<PagedResult<AssignmentListDto>> GetMyAssignmentsAsync(
        AssignmentFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<AssignmentListDto>>(
            HttpMethod.Get, $"api/workassignments/mine{BuildQuery(filter)}", ct);

    public Task<PagedResult<InterventionListDto>> GetInterventionsAsync(
        InterventionFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<InterventionListDto>>(
            HttpMethod.Get, $"api/interventions{BuildQuery(filter)}", ct);

    public Task<PagedResult<InterventionListDto>> GetMyInterventionsAsync(
        InterventionFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<InterventionListDto>>(
            HttpMethod.Get, $"api/interventions/mine{BuildQuery(filter)}", ct);

    public Task<InterventionDetailDto> GetInterventionAsync(int id, CancellationToken ct = default)
        => SendAsync<InterventionDetailDto>(HttpMethod.Get, $"api/interventions/{id}", ct);

    public Task<InterventionDetailDto> UpdateInterventionAsync(
        int id, InterventionUpdateDto dto, CancellationToken ct = default)
        => SendAsync<InterventionDetailDto>(HttpMethod.Put, $"api/interventions/{id}", dto, ct);

    public Task<InterventionDetailDto> FinishInterventionAsync(
        int id, InterventionFinishDto dto, CancellationToken ct = default)
        => SendAsync<InterventionDetailDto>(HttpMethod.Put, $"api/interventions/{id}/finish", dto, ct);

    public Task<InterventionDetailDto> CreateInterventionAsync(
        InterventionCreateDto dto, CancellationToken ct = default)
        => SendAsync<InterventionDetailDto>(HttpMethod.Post, "api/interventions", dto, ct);

    public Task<FaultReportDetailDto> CreateFaultReportAsync(
        FaultReportCreateDto dto, CancellationToken ct = default)
        => SendAsync<FaultReportDetailDto>(HttpMethod.Post, "api/faultreports", dto, ct);

    public Task<FaultReportDetailDto> GetFaultReportAsync(int id, CancellationToken ct = default)
        => SendAsync<FaultReportDetailDto>(HttpMethod.Get, $"api/faultreports/{id}", ct);

    public Task<IReadOnlyList<AssignmentListDto>> GetFaultReportAssignmentsAsync(
        int id, CancellationToken ct = default)
        => SendAsync<IReadOnlyList<AssignmentListDto>>(
            HttpMethod.Get, $"api/faultreports/{id}/assignments", ct);

    public Task<IReadOnlyList<InterventionListDto>> GetFaultReportInterventionsAsync(
        int id, CancellationToken ct = default)
        => SendAsync<IReadOnlyList<InterventionListDto>>(
            HttpMethod.Get, $"api/faultreports/{id}/interventions", ct);

    public Task<FaultReportDetailDto> UpdateFaultReportAsync(
        int id, FaultReportUpdateDto dto, CancellationToken ct = default)
        => SendAsync<FaultReportDetailDto>(HttpMethod.Put, $"api/faultreports/{id}", dto, ct);

    public Task<FaultReportDetailDto> TriageFaultReportAsync(
        int id, FaultReportTriageDto dto, CancellationToken ct = default)
        => SendAsync<FaultReportDetailDto>(HttpMethod.Put, $"api/faultreports/{id}/triage", dto, ct);

    public Task<FaultReportDetailDto> CloseFaultReportAsync(
        int id, CloseFaultReportDto dto, CancellationToken ct = default)
        => SendAsync<FaultReportDetailDto>(HttpMethod.Put, $"api/faultreports/{id}/close", dto, ct);

    public Task<FaultReportDetailDto> ReopenFaultReportAsync(
        int id, ReopenFaultReportDto dto, CancellationToken ct = default)
        => SendAsync<FaultReportDetailDto>(HttpMethod.Put, $"api/faultreports/{id}/reopen", dto, ct);

    public Task<AssignmentDetailDto> AssignAsync(AssignmentSaveDto dto, CancellationToken ct = default)
        => SendAsync<AssignmentDetailDto>(HttpMethod.Post, "api/workassignments", dto, ct);

    public Task<AssignmentDetailDto> ReassignAsync(
        int id, ReassignDto dto, CancellationToken ct = default)
        => SendAsync<AssignmentDetailDto>(HttpMethod.Put, $"api/workassignments/{id}/reassign", dto, ct);

    public Task<PagedResult<MaterialListDto>> GetMaterialsAsync(
        MaterialFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<MaterialListDto>>(
            HttpMethod.Get, $"api/materials{BuildQuery(filter)}", ct);

    public Task<MaterialDetailDto> GetMaterialAsync(int id, CancellationToken ct = default)
        => SendAsync<MaterialDetailDto>(HttpMethod.Get, $"api/materials/{id}", ct);

    public Task<MaterialDetailDto> CreateMaterialAsync(
        MaterialSaveDto dto, CancellationToken ct = default)
        => SendAsync<MaterialDetailDto>(HttpMethod.Post, "api/materials", dto, ct);

    public Task<MaterialDetailDto> UpdateMaterialAsync(
        int id, MaterialSaveDto dto, CancellationToken ct = default)
        => SendAsync<MaterialDetailDto>(HttpMethod.Put, $"api/materials/{id}", dto, ct);

    public async Task DeleteMaterialAsync(int id, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/materials/{id}");
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public Task<List<InterventionMaterialDto>> GetInterventionMaterialsAsync(
        int interventionId, CancellationToken ct = default)
        => SendAsync<List<InterventionMaterialDto>>(
            HttpMethod.Get, $"api/interventions/{interventionId}/materials", ct);

    public Task<InterventionMaterialDto> AddInterventionMaterialAsync(
        int interventionId, InterventionMaterialSaveDto dto, CancellationToken ct = default)
        => SendAsync<InterventionMaterialDto>(
            HttpMethod.Post, $"api/interventions/{interventionId}/materials", dto, ct);

    public async Task<AttachmentDto> UploadFaultReportAttachmentAsync(
        int faultReportId,
        AttachmentPurpose purpose,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);
        content.Add(new StringContent(((short)purpose).ToString()), "purpose");

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"api/faultreports/{faultReportId}/attachments")
        {
            Content = content
        };
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<AttachmentDto>(cancellationToken: ct))!;
    }

    public async Task<AttachmentDto> UploadInterventionAttachmentAsync(
        int interventionId,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"api/interventions/{interventionId}/attachments")
        {
            Content = content
        };
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<AttachmentDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAttachmentAsync(int attachmentId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/attachments/{attachmentId}");
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public Task<PagedResult<UserListDto>> GetUsersAsync(
        UserFilterDto filter, CancellationToken ct = default)
        => SendAsync<PagedResult<UserListDto>>(
            HttpMethod.Get, $"api/users{BuildQuery(filter)}", ct);

    public Task<UserDetailDto> GetUserAsync(int id, CancellationToken ct = default)
        => SendAsync<UserDetailDto>(HttpMethod.Get, $"api/users/{id}", ct);

    public Task<UserDetailDto> CreateUserAsync(UserSaveDto dto, CancellationToken ct = default)
        => SendAsync<UserDetailDto>(HttpMethod.Post, "api/users", dto, ct);

    public Task<UserDetailDto> UpdateUserAsync(int id, UserSaveDto dto, CancellationToken ct = default)
        => SendAsync<UserDetailDto>(HttpMethod.Put, $"api/users/{id}", dto, ct);

    public async Task ResetUserPasswordAsync(int id, ResetPasswordDto dto, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/users/{id}/password")
        {
            Content = JsonContent.Create(dto)
        };
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task ActivateUserAsync(int id, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/users/{id}/activate");
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task DeactivateUserAsync(int id, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/users/{id}/deactivate");
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public Task<TransferResultDto> TransferUserAssignmentsAsync(
        int userId, TransferAssignmentsDto dto, CancellationToken ct = default)
        => SendAsync<TransferResultDto>(
            HttpMethod.Post, $"api/users/{userId}/transfer-assignments", dto, ct);

    public Task<DashboardDto> GetDashboardAsync(
        DashboardFilterDto filter, CancellationToken ct = default)
        => SendAsync<DashboardDto>(
            HttpMethod.Get, $"api/dashboard{BuildQuery(filter)}", ct);

    private static string BuildQuery(FaultReportFilterDto filter)
    {
        var parameters = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        Append(parameters, "search", filter.Search);
        Append(parameters, "locationId", filter.LocationId);
        Append(parameters, "faultTypeId", filter.FaultTypeId);
        Append(parameters, "faultPriorityId", filter.FaultPriorityId);
        Append(parameters, "faultStatusId", filter.FaultStatusId);
        Append(parameters, "technicianUserId", filter.TechnicianUserId);
        Append(parameters, "reportedFrom", filter.ReportedFrom);
        Append(parameters, "reportedTo", filter.ReportedTo);
        Append(parameters, "sortBy", filter.SortBy);
        Append(parameters, "sortDir", filter.SortDir);

        if (filter.OnlyOverdue == true)
            parameters.Add("onlyOverdue=true");

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static string BuildQuery(LocationFilterDto filter)
    {
        var parameters = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        Append(parameters, "search", filter.Search);
        Append(parameters, "locationTypeId", filter.LocationTypeId);
        Append(parameters, "sortBy", filter.SortBy);
        Append(parameters, "sortDir", filter.SortDir);

        if (filter.IsActive == true)
            parameters.Add("isActive=true");
        else if (filter.IsActive == false)
            parameters.Add("isActive=false");

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static string BuildQuery(AssignmentFilterDto filter)
    {
        var parameters = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        Append(parameters, "search", filter.Search);
        Append(parameters, "technicianUserId", filter.TechnicianUserId);
        Append(parameters, "isActive", filter.IsActive);
        Append(parameters, "locationId", filter.LocationId);
        Append(parameters, "assignedFrom", filter.AssignedFrom);
        Append(parameters, "assignedTo", filter.AssignedTo);
        Append(parameters, "sortBy", filter.SortBy);
        Append(parameters, "sortDir", filter.SortDir);

        if (filter.OnlyActive == false)
            parameters.Add("onlyActive=false");

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static string BuildQuery(InterventionFilterDto filter)
    {
        var parameters = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        Append(parameters, "search", filter.Search);
        Append(parameters, "interventionStatusId", filter.InterventionStatusId);
        Append(parameters, "technicianUserId", filter.TechnicianUserId);
        Append(parameters, "startedFrom", filter.StartedFrom);
        Append(parameters, "startedTo", filter.StartedTo);
        Append(parameters, "sortBy", filter.SortBy);
        Append(parameters, "sortDir", filter.SortDir);

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static string BuildQuery(MaterialFilterDto filter)
    {
        var parameters = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        Append(parameters, "search", filter.Search);
        Append(parameters, "materialUnitId", filter.MaterialUnitId);
        Append(parameters, "sortBy", filter.SortBy);
        Append(parameters, "sortDir", filter.SortDir);

        if (filter.IsActive == true)
            parameters.Add("isActive=true");
        else if (filter.IsActive == false)
            parameters.Add("isActive=false");

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static string BuildQuery(UserFilterDto filter)
    {
        var parameters = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        Append(parameters, "search", filter.Search);
        Append(parameters, "roleId", filter.RoleId);
        Append(parameters, "locationId", filter.LocationId);
        Append(parameters, "sortBy", filter.SortBy);
        Append(parameters, "sortDir", filter.SortDir);

        if (filter.IsActive == true)
            parameters.Add("isActive=true");
        else if (filter.IsActive == false)
            parameters.Add("isActive=false");

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static string BuildQuery(DashboardFilterDto filter)
    {
        var parameters = new List<string>();
        Append(parameters, "periodFrom", filter.PeriodFrom);
        Append(parameters, "periodTo", filter.PeriodTo);
        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static void Append(List<string> parameters, string name, bool? value)
    {
        if (value.HasValue)
            parameters.Add($"{name}={value.Value.ToString().ToLowerInvariant()}");
    }

    private static void Append(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
    }

    private static void Append(List<string> parameters, string name, int? value)
    {
        if (value.HasValue)
            parameters.Add($"{name}={value.Value}");
    }

    private static void Append(List<string> parameters, string name, DateTime? value)
    {
        if (value.HasValue)
            parameters.Add($"{name}={Uri.EscapeDataString(value.Value.ToString("o"))}");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        using var response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        ApiErrorDto? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(cancellationToken: ct);
        }
        catch
        {
            // API nije vratio ApiErrorDto — koristi se zadana poruka.
        }

        throw ApiClientException.FromResponse((int)response.StatusCode, error);
    }
}
