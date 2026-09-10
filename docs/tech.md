# Tehnička dokumentacija — e-Kvarovi Županije

| | |
|---|---|
| **Verzija** | 1.0 |
| **Datum** | 9. rujna 2026. |
| **Prateći dokumenti** | [prd.md](prd.md) — zahtjevi i opseg · [shema.md](shema.md) — baza podataka |

Ovaj dokument opisuje kako se zahtjevi iz PRD-a realiziraju: arhitekturu, strukturu koda, konvencije te konkretna rješenja za dijelove koji nose najveći rizik.

---

## Sadržaj

1. [Arhitektura rješenja](#1-arhitektura-rješenja)
2. [Struktura mapa](#2-struktura-mapa)
3. [Tijek zahtjeva kroz slojeve](#3-tijek-zahtjeva-kroz-slojeve)
4. [Konfiguracija i pokretanje](#4-konfiguracija-i-pokretanje)
5. [Pristup podacima](#5-pristup-podacima)
6. [Autentikacija i autorizacija](#6-autentikacija-i-autorizacija)
7. [Filtriranje, sortiranje i straničenje](#7-filtriranje-sortiranje-i-straničenje)
8. [Transakcijske operacije](#8-transakcijske-operacije)
9. [Rad s datotekama](#9-rad-s-datotekama)
10. [Obrada grešaka](#10-obrada-grešaka)
11. [Blazor klijent](#11-blazor-klijent)
12. [Isporuka](#12-isporuka)
13. [Konvencije](#13-konvencije)
14. [Kontrolna lista rizika](#14-kontrolna-lista-rizika)

---

## 1. Arhitektura rješenja

Tri projekta u jednom rješenju:

```
EKvarovi.sln
├── EKvarovi.Api      ASP.NET Core Web API (.NET 9)
├── EKvarovi.App      Blazor WebAssembly (standalone)
└── EKvarovi.Shared   razredna biblioteka: DTO-ovi, enumi, konstante
```

`EKvarovi.Shared` referenciraju oba projekta. Time su ugovori API-ja definirani na jednom mjestu, pa nema ručnog prepisivanja modela na klijentu ni razilaženja naziva polja.

Unutar API-ja koriste se tri sloja, bez zasebnih projekata jer bi za ovu veličinu bili nepotreban režijski trošak:

| Sloj | Odgovornost | Što tu **ne** smije biti |
|---|---|---|
| **Controllers** | Rute, model binding, autorizacijski atributi, mapiranje rezultata u HTTP status | Poslovna pravila, LINQ upiti, pristup `DbContext`-u |
| **Services** | Poslovna pravila, transakcije, upiti, mapiranje entiteta u DTO | Ovisnost o `HttpContext`-u (osim kroz `ICurrentUserService`) |
| **Data** | `DbContext`, konfiguracije entiteta, migracije, seed | Poslovna logika |

Servisi se registriraju kao `Scoped` i injektiraju u kontrolere kroz sučelja (`IFaultReportService`, `IWorkAssignmentService`, …). Kontroleri su tanki — tipičan kontroler ima 5 do 10 redaka po akciji.

### 1.1 NuGet paketi

| Projekt | Paket | Svrha |
|---|---|---|
| Api | `Npgsql.EntityFrameworkCore.PostgreSQL` | EF Core pružatelj za PostgreSQL |
| Api | `Microsoft.EntityFrameworkCore.Design` | Alati za migracije |
| Api | `EFCore.NamingConventions` | Automatski `snake_case` nazivi u bazi |
| Api | `Microsoft.AspNetCore.Authentication.JwtBearer` | Validacija JWT-a |
| Api | `BCrypt.Net-Next` | Hashiranje lozinki |
| Api | `Swashbuckle.AspNetCore` | Swagger UI s podrškom za Bearer token |
| App | `MudBlazor` | UI komponente |
| App | `Blazored.LocalStorage` | Pohrana tokena |
| App | `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | Infrastruktura stanja autentikacije |

Verzije paketa uskladiti s ciljanim .NET 9 (glavni broj verzije EF Core i ASP.NET paketa mora odgovarati verziji SDK-a).

---

## 2. Struktura mapa

```
EKvarovi.Api/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── LocationsController.cs
│   ├── FaultReportsController.cs
│   ├── WorkAssignmentsController.cs
│   ├── InterventionsController.cs
│   ├── MaterialsController.cs
│   ├── AttachmentsController.cs
│   ├── LookupsController.cs
│   ├── DashboardController.cs
│   └── AiController.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── Configurations/          (jedna klasa po entitetu)
│   ├── Seed/
│   │   ├── LookupSeed.cs        (šifrarnici s fiksnim ID-evima)
│   │   └── DemoDataSeeder.cs    (korisnici, lokacije, prijave…)
│   └── Migrations/
├── Entities/
│   ├── Common/AuditableEntity.cs
│   ├── Lookups/                 (7 šifrarnika)
│   └── …                        (10 entiteta jezgre)
├── Services/
│   ├── Abstractions/            (sučelja svih servisa)
│   ├── AuthService.cs
│   ├── FaultReportService.cs
│   ├── WorkAssignmentService.cs
│   ├── InterventionService.cs
│   ├── AttachmentService.cs
│   ├── DashboardService.cs
│   ├── Ai/{IAiService,MockAiService}.cs
│   └── Storage/{IFileStorage,LocalDiskFileStorage}.cs
├── Infrastructure/
│   ├── CurrentUserService.cs
│   ├── ExceptionHandlingMiddleware.cs
│   ├── AppException.cs
│   └── QueryableExtensions.cs   (straničenje i sortiranje)
└── Properties/launchSettings.json

EKvarovi.App/
├── Program.cs
├── App.razor, MainLayout.razor, NavMenu.razor
├── Pages/                       (14 stranica iz PRD-a)
├── Components/
│   ├── FilterBar.razor
│   ├── PriorityBadge.razor, StatusBadge.razor
│   ├── AttachmentGallery.razor
│   ├── ConfirmDialog.razor
│   └── AssignDialog.razor, ReassignDialog.razor, FinishInterventionDialog.razor
├── Services/
│   ├── ApiClient.cs             (tipizirani pozivi prema API-ju)
│   ├── AuthService.cs
│   ├── JwtAuthStateProvider.cs
│   ├── AuthHeaderHandler.cs
│   └── LookupCache.cs
└── wwwroot/

EKvarovi.Shared/
├── Dtos/                        (po modulu: Auth, FaultReports, Assignments…)
├── Common/PagedResult.cs, LookupDto.cs, ApiErrorDto.cs
└── Enums/AttachmentPurpose.cs, HistoryChangeType.cs, FaultStatusId.cs…
```

**Fiksni identifikatori šifrarnika** drže se kao statički razredi u `EKvarovi.Shared/Enums`, kako bi ih i API i klijent koristili bez pogađanja:

```csharp
public static class FaultStatusIds
{
    public const int Zaprimljeno = 1;
    public const int Pregledano  = 2;
    public const int Dodijeljeno = 3;
    public const int URadu       = 4;
    public const int Rijeseno    = 5;
    public const int Zatvoreno   = 6;
}
```

---

## 3. Tijek zahtjeva kroz slojeve

Primjer: izvršitelj završava intervenciju kao uspješnu.

```
Blazor InterventionDetail.razor
   │  ApiClient.FinishInterventionAsync(id, dto)
   ▼
AuthHeaderHandler  ──► dodaje Authorization: Bearer <token>
   ▼
InterventionsController.Finish(id, dto)
   │  [Authorize(Roles = "Technician,Admin")]
   ▼
InterventionService.FinishAsync(id, dto, ct)
   │  1. dohvat intervencije s nalogom i prijavom
   │  2. BR-33: je li pozivatelj vlasnik aktivne dodjele        → AppException.Forbidden
   │  3. BR-32: je li već završena                              → AppException.Conflict
   │  4. BR-27/28/29: vremena, bilješka, razlog neuspjeha       → AppException.Validation
   │  5. postavi status intervencije i vremena
   │  6. BR-30: ako je uspješna → prijava u Riješeno, ResolvedAt
   │  7. upis u FaultReportHistory
   │  8. SaveChanges u jednoj transakciji
   ▼
   vraća InterventionDetailDto
   ▼
ExceptionHandlingMiddleware  ──► pretvara AppException u 400/403/409 + ApiErrorDto
```

Pravilo bez iznimke: **svaka provjera iz poglavlja 8 PRD-a živi u servisu**. Kontroler smije imati samo `[Authorize]` atribut, a UI smije samo sakriti gumb — nijedno od toga nije zamjena za provjeru u servisu.

---

## 4. Konfiguracija i pokretanje

### 4.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=ekvarovi;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "EKvarovi.Api",
    "Audience": "EKvarovi.App",
    "Key": "postavi-dugacki-tajni-kljuc-od-najmanje-32-znaka",
    "ExpiryHours": 8
  },
  "FileStorage": {
    "RootPath": "App_Data/uploads",
    "MaxImageSizeBytes": 5242880,
    "MaxDocumentSizeBytes": 10485760
  },
  "Cors": {
    "AllowedOrigins": [ "https://localhost:7226" ]
  },
  "Seed": {
    "Enabled": true,
    "DemoData": true
  }
}
```

> **Napomena (CORS):** generirani Blazor WASM projekt koristi portove `https://localhost:7097` i `http://localhost:5204`. U `appsettings.json` koristiti te adrese umjesto `7226` ako CORS pri lokalnom razvoju ne prolazi.

Sve vrijednosti mogu se pregaziti varijablama okoline po standardnoj konvenciji s dvostrukom podvlakom, primjerice `ConnectionStrings__Default` ili `Jwt__Key`. Tajni ključ nikad ne ide u repozitorij s pravom vrijednošću — lokalno se drži u korisničkim tajnama (`dotnet user-secrets`), a u oblaku u varijablama okoline.

### 4.2 Program.cs — kostur

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFaultReportService, FaultReportService>();
// … ostali servisi
builder.Services.AddSingleton<IFileStorage, LocalDiskFileStorage>();
builder.Services.AddScoped<IAiService, MockAiService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* v. poglavlje 6 */ });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(/* v. poglavlje 6.4 */);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DemoDataSeeder.SeedAsync(db, app.Configuration);
}

app.Run();
```

Redoslijed međusloja je bitan: obrada grešaka mora biti prva da bi uhvatila i greške iz autentikacije, a `UseAuthentication` uvijek ide prije `UseAuthorization`.

### 4.3 Pokretanje lokalno

```powershell
docker compose up -d db          # samo baza
dotnet ef database update --project EKvarovi.Api
dotnet run --project EKvarovi.Api      # https://localhost:7000, Swagger na /swagger
dotnet run --project EKvarovi.App      # https://localhost:7226
```

Migracije se ionako primjenjuju pri pokretanju API-ja, pa je drugi korak potreban samo kad želiš bazu pripremiti unaprijed.

---

## 5. Pristup podacima

### 5.1 Konvencija imenovanja

`UseSnakeCaseNamingConvention()` prevodi `FaultReport.ReportedByUserId` u `fault_reports.reported_by_user_id`. Time su svi identifikatori u PostgreSQL-u malim slovima i ne treba ih navodnikovati — što je izravno rješenje za problem koji inače iskoči kod ručno pisanih SQL izraza, primjerice u filtru parcijalnog indeksa.

### 5.2 Bazna klasa i globalni filtri

```csharp
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

Globalni filtar postavlja se za svaki entitet koji nasljeđuje baznu klasu, refleksijom u `OnModelCreating`:

```csharp
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
```

> **Zamka:** EF Core zahtijeva da filtar postoji i na ovisnom entitetu ako postoji na glavnom, inače pri učitavanju veza javlja upozorenje. Budući da filtar dobivaju svi nasljednici bazne klase, to je zadovoljeno. Kad treba dohvatiti i obrisane zapise, koristi se `IgnoreQueryFilters()`.

### 5.3 Automatsko popunjavanje polja praćenja

```csharp
public override Task<int> SaveChangesAsync(CancellationToken ct = default)
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
                entry.State = EntityState.Modified;   // logičko brisanje
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                break;
        }
    }

    return base.SaveChangesAsync(ct);
}
```

Presretanje stanja `Deleted` znači da poziv `Remove()` nikad fizički ne briše zapis — pravilo BR-07 i srodna ostaju zadovoljena čak i ako se negdje omakne izravni poziv.

### 5.4 Najviše jedna aktivna dodjela

Ovo je najvažnije ograničenje u cijelom modelu i provodi se na dvije razine.

**U bazi**, parcijalnim jedinstvenim indeksom:

```csharp
builder.HasIndex(a => a.FaultReportId)
       .HasFilter("is_active AND NOT is_deleted")
       .IsUnique()
       .HasDatabaseName("ux_work_assignments_active_per_report");
```

**U servisu**, provjerom prije upisa, kako bi korisnik dobio razumljivu poruku umjesto greške baze:

```csharp
var hasActive = await _db.WorkAssignments
    .AnyAsync(a => a.FaultReportId == dto.FaultReportId && a.IsActive, ct);

if (hasActive)
    throw AppException.Conflict("Prijava već ima aktivnu dodjelu. Koristite re-dodjelu.");
```

Indeks je zaštita od utrke dvaju istovremenih zahtjeva; provjera u servisu je ono što korisnik zapravo vidi.

### 5.5 Uvjet nad privicima

Namjena privitka mora odgovarati tome na što je vezan:

```csharp
builder.ToTable(t => t.HasCheckConstraint("ck_attachments_purpose_target",
    """
    (purpose IN (1, 3) AND fault_report_id IS NOT NULL AND intervention_id IS NULL)
    OR
    (purpose = 2 AND intervention_id IS NOT NULL AND fault_report_id IS NULL)
    """));
```

### 5.6 Generiranje broja prijave

Format je `KV-{godina}-{6 znamenki}`, a brojač kreće od jedinice svake godine. Zasebna tablica brojača nije potrebna — dovoljan je upit nad postojećim brojevima unutar iste transakcije, uz jedinstveni indeks kao zaštitu:

```csharp
var year = DateTime.UtcNow.Year;
var prefix = $"KV-{year}-";

var lastNumber = await _db.FaultReports
    .IgnoreQueryFilters()
    .Where(r => r.ReportNumber.StartsWith(prefix))
    .OrderByDescending(r => r.ReportNumber)
    .Select(r => r.ReportNumber)
    .FirstOrDefaultAsync(ct);

var next = lastNumber is null ? 1 : int.Parse(lastNumber[^6..]) + 1;
report.ReportNumber = prefix + next.ToString("D6");
```

Jedinstveni indeks nad `report_number` osigurava da u rijetkom slučaju istovremenog upisa drugi zahtjev padne, a ne da nastanu dva ista broja. Kod ovog opsega korištenja to je prihvatljivo; za veći promet uzeo bi se `CREATE SEQUENCE` po godini.

---

## 6. Autentikacija i autorizacija

### 6.1 Postav JWT-a

```csharp
var jwt = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            // ključno za rad [Authorize(Roles = "...")] i za čitanje identiteta
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
    });
```

> **Rizik broj jedan iz PRD-a.** Ako `RoleClaimType` ne odgovara nazivu claima koji se stavlja u token, `[Authorize(Roles = "Manager")]` će uvijek vraćati 403 iako je uloga u tokenu. Zato se u tokenu koristi točno `ClaimTypes.Role`, a ovdje se isti naziv izrijekom navodi. Drugi čest uzrok istog simptoma je ASP.NET-ovo automatsko preslikavanje naziva claimova; isključuje se s `JwtSecurityTokenHandler.DefaultMapInboundClaims = false;` na početku `Program.cs`.

### 6.2 Izdavanje tokena s više uloga

```csharp
public string GenerateToken(User user, IReadOnlyList<string> roleCodes)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new("fullName", $"{user.FirstName} {user.LastName}")
    };

    if (user.HomeLocationId is int locationId)
        claims.Add(new Claim("homeLocationId", locationId.ToString()));

    // jedan claim po ulozi — tako multi-role radi bez ikakvog dodatnog koda
    claims.AddRange(roleCodes.Select(code => new Claim(ClaimTypes.Role, code)));

    var token = new JwtSecurityToken(
        issuer: _cfg["Jwt:Issuer"],
        audience: _cfg["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(int.Parse(_cfg["Jwt:ExpiryHours"]!)),
        signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

Korisnik s ulogama Manager i Technician dobiva dva `role` claima. `[Authorize(Roles = "Manager")]` i `[Authorize(Roles = "Technician")]` tada oba prolaze, čime je pravilo BR-51 zadovoljeno bez posebne logike.

### 6.3 Čitanje identiteta — jedini dopušteni izvor

```csharp
public interface ICurrentUserService
{
    int UserId { get; }                 // baca ako nije prijavljen
    int? UserIdOrNull { get; }
    bool IsInRole(string roleCode);
    bool IsAdminOrManager { get; }
}

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public int? UserIdOrNull =>
        int.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public int UserId => UserIdOrNull
        ?? throw AppException.Unauthorized("Korisnik nije prijavljen.");

    public bool IsInRole(string roleCode) => User?.IsInRole(roleCode) ?? false;

    public bool IsAdminOrManager => IsInRole(Roles.Admin) || IsInRole(Roles.Manager);
}
```

Endpointi s nastavkom `/mine` **nikad** ne primaju identifikator korisnika kao parametar (pravilo BR-50):

```csharp
[HttpGet("mine")]
[Authorize]
public async Task<PagedResult<FaultReportListDto>> GetMine(
    [FromQuery] FaultReportFilterDto filter, CancellationToken ct)
    => await _service.GetMineAsync(filter, ct);   // servis sam poseže za _currentUser.UserId
```

### 6.4 Filtar vidljivosti po ulozi

Umjesto da se pravila BR-47 i BR-48 ponavljaju u svakoj metodi, vidljivost se centralizira u jednoj metodi proširenja:

```csharp
public static IQueryable<FaultReport> VisibleTo(
    this IQueryable<FaultReport> query, ICurrentUserService user)
{
    if (user.IsAdminOrManager)
        return query;

    var id = user.UserId;

    return query.Where(r =>
        r.ReportedByUserId == id ||                                    // Reporter: svoje prijave
        r.WorkAssignments.Any(a => a.TechnicianUserId == id));         // Technician: sve na kojima ima ili je imao dodjelu
}
```

Uvjet nad dodjelama namjerno ne provjerava `IsActive` — izvršitelj po pravilu BR-48 zadržava pristup i povijesnim, zatvorenim nalozima.

### 6.5 Swagger s Bearer tokenom

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Zalijepi samo token, bez prefiksa Bearer."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});
```

Budući da automatiziranih testova nema, Swagger je glavni alat za provjeru poslovnih pravila. Svako pravilo iz poglavlja 8 PRD-a treba barem jednom namjerno prekršiti kroz Swagger i potvrditi da vraća očekivani status i poruku.

---

## 7. Filtriranje, sortiranje i straničenje

### 7.1 Zajednički ugovori

```csharp
public abstract class PagedRequest
{
    private int _pageSize = 10;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? 10 : value;   // BR-53
    }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }        // "asc" | "desc"
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

### 7.2 Metode proširenja

```csharp
public static class QueryableExtensions
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortBy,
        string? sortDir,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> allowed,
        string defaultKey)
    {
        // nepoznata vrijednost tiho pada na zadano sortiranje (BR-53)
        var key = sortBy is not null && allowed.ContainsKey(sortBy) ? sortBy : defaultKey;
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return descending
            ? query.OrderByDescending(allowed[key])
            : query.OrderBy(allowed[key]);
    }

    public static async Task<PagedResult<TDto>> ToPagedResultAsync<TDto>(
        this IQueryable<TDto> query, PagedRequest request, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
```

### 7.3 Primjena na popisu prijava

```csharp
public async Task<PagedResult<FaultReportListDto>> SearchAsync(
    FaultReportFilterDto f, bool onlyMine, CancellationToken ct)
{
    if (f.ReportedFrom > f.ReportedTo)                                    // BR-55
        throw AppException.Validation("Početni datum ne smije biti nakon završnog.");

    var now = DateTime.UtcNow;

    var query = _db.FaultReports.AsNoTracking().VisibleTo(_currentUser);

    if (onlyMine)
        query = query.Where(r => r.ReportedByUserId == _currentUser.UserId);

    if (!string.IsNullOrWhiteSpace(f.Search))
    {
        var term = $"%{f.Search.Trim()}%";
        query = query.Where(r =>
            EF.Functions.ILike(r.ReportNumber, term) ||
            EF.Functions.ILike(r.Title, term) ||
            EF.Functions.ILike(r.Description, term));
    }

    if (f.LocationId is int loc)          query = query.Where(r => r.LocationId == loc);
    if (f.FaultTypeId is int type)        query = query.Where(r => r.FaultTypeId == type);
    if (f.FaultPriorityId is int prio)    query = query.Where(r => r.FaultPriorityId == prio);
    if (f.FaultStatusId is int status)    query = query.Where(r => r.FaultStatusId == status);
    if (f.ReportedFrom is DateTime from)  query = query.Where(r => r.ReportedAt >= from);
    if (f.ReportedTo is DateTime to)      query = query.Where(r => r.ReportedAt <= to);

    if (f.TechnicianUserId is int tech)
        query = query.Where(r => r.WorkAssignments
            .Any(a => a.IsActive && a.TechnicianUserId == tech));

    if (f.OnlyOverdue == true)
        query = query.Where(r => r.DueDate != null && r.DueDate < now
                                 && r.FaultStatusId != FaultStatusIds.Rijeseno
                                 && r.FaultStatusId != FaultStatusIds.Zatvoreno);

    var sortMap = new Dictionary<string, Expression<Func<FaultReport, object>>>
    {
        ["reportedAt"]  = r => r.ReportedAt,
        ["dueDate"]     = r => r.DueDate!,
        ["priority"]    = r => r.FaultPriorityId!,
        ["status"]      = r => r.FaultStatusId,
        ["location"]    = r => r.Location.Name,
        ["reportNumber"]= r => r.ReportNumber
    };

    return await query
        .ApplySort(f.SortBy, f.SortDir, sortMap, defaultKey: "reportedAt")   // BR-54
        .Select(r => new FaultReportListDto
        {
            Id = r.Id,
            ReportNumber = r.ReportNumber,
            Title = r.Title,
            LocationName = r.Location.Name,
            FaultTypeName = r.FaultType != null ? r.FaultType.Name : null,
            PriorityName = r.FaultPriority != null ? r.FaultPriority.Name : null,
            PriorityColor = r.FaultPriority != null ? r.FaultPriority.ColorHex : null,
            StatusName = r.FaultStatus.Name,
            DueDate = r.DueDate,
            IsOverdue = r.DueDate != null && r.DueDate < now
                        && r.FaultStatusId != FaultStatusIds.Rijeseno
                        && r.FaultStatusId != FaultStatusIds.Zatvoreno,
            TechnicianName = r.WorkAssignments
                .Where(a => a.IsActive)
                .Select(a => a.Technician.FirstName + " " + a.Technician.LastName)
                .FirstOrDefault(),
            ReportedAt = r.ReportedAt
        })
        .ToPagedResultAsync(f, ct);
}
```

Projekcija u DTO ide **prije** straničenja, pa EF generira jedan `SELECT` s `LIMIT` i `OFFSET` i ne učitava cijele entitete. Isti obrazac ponavlja se za intervencije, naloge, lokacije, materijale i korisnike.

---

## 8. Transakcijske operacije

Četiri operacije mijenjaju više entiteta odjednom i moraju biti atomarne.

### 8.1 Dodjela s automatskim prijelazom statusa

```csharp
public async Task<AssignmentDetailDto> AssignAsync(AssignmentSaveDto dto, CancellationToken ct)
{
    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    var report = await _db.FaultReports
        .Include(r => r.WorkAssignments)
        .FirstOrDefaultAsync(r => r.Id == dto.FaultReportId, ct)
        ?? throw AppException.NotFound("Prijava nije pronađena.");

    if (report.FaultStatusId == FaultStatusIds.Zatvoreno)                      // BR-12
        throw AppException.Conflict("Zatvorena prijava se ne može mijenjati.");

    if (report.FaultTypeId is null || report.FaultPriorityId is null)          // BR-20
        throw AppException.Validation("Prijava mora imati određenu vrstu i prioritet prije dodjele.");

    if (report.WorkAssignments.Any(a => a.IsActive))                           // BR-15
        throw AppException.Conflict("Prijava već ima aktivnu dodjelu. Koristite re-dodjelu.");

    await EnsureIsActiveTechnicianAsync(dto.TechnicianUserId, ct);             // BR-16

    var assignment = new WorkAssignment
    {
        FaultReportId = report.Id,
        TechnicianUserId = dto.TechnicianUserId,
        AssignedByUserId = _currentUser.UserId,
        AssignedAt = DateTime.UtcNow,
        IsActive = true,
        Note = dto.Note
    };
    _db.WorkAssignments.Add(assignment);

    // BR-10: jedini dopušteni preskok — do statusa Dodijeljeno u jednom potezu
    if (report.ReviewedAt is null)
    {
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewedByUserId = _currentUser.UserId;
    }
    ChangeStatus(report, FaultStatusIds.Dodijeljeno);

    _history.Add(report.Id, HistoryChangeType.Assigned,
        oldValue: null, newValue: dto.TechnicianUserId.ToString(), note: dto.Note);

    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return await GetDetailAsync(assignment.Id, ct);
}
```

### 8.2 Re-dodjela

```csharp
public async Task<AssignmentDetailDto> ReassignAsync(int assignmentId, ReassignDto dto, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 5)  // BR-17
        throw AppException.Validation("Razlog re-dodjele je obavezan i mora imati barem 5 znakova.");

    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    var current = await _db.WorkAssignments
        .Include(a => a.FaultReport)
        .FirstOrDefaultAsync(a => a.Id == assignmentId && a.IsActive, ct)
        ?? throw AppException.NotFound("Aktivna dodjela nije pronađena.");

    if (current.FaultReport.FaultStatusId == FaultStatusIds.Zatvoreno)          // BR-12
        throw AppException.Conflict("Zatvorena prijava se ne može re-dodijeliti.");

    if (current.TechnicianUserId == dto.NewTechnicianUserId)                    // BR-18
        throw AppException.Validation("Nalog je već dodijeljen odabranom izvršitelju.");

    await EnsureIsActiveTechnicianAsync(dto.NewTechnicianUserId, ct);           // BR-16

    // BR-19: stara dodjela se deaktivira, nikad ne briše
    current.IsActive = false;
    current.UnassignedAt = DateTime.UtcNow;
    current.ReassignReason = dto.Reason.Trim();

    var replacement = new WorkAssignment
    {
        FaultReportId = current.FaultReportId,
        TechnicianUserId = dto.NewTechnicianUserId,
        AssignedByUserId = _currentUser.UserId,
        AssignedAt = DateTime.UtcNow,
        IsActive = true,
        ReassignReason = dto.Reason.Trim(),
        Note = dto.Note
    };
    _db.WorkAssignments.Add(replacement);

    _history.Add(current.FaultReportId, HistoryChangeType.Reassigned,
        oldValue: current.TechnicianUserId.ToString(),
        newValue: dto.NewTechnicianUserId.ToString(),
        note: dto.Reason);

    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return await GetDetailAsync(replacement.Id, ct);
}
```

> **Zamka s parcijalnim indeksom.** Deaktivacija stare i umetanje nove dodjele moraju se dogoditi u istom `SaveChangesAsync` pozivu. Ako bi se izveli u dva odvojena spremanja, kratkotrajno bi postojale dvije aktivne dodjele i indeks bi odbio upis. EF Core unutar jednog spremanja šalje `UPDATE` prije `INSERT`-a, što je ovdje upravo željeni redoslijed.

### 8.3 Deaktivacija izvršitelja s prijenosom naloga

Dva koraka, u skladu s pravilima BR-21, BR-22 i BR-23.

**Korak 1 — pokušaj deaktivacije vraća 409 s popisom:**

```csharp
public async Task DeactivateAsync(int userId, CancellationToken ct)
{
    var active = await _db.WorkAssignments
        .Where(a => a.TechnicianUserId == userId && a.IsActive)
        .Select(a => new ActiveAssignmentInfoDto
        {
            AssignmentId = a.Id,
            ReportNumber = a.FaultReport.ReportNumber,
            Title = a.FaultReport.Title,
            PriorityName = a.FaultReport.FaultPriority!.Name
        })
        .ToListAsync(ct);

    if (active.Count > 0)
        throw AppException.Conflict(
            $"Izvršitelj ima {active.Count} aktivnih naloga. Prebacite ih na zamjenika prije deaktivacije.",
            payload: active);

    var user = await _db.Users.FindAsync([userId], ct)
        ?? throw AppException.NotFound("Korisnik nije pronađen.");
    user.IsActive = false;
    user.DeactivatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync(ct);
}
```

**Korak 2 — masovni prijenos, pa deaktivacija:**

```csharp
public async Task<TransferResultDto> TransferAssignmentsAsync(
    int fromUserId, TransferAssignmentsDto dto, CancellationToken ct)
{
    if (fromUserId == dto.ReplacementTechnicianId)                              // BR-23
        throw AppException.Validation("Zamjenik ne može biti isti izvršitelj.");

    await EnsureIsActiveTechnicianAsync(dto.ReplacementTechnicianId, ct);       // BR-23

    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    var assignments = await _db.WorkAssignments
        .Where(a => a.TechnicianUserId == fromUserId && a.IsActive)
        .ToListAsync(ct);

    var now = DateTime.UtcNow;
    const string reason = "Deaktivacija izvršitelja";
    var interrupted = 0;

    foreach (var assignment in assignments)
    {
        // BR-22: intervencije u tijeku zatvaraju se kao neuspješne i ostaju u povijesti
        var running = await _db.Interventions
            .Where(i => i.WorkAssignmentId == assignment.Id
                        && i.InterventionStatusId == InterventionStatusIds.UTijeku)
            .ToListAsync(ct);

        foreach (var intervention in running)
        {
            intervention.InterventionStatusId = InterventionStatusIds.Neuspjesna;
            intervention.FinishedAt = now;
            intervention.FailureReason = "Prekinuto — deaktivacija izvršitelja";
            intervention.Note = string.IsNullOrWhiteSpace(intervention.Note)
                ? "Prekinuto — deaktivacija izvršitelja"
                : intervention.Note + "\nPrekinuto — deaktivacija izvršitelja";
            interrupted++;
        }

        assignment.IsActive = false;
        assignment.UnassignedAt = now;
        assignment.ReassignReason = reason;

        _db.WorkAssignments.Add(new WorkAssignment
        {
            FaultReportId = assignment.FaultReportId,
            TechnicianUserId = dto.ReplacementTechnicianId,
            AssignedByUserId = _currentUser.UserId,
            AssignedAt = now,
            IsActive = true,
            ReassignReason = reason,
            Note = dto.Note
        });

        _history.Add(assignment.FaultReportId, HistoryChangeType.Reassigned,
            assignment.TechnicianUserId.ToString(),
            dto.ReplacementTechnicianId.ToString(), reason);
    }

    var user = await _db.Users.FindAsync([fromUserId], ct)!;
    user!.IsActive = false;
    user.DeactivatedAt = now;

    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return new TransferResultDto
    {
        TransferredCount = assignments.Count,
        InterruptedInterventionsCount = interrupted
    };
}
```

### 8.4 Završetak intervencije

```csharp
public async Task<InterventionDetailDto> FinishAsync(
    int id, InterventionFinishDto dto, CancellationToken ct)
{
    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    var intervention = await _db.Interventions
        .Include(i => i.WorkAssignment)
        .Include(i => i.FaultReport)
        .FirstOrDefaultAsync(i => i.Id == id, ct)
        ?? throw AppException.NotFound("Intervencija nije pronađena.");

    // BR-33: samo vlasnik aktivne dodjele (ili Admin)
    if (!_currentUser.IsInRole(Roles.Admin)
        && (intervention.WorkAssignment.TechnicianUserId != _currentUser.UserId
            || !intervention.WorkAssignment.IsActive))
        throw AppException.Forbidden("Možete mijenjati samo vlastiti aktivni nalog.");

    // BR-32
    if (intervention.InterventionStatusId is InterventionStatusIds.Zavrsena
                                          or InterventionStatusIds.Neuspjesna)
        throw AppException.Conflict("Završena intervencija se više ne može mijenjati.");

    var finishedAt = dto.FinishedAt ?? DateTime.UtcNow;

    if (intervention.StartedAt is null)                                          // BR-28
        throw AppException.Validation("Intervencija nema zabilježeno vrijeme početka.");
    if (finishedAt < intervention.StartedAt || finishedAt > DateTime.UtcNow)     // BR-27
        throw AppException.Validation("Vrijeme završetka mora biti nakon početka i ne u budućnosti.");
    if (string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Trim().Length < 10)      // BR-28
        throw AppException.Validation("Bilješka je obavezna i mora imati barem 10 znakova.");
    if (!dto.IsSuccessful && string.IsNullOrWhiteSpace(dto.FailureReason))       // BR-29
        throw AppException.Validation("Razlog neuspjeha je obavezan.");

    intervention.FinishedAt = finishedAt;
    intervention.Note = dto.Note.Trim();
    intervention.InterventionStatusId = dto.IsSuccessful
        ? InterventionStatusIds.Zavrsena
        : InterventionStatusIds.Neuspjesna;
    intervention.FailureReason = dto.IsSuccessful ? null : dto.FailureReason!.Trim();

    if (dto.IsSuccessful)
    {
        // BR-30
        intervention.FaultReport.ResolvedAt = finishedAt;
        ChangeStatus(intervention.FaultReport, FaultStatusIds.Rijeseno);
    }
    // BR-31: kod neuspjeha prijava namjerno ostaje U radu, a dodjela aktivna

    _history.Add(intervention.FaultReportId, HistoryChangeType.InterventionFinished,
        oldValue: null,
        newValue: dto.IsSuccessful ? "Uspješna" : "Neuspješna",
        note: dto.Note);

    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return await GetDetailAsync(id, ct);
}
```

---

## 9. Rad s datotekama

### 9.1 Apstrakcija spremišta

```csharp
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName,
                               int faultReportId, CancellationToken ct);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
}

public sealed record StoredFile(string StoredFileName, string RelativePath, long SizeBytes);
```

Implementacija nad lokalnim diskom sprema u `{RootPath}/{godina}/{faultReportId}/{guid}{ekstenzija}`. Korijenski put dolazi iz konfiguracije i **nije unutar `wwwroot`** — datoteke nikad nisu dostupne kao statički sadržaj, nego isključivo kroz autorizirani endpoint (pravila FR-ATT-05 i BR-44).

### 9.2 Validacija uploada

Tri provjere, sve tri moraju proći (pravilo BR-41):

```csharp
private static readonly Dictionary<string, (string Mime, byte[][] Signatures)> Allowed = new()
{
    [".jpg"]  = ("image/jpeg",      [[0xFF, 0xD8, 0xFF]]),
    [".jpeg"] = ("image/jpeg",      [[0xFF, 0xD8, 0xFF]]),
    [".png"]  = ("image/png",       [[0x89, 0x50, 0x4E, 0x47]]),
    [".webp"] = ("image/webp",      [[0x52, 0x49, 0x46, 0x46]]),   // RIFF, uz "WEBP" na offsetu 8
    [".pdf"]  = ("application/pdf", [[0x25, 0x50, 0x44, 0x46]])    // %PDF
};

private static async Task ValidateAsync(IFormFile file, FileStorageOptions options)
{
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

    // 1. ekstenzija
    if (!Allowed.TryGetValue(extension, out var spec))
        throw AppException.UnsupportedMedia(
            "Dopušteni su samo jpg, jpeg, png, webp i pdf.");

    // 2. prijavljeni MIME tip
    if (!string.Equals(file.ContentType, spec.Mime, StringComparison.OrdinalIgnoreCase))
        throw AppException.UnsupportedMedia("Tip datoteke ne odgovara ekstenziji.");

    // 3. veličina, ovisno o vrsti
    var maxSize = extension == ".pdf"
        ? options.MaxDocumentSizeBytes
        : options.MaxImageSizeBytes;
    if (file.Length > maxSize)
        throw AppException.TooLarge(
            $"Datoteka premašuje dopuštenih {maxSize / 1024 / 1024} MB.");

    // 4. potpis datoteke — jedina provjera koju korisnik ne može lažirati preimenovanjem
    await using var stream = file.OpenReadStream();
    var header = new byte[8];
    var read = await stream.ReadAsync(header);

    if (!spec.Signatures.Any(sig =>
            read >= sig.Length && header.Take(sig.Length).SequenceEqual(sig)))
        throw AppException.UnsupportedMedia("Sadržaj datoteke ne odgovara navedenom tipu.");
}
```

Ograničenje broja privitaka po namjeni (pravilo BR-42) provjerava se prije spremanja brojanjem postojećih neizbrisanih privitaka iste namjene na istom nadređenom zapisu.

Uz to, u `Program.cs` treba podignuti granicu veličine tijela zahtjeva:

```csharp
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 10 * 1024 * 1024);
```

### 9.3 Autorizirano preuzimanje

```csharp
[HttpGet("{id:int}")]
[Authorize]
public async Task<IActionResult> Download(int id, CancellationToken ct)
{
    var (attachment, stream) = await _service.OpenForDownloadAsync(id, ct);
    return File(stream, attachment.ContentType, attachment.OriginalFileName);
}
```

Servis prije otvaranja toka provjerava smije li pozivatelj vidjeti nadređenu prijavu, koristeći isti filtar `VisibleTo` iz poglavlja 6.4. Time je nemoguće doći do tuđe fotografije pogađanjem identifikatora.

---

## 10. Obrada grešaka

### 10.1 Iznimka s pripadajućim statusom

```csharp
public sealed class AppException : Exception
{
    public int StatusCode { get; }
    public object? Payload { get; }
    public IDictionary<string, string[]>? Errors { get; }

    private AppException(int statusCode, string message,
                         object? payload = null,
                         IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Payload = payload;
        Errors = errors;
    }

    public static AppException Validation(string message, IDictionary<string, string[]>? errors = null)
        => new(400, message, errors: errors);
    public static AppException Unauthorized(string message) => new(401, message);
    public static AppException Forbidden(string message)    => new(403, message);
    public static AppException NotFound(string message)     => new(404, message);
    public static AppException Conflict(string message, object? payload = null)
        => new(409, message, payload);
    public static AppException TooLarge(string message)         => new(413, message);
    public static AppException UnsupportedMedia(string message) => new(415, message);
}
```

### 10.2 Međusloj

```csharp
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Message, ex.Errors, ex.Payload);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            logger.LogWarning(ex, "Prekršeno jedinstveno ograničenje");
            await WriteAsync(context, 409, "Zapis s tim vrijednostima već postoji.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Neočekivana greška");
            await WriteAsync(context, 500, "Došlo je do neočekivane greške.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: "23505" };

    private static Task WriteAsync(HttpContext context, int status, string message,
        IDictionary<string, string[]>? errors = null, object? payload = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new ApiErrorDto
        {
            Message = message,
            Errors = errors,
            Payload = payload
        });
    }
}
```

Format odgovora je ujednačen za cijeli API:

```json
{
  "message": "Prijava već ima aktivnu dodjelu. Koristite re-dodjelu.",
  "errors": null,
  "payload": null
}
```

Polje `payload` koristi se kod sukoba pri deaktivaciji izvršitelja, gdje uz poruku putuje i popis aktivnih naloga koji klijent prikazuje u dijalogu.

---

## 11. Blazor klijent

### 11.1 Postav

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore();

builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddHttpClient<ApiClient>(c =>
        c.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!))
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped<LookupCache>();

await builder.Build().RunAsync();
```

### 11.2 Presretač zahtjeva

```csharp
public sealed class AuthHeaderHandler(
    ILocalStorageService storage,
    NavigationManager navigation) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await storage.GetItemAsStringAsync("authToken", ct);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await storage.RemoveItemAsync("authToken", ct);
            navigation.NavigateTo("/login", forceLoad: true);
        }

        return response;
    }
}
```

### 11.3 Stanje autentikacije iz tokena

`JwtAuthStateProvider` raščlanjuje korisni teret tokena i gradi `ClaimsPrincipal`. Ključni detalj je da se **svaki** role claim iz tokena mora prenijeti, inače multi-role korisnik na klijentu gubi dio izbornika:

```csharp
private static IEnumerable<Claim> ParseClaims(string jwt)
{
    var payload = jwt.Split('.')[1];
    var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
        Convert.FromBase64String(PadBase64(payload)))!;

    foreach (var (key, value) in json)
    {
        if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray())
                yield return new Claim(key, item.ToString());     // više uloga
        else
            yield return new Claim(key, value.ToString());
    }
}
```

> **Zamka.** Kada korisnik ima samo jednu ulogu, JSON serijalizator tokena upiše je kao običan niz znakova, a kad ih ima više — kao polje. Gornja petlja pokriva oba slučaja. Uz to, `AuthorizationCore` traži da naziv role claima odgovara onome što je zadano u `ClaimsIdentity` konstruktoru, pa se identitet stvara kao `new ClaimsIdentity(claims, "jwt", "sub", ClaimTypes.Role)`.

Izbornik se onda gradi deklarativno:

```razor
<AuthorizeView Roles="Admin,Manager">
    <MudNavLink Href="/fault-reports" Icon="@Icons.Material.Filled.List">Sve prijave</MudNavLink>
    <MudNavLink Href="/assignments" Icon="@Icons.Material.Filled.Assignment">Radni nalozi</MudNavLink>
</AuthorizeView>

<AuthorizeView Roles="Technician">
    <MudNavLink Href="/my-assignments" Icon="@Icons.Material.Filled.Build">Moji nalozi</MudNavLink>
</AuthorizeView>
```

### 11.4 Tablica s poslužiteljskim podacima

`MudTable` s `ServerData` sam poziva metodu pri promjeni stranice, veličine stranice ili sortiranja — što je točno ono što treba za pravila BR-53 i BR-54:

```razor
<MudTable T="FaultReportListDto" @ref="_table" ServerData="LoadAsync" Hover="true" Dense="true">
    <HeaderContent>
        <MudTh><MudTableSortLabel T="FaultReportListDto" SortLabel="reportNumber">Broj</MudTableSortLabel></MudTh>
        <MudTh>Naslov</MudTh>
        <MudTh>Lokacija</MudTh>
        <MudTh><MudTableSortLabel T="FaultReportListDto" SortLabel="priority">Prioritet</MudTableSortLabel></MudTh>
        <MudTh>Status</MudTh>
        <MudTh><MudTableSortLabel T="FaultReportListDto" SortLabel="dueDate">Rok</MudTableSortLabel></MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Broj">@context.ReportNumber</MudTd>
        <MudTd DataLabel="Naslov">@context.Title</MudTd>
        <MudTd DataLabel="Lokacija">@context.LocationName</MudTd>
        <MudTd DataLabel="Prioritet"><PriorityBadge Name="@context.PriorityName" Color="@context.PriorityColor" /></MudTd>
        <MudTd DataLabel="Status"><StatusBadge Name="@context.StatusName" /></MudTd>
        <MudTd DataLabel="Rok">
            <span class="@(context.IsOverdue ? "text-danger fw-bold" : "")">
                @context.DueDate?.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            </span>
        </MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 25, 50 }" />
    </PagerContent>
</MudTable>

@code {
    private MudTable<FaultReportListDto>? _table;
    private FaultReportFilterDto _filter = new();

    private async Task<TableData<FaultReportListDto>> LoadAsync(
        TableState state, CancellationToken ct)
    {
        _filter.Page = state.Page + 1;              // MudTable broji stranice od nule
        _filter.PageSize = state.PageSize;
        _filter.SortBy = state.SortLabel;
        _filter.SortDir = state.SortDirection == SortDirection.Ascending ? "asc" : "desc";

        var result = await Api.GetFaultReportsAsync(_filter, ct);
        return new TableData<FaultReportListDto>
        {
            Items = result.Items,
            TotalItems = result.TotalCount
        };
    }

    private async Task ApplyFiltersAsync() => await _table!.ReloadServerData();

    private async Task ResetFiltersAsync()      // FR-FR-06
    {
        _filter = new FaultReportFilterDto();
        await _table!.ReloadServerData();
    }
}
```

Atribut `DataLabel` na svakoj ćeliji nije ukras — MudBlazor ga koristi kad se tablica na uskim zaslonima prelomi u kartice, čime je mobilna prilagodba iz bonus dijela riješena bez dodatnog koda.

### 11.5 Predlaganje roka iz prioriteta

```razor
<MudSelect T="int?" @bind-Value="_model.FaultPriorityId" Label="Prioritet"
           @bind-Value:after="SuggestDueDate">
    @foreach (var p in Lookups.Priorities)
    {
        <MudSelectItem T="int?" Value="@p.Id">@p.Name</MudSelectItem>
    }
</MudSelect>

<MudDatePicker @bind-Date="_dueDate" Label="Rok" />

@code {
    private void SuggestDueDate()
    {
        var priority = Lookups.Priorities.FirstOrDefault(p => p.Id == _model.FaultPriorityId);
        if (priority is not null)
            _dueDate = DateTime.Now.AddHours(priority.DefaultResolutionHours);  // FR-FR-04
    }
}
```

Prijedlog je samo predpopunjena vrijednost — Manager ga slobodno mijenja, a poslužitelj i dalje provjerava pravila BR-04 i BR-05.

### 11.6 Predložak stranice s AI prijedlogom

Ploča s prijedlogom prikazuje se odvojeno od forme i ništa ne mijenja dok korisnik ne klikne (pravila BR-57 i BR-58):

```razor
@if (_suggestion is not null)
{
    <MudAlert Severity="Severity.Info" Class="mb-4">
        <b>Prijedlog pomoćnika:</b> @_suggestion.SuggestedTitle —
        @_suggestion.SuggestedFaultTypeName, prioritet @_suggestion.SuggestedPriorityName
        <div class="mud-typography-caption">@_suggestion.Explanation</div>
        <MudButton Size="Size.Small" OnClick="ApplySuggestion">Primijeni prijedlog</MudButton>
        <MudButton Size="Size.Small" OnClick="() => _suggestion = null">Odbaci</MudButton>
    </MudAlert>
}
```

---

## 12. Isporuka

### 12.1 docker-compose.yml

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: ekvarovi
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports: [ "5432:5432" ]
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: [ "CMD-SHELL", "pg_isready -U postgres" ]
      interval: 5s
      retries: 10

  api:
    build:
      context: .
      dockerfile: EKvarovi.Api/Dockerfile
    environment:
      ConnectionStrings__Default: "Host=db;Port=5432;Database=ekvarovi;Username=postgres;Password=postgres"
      Jwt__Key: "${JWT_KEY}"
      FileStorage__RootPath: "/data/uploads"
      Cors__AllowedOrigins__0: "http://localhost:8080"
    ports: [ "8081:8080" ]
    volumes:
      - uploads:/data/uploads          # bez ovoga fotografije nestaju pri ponovnoj izgradnji
    depends_on:
      db:
        condition: service_healthy

  app:
    build:
      context: .
      dockerfile: EKvarovi.App/Dockerfile
    ports: [ "8080:80" ]
    depends_on: [ api ]

volumes:
  pgdata:
  uploads:
```

Blazor WebAssembly je nakon objave statički sadržaj, pa se njegov Dockerfile svodi na izgradnju u SDK sloju i posluživanje kroz nginx. Adresa API-ja se za WebAssembly ne može uzeti iz varijable okoline kontejnera jer se izvodi u pregledniku — čita se iz `wwwroot/appsettings.json`, koji se po potrebi zamijeni pri objavi.

### 12.2 Render

| Resurs | Postavke |
|---|---|
| **PostgreSQL** | Besplatni plan; nakon stvaranja preuzeti internu vezu i pretvoriti je u Npgsql oblik |
| **Web Service (API)** | Docker okruženje, `EKvarovi.Api/Dockerfile`, health check na `/health` |
| **Disk** | Priključen na API servis, montiran na `/data/uploads`, 1 GB |
| **Static Site (klijent)** | Build naredba `dotnet publish EKvarovi.App -c Release -o out`, direktorij za objavu `out/wwwroot`, uz preusmjeravanje svih ruta na `/index.html` |

Varijable okoline na API servisu:

```
ConnectionStrings__Default = Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
Jwt__Key                   = <generirani tajni ključ>
FileStorage__RootPath      = /data/uploads
Cors__AllowedOrigins__0    = https://<naziv-statickog-sitea>.onrender.com
Seed__DemoData             = true
```

Dvije stvari na koje treba paziti: bez priključenog diska uploadi nestaju pri svakoj objavi, a besplatni plan uspava servis nakon razdoblja neaktivnosti, pa prvi zahtjev nakon stanke traje i do pola minute. Za obranu projekta vrijedi API „probuditi” nekoliko minuta ranije.

---

## 13. Konvencije

| Područje | Pravilo |
|---|---|
| Jezik koda | Engleski — nazivi entiteta, svojstava, servisa, ruta i grana |
| Jezik sučelja | Hrvatski — natpisi, poruke o greškama, sadržaj seed podataka |
| Rute API-ja | Množina, mala slova: `/api/faultreports`, `/api/workassignments` |
| Radnje izvan CRUD-a | Podresurs s glagolom: `PUT /faultreports/{id}/close`, `PUT /workassignments/{id}/reassign` |
| Imenovanje DTO-a | `…ListDto` za popis, `…DetailDto` za detalj, `…SaveDto` za unos i izmjenu, `…FilterDto` za upitne parametre |
| Vrijeme | U bazi i API-ju UTC (`timestamptz`, ISO 8601); u UI-u lokalno, format `dd.MM.yyyy HH:mm` |
| Novac | `decimal(10,2)`, valuta euro, prikaz s dva decimalna mjesta i oznakom € |
| Asinkronost | Svaka metoda koja dira bazu ili mrežu je `async` i prima `CancellationToken` |
| Upiti za čitanje | Uvijek `AsNoTracking()` i projekcija u DTO |
| Nazivi u bazi | `snake_case`, automatski kroz konvenciju imenovanja |
| Migracije | Jedna po zaokruženoj promjeni modela, s opisnim nazivom (`AddWorkAssignmentHistory`) |
| Grananje | Rad na `main`, uz commit poruke oblika `feat:`, `fix:`, `docs:` |

---

## 14. Kontrolna lista rizika

Popis mjesta na kojima se najčešće gubi vrijeme, s rješenjem pri ruci.

| # | Simptom | Uzrok i rješenje |
|---|---|---|
| 1 | `[Authorize(Roles = "Manager")]` vraća 403 iako je uloga u tokenu | Neusklađen `RoleClaimType` ili automatsko preslikavanje claimova. Postaviti `RoleClaimType = ClaimTypes.Role` i `DefaultMapInboundClaims = false`. Poglavlje 6.1 |
| 2 | Migracija ne prolazi zbog izraza u filtru indeksa | Nazivi stupaca u `HasFilter` moraju odgovarati stvarnima u bazi. Uz `snake_case` konvenciju to je `is_active AND NOT is_deleted`, bez navodnika. Poglavlje 5.4 |
| 3 | Re-dodjela pada na jedinstvenom indeksu | Deaktivacija stare i umetanje nove dodjele moraju biti u istom `SaveChangesAsync`. Poglavlje 8.2 |
| 4 | Klijent s dvije uloge vidi izbornik samo jedne | Raščlamba tokena ne obrađuje polje uloga. Poglavlje 11.3 |
| 5 | `PostgresException: 42P01` pri pokretanju | Migracije nisu primijenjene ili baza nije dostupna. Provjeriti `depends_on` i health check u Composeu |
| 6 | Vremena su pomaknuta za sat ili dva | Miješanje lokalnog i UTC vremena. U bazu ide isključivo UTC, pretvorba u lokalno radi se tek pri prikazu |
| 7 | Upload prolazi u Swaggeru, ne prolazi iz Blazora | Nedostaje `MultipartBodyLengthLimit` ili CORS ne dopušta potrebna zaglavlja. Poglavlje 9.2 |
| 8 | Fotografije nestanu nakon ponovne objave | Nije priključen volumen odnosno disk na putanju uploada. Poglavlje 12 |
| 9 | Popis se učitava sporo | Izostala projekcija prije straničenja ili nedostaje indeks na stupcu po kojem se filtrira |
| 10 | Logički obrisani zapisi i dalje se pojavljuju | Negdje je pozvan `IgnoreQueryFilters()` ili entitet ne nasljeđuje baznu klasu |
