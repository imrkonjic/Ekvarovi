using System.Linq.Expressions;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Entities.Common;
using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
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
