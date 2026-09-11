using System.Linq.Expressions;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Entities.Common;
using EKvarovi.Api.Entities.Lookups;
using EKvarovi.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<LocationType> LocationTypes => Set<LocationType>();
    public DbSet<FaultType> FaultTypes => Set<FaultType>();
    public DbSet<FaultPriority> FaultPriorities => Set<FaultPriority>();
    public DbSet<FaultStatus> FaultStatuses => Set<FaultStatus>();
    public DbSet<InterventionStatus> InterventionStatuses => Set<InterventionStatus>();
    public DbSet<MaterialUnit> MaterialUnits => Set<MaterialUnit>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<FaultReport> FaultReports => Set<FaultReport>();
    public DbSet<WorkAssignment> WorkAssignments => Set<WorkAssignment>();
    public DbSet<Intervention> Interventions => Set<Intervention>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<InterventionMaterial> InterventionMaterials => Set<InterventionMaterial>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<FaultReportHistory> FaultReportHistories => Set<FaultReportHistory>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserIdOrNull;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedByUserId = userId ?? 0;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = userId;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(AuditableEntity).IsAssignableFrom(t.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Equal(
                Expression.Property(parameter, nameof(AuditableEntity.IsDeleted)),
                Expression.Constant(false));
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
