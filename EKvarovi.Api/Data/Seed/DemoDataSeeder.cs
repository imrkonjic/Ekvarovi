using EKvarovi.Api.Entities;
using EKvarovi.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Data.Seed;

public static class DemoDataSeeder
{
    private const string DemoPassword = "Test123!";
    private const int BcryptWorkFactor = 11;

    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration, CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("Seed:Enabled"))
            return;

        await db.Database.MigrateAsync(ct);

        if (!configuration.GetValue<bool>("Seed:DemoData"))
            return;

        if (await db.Users.AnyAsync(ct))
            return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, BcryptWorkFactor);
        var now = DateTime.UtcNow;

        var locations = new[]
        {
            new Location { Code = "UZ-01", Name = "Zgrada Županijske uprave", Address = "Franjevački trg 1", City = "Varaždin", LocationTypeId = 1, IsActive = true },
            new Location { Code = "UZ-02", Name = "Ispostava Ludbreg", Address = "Trg Ludbreg 5", City = "Ludbreg", LocationTypeId = 1, IsActive = true },
            new Location { Code = "SK-01", Name = "OŠ Ivana Kukuljevića", Address = "Školska ulica 12", City = "Varaždin", LocationTypeId = 2, IsActive = true },
            new Location { Code = "SK-02", Name = "Gimnazija Varaždin", Address = "Ivanisa Dalmatina 4", City = "Varaždin", LocationTypeId = 2, IsActive = true },
            new Location { Code = "ZD-01", Name = "Dom zdravlja Novi Marof", Address = "Zdravstvena 3", City = "Novi Marof", LocationTypeId = 3, IsActive = true },
            new Location { Code = "SL-01", Name = "Središnje skladište", Address = "Industrijska cesta 8", City = "Varaždin", LocationTypeId = 4, IsActive = true }
        };

        db.Locations.AddRange(locations);
        await db.SaveChangesAsync(ct);

        var locationIds = await db.Locations
            .Where(l => locations.Select(x => x.Code).Contains(l.Code))
            .ToDictionaryAsync(l => l.Code, l => l.Id, ct);

        var materials = new[]
        {
            new Material { Code = "MAT-001", Name = "Žarulja LED 10W", MaterialUnitId = MaterialUnitIds.Komad, UnitPrice = 3.50m, IsActive = true },
            new Material { Code = "MAT-002", Name = "Osigurač automatski 16A", MaterialUnitId = MaterialUnitIds.Komad, UnitPrice = 6.20m, IsActive = true },
            new Material { Code = "MAT-003", Name = "Kabel NYM 3×1,5", MaterialUnitId = MaterialUnitIds.Metar, UnitPrice = 1.10m, IsActive = true },
            new Material { Code = "MAT-004", Name = "Brtva gumena 1/2\"", MaterialUnitId = MaterialUnitIds.Komad, UnitPrice = 0.45m, IsActive = true },
            new Material { Code = "MAT-005", Name = "Slavina jednoručna", MaterialUnitId = MaterialUnitIds.Komad, UnitPrice = 28.90m, IsActive = true },
            new Material { Code = "MAT-006", Name = "Cijev PPR 20 mm", MaterialUnitId = MaterialUnitIds.Metar, UnitPrice = 2.30m, IsActive = true },
            new Material { Code = "MAT-007", Name = "Termostatska glava", MaterialUnitId = MaterialUnitIds.Komad, UnitPrice = 15.40m, IsActive = true },
            new Material { Code = "MAT-008", Name = "UTP kabel Cat6", MaterialUnitId = MaterialUnitIds.Metar, UnitPrice = 0.85m, IsActive = true },
            new Material { Code = "MAT-009", Name = "Silikon sanitarni", MaterialUnitId = MaterialUnitIds.Komad, UnitPrice = 4.60m, IsActive = true },
            new Material { Code = "MAT-010", Name = "Gips-karton ploča", MaterialUnitId = MaterialUnitIds.Komad, UnitPrice = 9.80m, IsActive = true }
        };

        db.Materials.AddRange(materials);
        await db.SaveChangesAsync(ct);

        var users = new List<User>
        {
            new()
            {
                FirstName = "Ana",
                LastName = "Adminović",
                Email = "admin@ekvarovi.hr",
                PasswordHash = passwordHash,
                HomeLocationId = locationIds["UZ-01"],
                IsActive = true
            },
            new()
            {
                FirstName = "Marko",
                LastName = "Upravić",
                Email = "manager@ekvarovi.hr",
                PasswordHash = passwordHash,
                HomeLocationId = locationIds["UZ-01"],
                IsActive = true
            },
            new()
            {
                FirstName = "Ivan",
                LastName = "Strujić",
                Email = "tehnicar1@ekvarovi.hr",
                PasswordHash = passwordHash,
                Specialization = "Elektrotehnika",
                HomeLocationId = locationIds["UZ-01"],
                IsActive = true
            },
            new()
            {
                FirstName = "Petar",
                LastName = "Vodić",
                Email = "tehnicar2@ekvarovi.hr",
                PasswordHash = passwordHash,
                Specialization = "Vodoinstalacije",
                HomeLocationId = locationIds["SL-01"],
                IsActive = true
            },
            new()
            {
                FirstName = "Luka",
                LastName = "Mrežić",
                Email = "tehnicar3@ekvarovi.hr",
                PasswordHash = passwordHash,
                Specialization = "Informatika",
                HomeLocationId = locationIds["UZ-01"],
                IsActive = true
            },
            new()
            {
                FirstName = "Iva",
                LastName = "Školjić",
                Email = "prijavitelj1@ekvarovi.hr",
                PasswordHash = passwordHash,
                HomeLocationId = locationIds["SK-01"],
                IsActive = true
            },
            new()
            {
                FirstName = "Sara",
                LastName = "Zdravković",
                Email = "prijavitelj2@ekvarovi.hr",
                PasswordHash = passwordHash,
                HomeLocationId = locationIds["ZD-01"],
                IsActive = true
            },
            new()
            {
                FirstName = "Tomislav",
                LastName = "Dvojić",
                Email = "voditelj.tehnicar@ekvarovi.hr",
                PasswordHash = passwordHash,
                HomeLocationId = locationIds["UZ-02"],
                IsActive = true
            },
            new()
            {
                FirstName = "Bivši",
                LastName = "Djelatnik",
                Email = "neaktivan@ekvarovi.hr",
                PasswordHash = passwordHash,
                HomeLocationId = locationIds["SK-02"],
                IsActive = false,
                DeactivatedAt = now
            }
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);

        var userIds = await db.Users
            .Where(u => users.Select(x => x.Email).Contains(u.Email))
            .ToDictionaryAsync(u => u.Email, u => u.Id, ct);

        var roleIds = await db.Roles.ToDictionaryAsync(r => r.Code, r => r.Id, ct);

        var userRoles = new List<UserRole>
        {
            new() { UserId = userIds["admin@ekvarovi.hr"], RoleId = roleIds[Roles.Admin], AssignedAt = now },
            new() { UserId = userIds["manager@ekvarovi.hr"], RoleId = roleIds[Roles.Manager], AssignedAt = now },
            new() { UserId = userIds["tehnicar1@ekvarovi.hr"], RoleId = roleIds[Roles.Technician], AssignedAt = now },
            new() { UserId = userIds["tehnicar2@ekvarovi.hr"], RoleId = roleIds[Roles.Technician], AssignedAt = now },
            new() { UserId = userIds["tehnicar3@ekvarovi.hr"], RoleId = roleIds[Roles.Technician], AssignedAt = now },
            new() { UserId = userIds["prijavitelj1@ekvarovi.hr"], RoleId = roleIds[Roles.Reporter], AssignedAt = now },
            new() { UserId = userIds["prijavitelj2@ekvarovi.hr"], RoleId = roleIds[Roles.Reporter], AssignedAt = now },
            new() { UserId = userIds["voditelj.tehnicar@ekvarovi.hr"], RoleId = roleIds[Roles.Manager], AssignedAt = now },
            new() { UserId = userIds["voditelj.tehnicar@ekvarovi.hr"], RoleId = roleIds[Roles.Technician], AssignedAt = now },
            new() { UserId = userIds["neaktivan@ekvarovi.hr"], RoleId = roleIds[Roles.Reporter], AssignedAt = now }
        };

        db.UserRoles.AddRange(userRoles);
        await db.SaveChangesAsync(ct);
    }
}
