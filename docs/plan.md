# Plan implementacije — e-Kvarovi Županije

**Pravilo:** jedan korak = jedan prompt agentu = jedan commit.

Prije svakog koraka agent mora pročitati `docs/prd.md`, `docs/tech.md` i `docs/shema.md`.

---

## Pravila rada s agentom

1. Svaki prompt počni s: *„Pročitaj `docs/prd.md`, `docs/tech.md` i `docs/shema.md`, zatim napravi SAMO korak X.Y."*
2. Nikad ne dopusti dva koraka odjednom.
3. Nakon svakog koraka pokreni `dotnet build`. Ako ne prolazi, popravi prije sljedećeg koraka.

---

## BLOK A — Kostur rješenja

### A1 — Solution i tri projekta
Napravi prazno rješenje s tri projekta i postavi reference.
- `dotnet new sln -n EKvarovi`
- `EKvarovi.Api` (webapi), `EKvarovi.App` (blazorwasm), `EKvarovi.Shared` (classlib), svi na .NET 9
- Api i App referenciraju Shared
- **Gotovo kad:** `dotnet build` prolazi bez greške

### A2 — Git i .gitignore
Inicijaliziraj repozitorij prije nego išta napišeš.
- `git init`, standardni .NET `.gitignore`
- Dodaj `App_Data/` i `appsettings.*.local.json` u ignore
- Prvi commit s tri prazna projekta i `docs/` mapom

### A3 — NuGet paketi
Instaliraj sve pakete odjednom da poslije ne prekidaš rad.
- Api: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `EFCore.NamingConventions`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`, `Swashbuckle.AspNetCore`
- App: `MudBlazor`, `Blazored.LocalStorage`
- **Gotovo kad:** build prolazi, nema upozorenja o nekompatibilnim verzijama

### A4 — Docker Compose samo s bazom
Za sada ti treba samo PostgreSQL, aplikacije ostaju izvan Dockera.
- Servis `db` (postgres:16-alpine), volumen `pgdata`, port 5432, health check
- **Gotovo kad:** `docker compose up -d db` radi i možeš se spojiti alatom po izboru

### A5 — Konfiguracija
Prepiši `appsettings.json` iz poglavlja 4.1 dokumenta `tech.md`.
- Connection string, sekcija `Jwt`, `FileStorage`, `Cors`, `Seed`
- Tajni ključ u `dotnet user-secrets`, u datoteci ostaje samo mjestodržač
- Options klase (`JwtOptions`, `FileStorageOptions`) i njihova registracija

---

## BLOK B — Model podataka

> Ovo je blok u kojem se najviše isplati ići sitno. Ako agent odjednom generira svih 17 entiteta, veliki je izgled da negdje promaši nullability ili smjer veze.

### B1 — DBML u dbdiagram.io
Prije koda vizualno provjeri model.
- Zalijepi DBML iz `shema.md` u dbdiagram.io
- Izvezi PNG u `docs/dijagram.png`
- Pogledom potvrdi da su sve veze u ispravnom smjeru

### B2 — Konstante i enumi u Shared
Fiksni identifikatori šifrarnika na jednom mjestu.
- `FaultStatusIds`, `InterventionStatusIds`, `FaultPriorityIds`, `MaterialUnitIds`, `Roles` (kao `const string`)
- Enumi `AttachmentPurpose` i `HistoryChangeType` s vrijednostima iz `shema.md`

### B3 — Entiteti šifrarnika
Sedam malih klasa, bez navigacijskih svojstava prema jezgri.
- `LocationType`, `FaultType`, `FaultPriority`, `FaultStatus`, `InterventionStatus`, `MaterialUnit`, `Role`
- Posebna polja: `DefaultResolutionHours` i `ColorHex`, `IsClosedState`, `Abbreviation`, `Code`

### B4 — Bazna klasa i korisnici
- `AuditableEntity` s poljima praćenja i logičkog brisanja
- `User`, `UserRole`, `Location` s navigacijskim svojstvima
- **Pazi:** `HomeLocationId` je nullable

### B5 — Prijave, dodjele, intervencije
Tri najvažnija entiteta jezgre.
- `FaultReport` — `FaultTypeId`, `FaultPriorityId`, `DueDate` **moraju biti nullable**
- `WorkAssignment` — `IsActive`, `UnassignedAt`, `ReassignReason`
- `Intervention` — i `WorkAssignmentId` i `FaultReportId`, `StartedAt` i `FinishedAt` nullable

### B6 — Materijali, privici, povijest
- `Material`, `InterventionMaterial`, `Attachment`, `FaultReportHistory`
- `Attachment` ima **obje** veze nullable
- Kolekcijska svojstva inicijaliziraj na prazne liste

### B7 — DbContext
- Svi `DbSet`-ovi, `OnModelCreating` s `ApplyConfigurationsFromAssembly`
- `UseSnakeCaseNamingConvention()` u registraciji
- **Gotovo kad:** build prolazi

### B8 — Konfiguracije šifrarnika i jednostavnih entiteta
Jedna `IEntityTypeConfiguration` klasa po entitetu.
- Duljine tekstualnih stupaca točno kao u `shema.md`
- Jedinstveni indeksi na `email`, `code`, `report_number`
- Složeni ključ za `UserRole`, kaskadno brisanje samo ovdje

### B9 — Konfiguracija dodjela s parcijalnim indeksom
Najvažniji korak u cijelom bloku, zato je odvojen.
- `HasIndex(a => a.FaultReportId).HasFilter("is_active AND NOT is_deleted").IsUnique()`
- Naziv indeksa `ux_work_assignments_active_per_report`
- `CHECK` uvjet `ck_work_assignments_unassigned`

### B10 — Uvjeti nad privicima i materijalima
- `ck_attachments_purpose_target` i `ck_attachments_purpose_range`
- `ck_intervention_materials_quantity`, jedinstveni indeks nad parom intervencija-materijal
- `ck_interventions_times`, `ck_materials_unit_price`

### B11 — Ponašanje pri brisanju i indeksi za filtriranje
- Svi strani ključevi na `Restrict`
- Svi indeksi iz poglavlja 3.2 dokumenta `shema.md`

### B12 — Globalni filtri logičkog brisanja
Refleksijom, za svaki entitet koji nasljeđuje baznu klasu.
- Kod je u poglavlju 5.2 dokumenta `tech.md`

### B13 — Seed šifrarnika
Sedam `HasData` poziva s fiksnim identifikatorima.
- Vrijednosti doslovno iz poglavlja 6.1 dokumenta `shema.md`

### B14 — Prva migracija
- `dotnet ef migrations add InitialCreate`
- **Prije primjene otvori generiranu migraciju i pročitaj je** — provjeri parcijalni indeks i uvjete
- `dotnet ef database update`, zatim u bazi potvrdi da postoji 17 tablica

---

## BLOK C — Autentikacija

> **Označen rizik** — JWT i multi-role claimovi.

### C1 — ICurrentUserService
Jedini dopušteni izvor identiteta u cijeloj aplikaciji.
- Sučelje i implementacija iz poglavlja 6.3 dokumenta `tech.md`
- Registracija uz `AddHttpContextAccessor()`

### C2 — SaveChanges override
Automatska polja praćenja i pretvaranje brisanja u logičko.
- Kod iz poglavlja 5.3 dokumenta `tech.md`
- `DbContext` sada prima `ICurrentUserService` kroz konstruktor

### C3 — AppException i ApiErrorDto
- `AppException` sa statičkim tvornicama za svaki HTTP status
- `ApiErrorDto` u Shared projektu (`Message`, `Errors`, `Payload`)

### C4 — Međusloj za greške
- `ExceptionHandlingMiddleware` iz poglavlja 10.2
- Registracija **prva u nizu**, prije svega ostalog
- **Gotovo kad:** namjerno bačena iznimka vraća JSON u očekivanom obliku

### C5 — Generiranje tokena
- `JwtTokenService` iz poglavlja 6.2 — jedan role claim po ulozi
- `sub`, `email`, `fullName`, opcionalno `homeLocationId`

### C6 — Validacija tokena
Korak u kojem se najčešće gubi sat vremena, zato ga radi sam za sebe.
- `AddJwtBearer` s parametrima iz poglavlja 6.1
- **Obavezno:** `RoleClaimType = ClaimTypes.Role` i `JwtSecurityTokenHandler.DefaultMapInboundClaims = false`
- `UseAuthentication()` prije `UseAuthorization()`

### C7 — AuthService i kontroler
- `LoginDto`, `AuthResponseDto`, `CurrentUserDto` u Shared
- Provjera lozinke BCryptom, odbijanje neaktivnog korisnika (BR-52)
- `POST /auth/login`, `GET /auth/me`

### C8 — Swagger s Bearer tokenom
- Definicija sigurnosne sheme iz poglavlja 6.5
- **Gotovo kad:** gumb Authorize postoji i `/auth/me` radi s tokenom

### C9 — Seed korisnika, lokacija i materijala
Sada kad postoji BCrypt, možeš seedati račune.
- 6 lokacija, 9 korisnika, 10 materijala iz poglavlja 6.2 do 6.4 dokumenta `shema.md`
- **Gotovo kad:** prijava radi za svaku od četiri uloge i za multi-role račun

### C10 — Kontrolna točka
- U Swaggeru provjeri: poziv bez tokena vraća 401, s tokenom Reportera na admin rutu vraća 403
- Dekodiraj token multi-role korisnika na jwt.io i potvrdi dva role claima

---

## BLOK D — Infrastruktura lista

### D1 — Ugovori za straničenje
- `PagedRequest` s ograničenjem veličine stranice (BR-53), `PagedResult<T>`, `LookupDto` — sve u Shared

### D2 — Metode proširenja
- `ApplySort` i `ToPagedResultAsync` iz poglavlja 7.2 dokumenta `tech.md`

### D3 — Servis povijesti
Mali servis koji će koristiti gotovo svi ostali, zato ide rano.
- `IHistoryService.Add(faultReportId, changeType, oldValue, newValue, note)`
- Samo dodaje u `ChangeTracker`, ne poziva spremanje — pozivatelj kontrolira transakciju

---

## BLOK E — Šifrarnici i jednostavni CRUD

> Ovdje nastaje obrazac koji se poslije ponavlja. Prvi CRUD radi pažljivo, ostali su prepisivanje.

### E1 — Lookup servis i kontroler
- Svih 8 GET endpointa plus zbirni `/lookups/all`
- `PriorityLookupDto` nosi zadani broj sati i boju

### E2 — DTO-ovi lokacija
- `LocationListDto`, `LocationDetailDto`, `LocationSaveDto`, `LocationFilterDto`

### E3 — Servis lokacija
- Search s filtrima, sortiranjem i straničenjem — **ovo je predložak za sve ostale liste**
- Create, Update, logičko brisanje uz provjeru postojećih prijava (FR-LOC-04)

### E4 — Kontroler lokacija
- Pet ruta, atributi uloga, bez ikakve logike u kontroleru

### E5 — Materijali
- Isti obrazac: DTO-ovi, servis, kontroler
- Brisanje blokirano ako je materijal u upotrebi (BR-39)

### E6 — Korisnici, osnovni dio
- DTO-ovi, popis s filtrima, detalj, kreiranje, izmjena, reset lozinke, aktivacija
- **Deaktivaciju preskoči** — dolazi u koraku G7

---

## BLOK F — Prijave

### F1 — DTO-ovi prijava
Sedam klasa, sve u Shared.
- `FaultReportListDto`, `FaultReportDetailDto`, `FaultReportCreateDto`, `FaultReportUpdateDto`, `FaultReportTriageDto`, `CloseFaultReportDto`, `ReopenFaultReportDto`, `FaultReportFilterDto`

### F2 — Filtar vidljivosti
- Metoda proširenja `VisibleTo` iz poglavlja 6.4 dokumenta `tech.md`
- **Pazi:** uvjet nad dodjelama namjerno ne gleda `IsActive` (BR-48)

### F3 — Generiranje broja prijave
- Privatna metoda po uzoru na poglavlje 5.6
- Format `KV-{godina}-{6 znamenki}`

### F4 — Kreiranje prijave
- Pravila BR-01, BR-02, status uvijek 1, upis u povijest kao `Created`
- Vrsta, prioritet i rok ostaju prazni

### F5 — Dohvat detalja
- Uz filtar vidljivosti, sa svim uključenim vezama
- Za sada bez privitaka i intervencija (kolekcije ostaju prazne)

### F6 — Pretraga s filtrima
Najveći pojedinačni upit u projektu.
- Cijeli kod je u poglavlju 7.3 dokumenta `tech.md`
- Svih devet filtara, mapa sortiranja, izračun kašnjenja

### F7 — Endpoint /mine
- Ista pretraga, uz dodatni uvjet nad podnositeljem
- **Bez ikakvog parametra s identifikatorom korisnika** (BR-50)

### F8 — Kategorizacija
- `PUT /faultreports/{id}/triage` — pravila BR-04, BR-05, BR-08
- Tri odvojena zapisa u povijest ako se promijene tip, prioritet i rok

### F9 — Izmjena i brisanje
- Reporter samo vlastito i samo u statusu Zaprimljeno (BR-06)
- Brisanje blokirano ako postoji intervencija (BR-07)

### F10 — Kontroler prijava
- Sve rute iz poglavlja 6.4 dokumenta `prd.md` osim `close`, `reopen` i `timeline`

---

## BLOK G — Radni nalozi

### G1 — DTO-ovi dodjela
- `AssignmentListDto`, `AssignmentDetailDto`, `AssignmentSaveDto`, `ReassignDto`, `AssignmentFilterDto`, `ActiveAssignmentInfoDto`, `TransferAssignmentsDto`, `TransferResultDto`

### G2 — Provjera izvršitelja
- Privatna metoda `EnsureIsActiveTechnicianAsync` — aktivan korisnik s ulogom Technician (BR-16)

### G3 — Dodjela
- Cijeli kod u poglavlju 8.1 dokumenta `tech.md`
- Transakcija, pravila BR-12, BR-15, BR-20, automatski prijelaz do statusa Dodijeljeno

### G4 — Re-dodjela
- Kod u poglavlju 8.2
- **Ključno:** deaktivacija stare i umetanje nove u **istom** `SaveChangesAsync`
- **Gotovo kad:** u bazi vidiš dva retka, samo jedan s `is_active = true`

### G5 — Pretraga naloga i /mine
- Filtri po izvršitelju, aktivnosti, lokaciji i razdoblju
- `/workassignments/mine` s prekidačem samo aktivni

### G6 — Kontroler naloga
- Pet ruta, uključujući povijest dodjela po prijavi

### G7 — Deaktivacija izvršitelja s 409
- Kod u poglavlju 8.3, prvi dio
- Vraća popis aktivnih naloga u polju `payload`

### G8 — Prijenos naloga na zamjenika
- Drugi dio poglavlja 8.3
- Prekid intervencija u tijeku kao neuspješnih (BR-22), zatim deaktivacija korisnika
- **Gotovo kad:** kroz Swagger odradiš cijeli scenarij i potvrdiš da su svi nalozi prešli

---

## BLOK H — Intervencije

### H1 — DTO-ovi intervencija
- `InterventionListDto`, `InterventionDetailDto`, `InterventionCreateDto`, `InterventionUpdateDto`, `InterventionFinishDto`, `InterventionFilterDto`

### H2 — Pokretanje intervencije
- Pravila BR-24, BR-25, BR-26 — provjera vlasništva nad aktivnom dodjelom
- Prijava prelazi u status U radu, upis u povijest

### H3 — Izmjena bilješke
- Samo dok intervencija nije završena (BR-32), samo vlasnik (BR-33)

### H4 — Završetak intervencije
- Cijeli kod u poglavlju 8.4 dokumenta `tech.md`
- Pravila BR-27 do BR-31 — obrati pažnju da kod neuspjeha prijava **ostaje** U radu

### H5 — Pretraga intervencija i /mine
- Filtri po tekstu, statusu, izvršitelju i razdoblju, zadano sortiranje po početku silazno

### H6 — Kontroler intervencija

### H7 — Zatvaranje i vraćanje prijave u rad
Sada kad intervencije postoje, dovrši tijek prijave.
- Zatvaranje traži barem jednu uspješnu intervenciju (BR-13) i napomenu (BR-14)
- Vraćanje u rad samo iz Riješeno, uz obrazloženje (BR-11)

### H8 — Kontrolna točka
Ovo je najvažnija provjera u cijelom projektu.
- Kroz Swagger odradi: prijava → kategorizacija → dodjela → re-dodjela → intervencija → neuspjeh → nova intervencija → uspjeh → zatvaranje
- Zatim namjerno prekrši pet pravila i potvrdi statuse 400, 403, 409

---

## BLOK I — Materijali na intervenciji

### I1 — DTO-ovi stavki
- `InterventionMaterialDto`, `InterventionMaterialSaveDto`

### I2 — Dodavanje stavke
- Pravila BR-34 do BR-38, snimka jedinične cijene

### I3 — Izmjena i brisanje stavke
- Samo na intervenciji koja nije završena

### I4 — Ukupni trošak
- Zbroj u `InterventionDetailDto` i `FaultReportDetailDto`

---

## BLOK J — Privici

### J1 — Apstrakcija spremišta
- `IFileStorage` i `LocalDiskFileStorage`, putanja `{root}/{godina}/{faultReportId}/{guid}.ext`
- Korijenski direktorij **izvan** `wwwroot`

### J2 — Validator datoteka
- Tri provjere iz poglavlja 9.2 dokumenta `tech.md`, uključujući potpis datoteke
- `MultipartBodyLengthLimit` u `Program.cs`

### J3 — Servis privitaka
- Upload uz provjeru broja po namjeni (BR-42) i namjene naspram veze (BR-43)
- Logičko brisanje (BR-45)

### J4 — Endpointi privitaka
- Dva uploada, preuzimanje uz provjeru vidljivosti (BR-44), brisanje
- **Gotovo kad:** upload PNG-a preimenovanog u `.pdf` vraća 415

### J5 — Uključivanje privitaka u DTO-ove
- `FaultReportDetailDto` grupira po namjeni, `InterventionDetailDto` nosi fotografije poslije

---

## BLOK K — Dashboard

### K1 — DashboardDto
- Pet metrika, razdoblje, oznaka opsega

### K2 — Servis dashboarda
- Svaka metrika kao zaseban `GroupBy` upit, opseg po ulozi (FR-DSH-08)
- Filtar razdoblja, zadano 30 dana

### K3 — Kontroler dashboarda

---

## BLOK L — Blazor kostur

### L1 — MudBlazor postav
- `AddMudServices`, `MudThemeProvider`, hrvatska lokalizacija datuma
- **Gotovo kad:** početna stranica prikazuje MudBlazor komponentu

### L2 — Pohrana tokena i presretač
- `Blazored.LocalStorage`, `AuthHeaderHandler` iz poglavlja 11.2
- Registracija `HttpClient` s osnovnom adresom API-ja

### L3 — Stanje autentikacije
- `JwtAuthStateProvider` iz poglavlja 11.3
- **Pazi:** raščlamba mora podnijeti i jedan i više role claimova

### L4 — ApiClient
- Tipizirane metode, obrada `ApiErrorDto` u čitljivu poruku
- Za sada samo metode prijave i šifrarnika

### L5 — Predložak i izbornik
- `MainLayout`, `NavMenu` s `AuthorizeView` po ulogama, gumb za odjavu

### L6 — Stranica prijave
- Forma, spremanje tokena, preusmjeravanje
- **Gotovo kad:** prijaviš se kao Manager i vidiš njegov izbornik

### L7 — Predmemorija šifrarnika
- Jedan poziv `/lookups/all` po sesiji, servis dostupan svim stranicama

---

## BLOK M — Blazor ekrani

> Redoslijed nije proizvoljan — prvo idu najkraći ekrani koji dokazuju da `/mine` scenariji rade.

### M1 — Zajedničke komponente
- `PriorityBadge`, `StatusBadge`, `ConfirmDialog`, `FilterBar` s gumbom za reset

### M2 — MyReports
- Najjednostavniji popis, bez filtara — služi kao provjera da lanac klijent-API radi

### M3 — MyAssignments
- Kartice naloga, prekidač aktivni/svi, gumb za pokretanje intervencije

### M4 — FaultReports
- `MudTable` sa `ServerData` po uzoru na poglavlje 11.4
- Svi filtri, reset, atributi `DataLabel` na svakoj ćeliji

### M5 — Kreiranje prijave
- Predodabrana matična lokacija, validacija, responzivna forma

### M6 — Uređivanje prijave
- Manager dodatno vidi vrstu, prioritet i rok s predlaganjem roka (poglavlje 11.5)

### M7 — Profil prijave, osnovni dio
- Zaglavlje, kartice s podacima, popis dodjela i intervencija

### M8 — Profil prijave, radnje
- Kontekstualni gumbi ovisno o ulozi i statusu: kategoriziraj, dodijeli, re-dodijeli, zatvori, vrati u rad

### M9 — Popis naloga i dijalozi
- Dijalog dodjele i dijalog re-dodjele s obaveznim razlogom

### M10 — Popis intervencija

### M11 — Detalj intervencije
- Bilješka, dijalog završetka s oba ishoda

### M12 — Materijali na intervenciji
- Dodavanje stavke, prikaz ukupnog troška

### M13 — Privici
- Komponenta galerije, upload na prijavi i na intervenciji, brisanje

### M14 — Katalog materijala

### M15 — Lokacije

### M16 — Korisnici
- Dodjela više uloga, reset lozinke
- Dijalog deaktivacije koji iz odgovora 409 gradi popis i traži zamjenika

### M17 — Početna stranica
- Dashboard po ulozi, filtar razdoblja, gumb za osvježavanje

---

## BLOK N — Isporuka

> **Prvo što režeš ako kasniš.**

### N1 — Dockerfile za API
- Višefazna izgradnja, izloženi port, health endpoint

### N2 — Dockerfile za klijent
- Izgradnja u SDK sloju, posluživanje kroz nginx, preusmjeravanje ruta na `index.html`

### N3 — Kompletiranje Composea
- Servisi `api` i `app`, volumen za uploade, varijable okoline
- **Gotovo kad:** `docker compose up` diže sve troje i aplikacija radi

### N4 — Render
- Baza, web servis, disk montiran na putanju uploada, statički site
- Varijable okoline i CORS prema adresi klijenta

### N5 — README
- Upute za pokretanje, popis demo računa, kratak opis arhitekture i poveznice na dokumentaciju

---

## BLOK O — Bonus

### O1 — Provjera zapisa u povijest
- Prođi kroz sve servise i potvrdi da svaka promjena upisuje zapis
- Najčešće nedostaju promjena roka i vraćanje u rad

### O2 — Endpoint vremenske crte
- Kronološki sortirano, s imenom korisnika i čitljivim opisom promjene

### O3 — Komponenta vremenske crte
- `MudTimeline` na profilu prijave, ikona i boja po vrsti promjene

### O4 — Mobilna prilagodba
- Točke preloma na formi prijave i ekranu izvršitelja
- Atribut `capture` na polju za fotografiju

### O5 — Sučelje AI servisa
- `IAiService`, `AiSuggestionDto`, `AiSummaryDto` u Shared

### O6 — MockAiService
- Tablica ključnih riječi iz poglavlja 10.2 dokumenta `prd.md`
- Sažetak naloga iz stvarnih podataka

### O7 — AI endpointi i ploča u UI-u
- Dvije rute, ploča s prijedlogom i gumbom Primijeni
- **Pravilo bez iznimke:** ništa se ne sprema automatski (BR-58)

---

## Raspored po danima

| Dan | Blokovi | Što na kraju dana mora raditi |
|---|---|---|
| 1 | A, B | Baza postoji sa 17 tablica i seedanim šifrarnicima |
| 2 | C, D, E | Prijava radi za sve uloge, CRUD lokacija i materijala prolazi |
| 3 | F | Prijave se kreiraju, kategoriziraju i pretražuju kroz Swagger |
| 4 | G, H | **Cijeli poslovni tijek prolazi kroz Swagger** |
| 5 | I, J, K | API je gotov u cijelosti |
| 6 | L, M | UI pokriva sve obavezne ekrane |
| 7 | N, O | Docker, Render, bonusi |

Ako kraj dana 4 ne prođe, izbaci blokove N i O bez razmišljanja i cijeli preostali čas uloži u blok M.

---

## Predložak prompta za agenta

```
Pročitaj docs/prd.md, docs/tech.md, docs/shema.md i docs/plan.md.
Napravi SAMO korak [X.Y] iz plana.
Ne diraj ništa izvan opsega tog koraka.
Na kraju pokreni dotnet build i napiši što si napravio.
```
