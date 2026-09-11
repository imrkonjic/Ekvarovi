using System.Linq.Expressions;
using EKvarovi.Api.Data;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure;
using EKvarovi.Api.Services.Abstractions;
using EKvarovi.Shared.Common;
using EKvarovi.Shared.Dtos.Users;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Services;

public sealed class UserService(AppDbContext db) : IUserService
{
    private const int BcryptWorkFactor = 11;
    private const string NotFoundMessage = "Korisnik nije pronađen.";
    private const string DuplicateEmailMessage = "Korisnik s tim e-mailom već postoji.";

    private static readonly Dictionary<string, Expression<Func<User, object>>> SortMap = new()
    {
        ["firstName"] = u => u.FirstName,
        ["lastName"] = u => u.LastName,
        ["email"] = u => u.Email,
        ["isActive"] = u => u.IsActive
    };

    public async Task<PagedResult<UserListDto>> SearchAsync(
        UserFilterDto filter, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(u =>
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term) ||
                u.Email.Contains(term) ||
                (u.Phone != null && u.Phone.Contains(term)) ||
                (u.Specialization != null && u.Specialization.Contains(term)));
        }

        if (filter.RoleId is int roleId)
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));

        if (filter.LocationId is int locationId)
            query = query.Where(u => u.HomeLocationId == locationId);

        if (filter.IsActive is bool isActive)
            query = query.Where(u => u.IsActive == isActive);

        query = query.ApplySort(filter.SortBy, filter.SortDir, SortMap, "lastName");

        return await query
            .Select(u => new UserListDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                HomeLocationId = u.HomeLocationId,
                HomeLocationName = u.HomeLocation != null ? u.HomeLocation.Name : null,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToPagedResultAsync(filter, ct);
    }

    public async Task<UserDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
        => await ProjectDetail(db.Users.AsNoTracking().Where(u => u.Id == id))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound(NotFoundMessage);

    public async Task<UserDetailDto> CreateAsync(UserSaveDto dto, CancellationToken ct = default)
    {
        await ValidateSaveDtoAsync(dto, requirePassword: true, ct);

        if (await db.Users.AnyAsync(u => u.Email == dto.Email.Trim(), ct))
            throw AppException.Conflict(DuplicateEmailMessage);

        var user = MapToEntity(new User(), dto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password!, BcryptWorkFactor);
        user.IsActive = true;

        SyncRoles(user, dto.RoleIds);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(user.Id, ct);
    }

    public async Task<UserDetailDto> UpdateAsync(int id, UserSaveDto dto, CancellationToken ct = default)
    {
        await ValidateSaveDtoAsync(dto, requirePassword: false, ct);

        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        var email = dto.Email.Trim();
        if (await db.Users.AnyAsync(u => u.Id != id && u.Email == email, ct))
            throw AppException.Conflict(DuplicateEmailMessage);

        MapToEntity(user, dto);
        SyncRoles(user, dto.RoleIds);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            throw AppException.Validation("Nova lozinka je obavezna.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, BcryptWorkFactor);
        await db.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound(NotFoundMessage);

        user.IsActive = true;
        user.DeactivatedAt = null;
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<UserDetailDto> ProjectDetail(IQueryable<User> query)
        => query.Select(u => new UserDetailDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Phone = u.Phone,
            Specialization = u.Specialization,
            HomeLocationId = u.HomeLocationId,
            HomeLocationName = u.HomeLocation != null ? u.HomeLocation.Name : null,
            IsActive = u.IsActive,
            DeactivatedAt = u.DeactivatedAt,
            RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList(),
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        });

    private async Task ValidateSaveDtoAsync(UserSaveDto dto, bool requirePassword, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.FirstName))
            errors["firstName"] = ["Ime je obavezno."];

        if (string.IsNullOrWhiteSpace(dto.LastName))
            errors["lastName"] = ["Prezime je obavezno."];

        if (string.IsNullOrWhiteSpace(dto.Email))
            errors["email"] = ["E-mail je obavezan."];

        if (requirePassword && string.IsNullOrWhiteSpace(dto.Password))
            errors["password"] = ["Lozinka je obavezna pri kreiranju korisnika."];

        if (dto.RoleIds.Count == 0)
            errors["roleIds"] = ["Korisnik mora imati barem jednu ulogu."];

        if (errors.Count > 0)
            throw AppException.Validation("Neispravni podaci korisnika.", errors);

        var distinctRoleIds = dto.RoleIds.Distinct().ToList();
        var existingRoleCount = await db.Roles.CountAsync(r => distinctRoleIds.Contains(r.Id), ct);

        if (existingRoleCount != distinctRoleIds.Count)
            throw AppException.Validation("Jedna ili više odabranih uloga ne postoji.");

        if (dto.HomeLocationId is int locationId)
        {
            var locationValid = await db.Locations
                .AnyAsync(l => l.Id == locationId && l.IsActive, ct);

            if (!locationValid)
                throw AppException.Validation("Odabrana matična lokacija ne postoji ili nije aktivna.");
        }
    }

    private static User MapToEntity(User user, UserSaveDto dto)
    {
        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Email = dto.Email.Trim();
        user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        user.Specialization = string.IsNullOrWhiteSpace(dto.Specialization) ? null : dto.Specialization.Trim();
        user.HomeLocationId = dto.HomeLocationId;
        return user;
    }

    private void SyncRoles(User user, IReadOnlyList<int> roleIds)
    {
        var desiredRoleIds = roleIds.Distinct().ToHashSet();
        var existingRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();

        foreach (var userRole in user.UserRoles.Where(ur => !desiredRoleIds.Contains(ur.RoleId)).ToList())
            db.UserRoles.Remove(userRole);

        foreach (var roleId in desiredRoleIds.Except(existingRoleIds))
        {
            user.UserRoles.Add(new UserRole
            {
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });
        }
    }
}
