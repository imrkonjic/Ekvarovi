# e-Kvarovi Županije

Web aplikacija za prijavu, dodjelu i obradu kvarova u objektima u vlasništvu županije. Korisnici prijavljuju kvarove, upravitelji ih kategoriziraju i dodjeljuju izvršiteljima, a cijeli tijek — intervencije, materijali, privici i zatvaranje — ostaje sljediv u bazi.

## Arhitektura

```
EKvarovi.sln
├── EKvarovi.Api      ASP.NET Core Web API (.NET 9), PostgreSQL, JWT
├── EKvarovi.App      Blazor WebAssembly + MudBlazor
└── EKvarovi.Shared   DTO-ovi, enumi, konstante
```

| Sloj | Tehnologija |
|------|-------------|
| Backend | ASP.NET Core 9, EF Core, Npgsql |
| Frontend | Blazor WebAssembly (standalone) |
| Baza | PostgreSQL 16 |
| Autentikacija | JWT Bearer, BCrypt, uloge po claimovima |
| Datoteke | Lokalni disk (`IFileStorage`) |
| Lokalna isporuka | Docker Compose |
| Oblak | [Render](https://render.com) (`render.yaml`) |

## Preduvjeti

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (za bazu ili cijeli stack)
- Git

---

## Pokretanje lokalno

Postoje **dvije opcije**. Obje koriste istu PostgreSQL bazu s demo podacima (seed se izvršava pri prvom pokretanju API-ja).

### Opcija A — cijeli stack u Dockeru (preporučeno)

Podiže bazu, API i Blazor klijent odjednom. Ne treba `dotnet run` niti user-secrets.

```powershell
# 1. Postavi JWT ključ (min. 32 znaka)
copy .env.example .env
# Uredi .env i postavi JWT_KEY

# 2. Pokreni sve servise
docker compose up --build -d
```

| Servis | Adresa |
|--------|--------|
| Klijent (Blazor) | http://localhost:8080 |
| API | http://localhost:8081 |
| Swagger | http://localhost:8081/swagger |
| Health check | http://localhost:8081/health |
| PostgreSQL | localhost:5432 |

Zaustavljanje: `docker compose down`

### Opcija B — razvoj iz IDE-a

Koristi se kad aktivno mijenjaš kod. Baza je u Dockeru, API i klijent se pokreću zasebno s hot reloadom.

```powershell
# 1. Samo baza
docker compose up -d db

# 2. JWT ključ (jednokratno)
dotnet user-secrets set "Jwt:Key" "tvoj-tajni-kljuc-od-najmanje-32-znaka" --project EKvarovi.Api

# 3. API (migracije i seed se pokreću automatski)
dotnet run --project EKvarovi.Api

# 4. Klijent (u drugom terminalu)
dotnet run --project EKvarovi.App
```

| Servis | Adresa |
|--------|--------|
| API | http://localhost:5275 |
| Swagger | http://localhost:5275/swagger |
| Klijent | http://localhost:5204 ili https://localhost:7097 |

Klijent u dev načinu čita API adresu iz `EKvarovi.App/wwwroot/appsettings.json` (`http://localhost:5275/`). CORS u API-ju dopušta portove klijenta iz `EKvarovi.Api/appsettings.json`.

> Migracije se ionako primjenjuju pri pokretanju API-ja. Ručno: `dotnet ef database update --project EKvarovi.Api`

---

## Demo računi

Lozinka za sve aktivne račune: **`Test123!`**

| E-mail | Ime | Uloge | Matična lokacija |
|--------|-----|-------|------------------|
| `admin@ekvarovi.hr` | Ana Adminović | Admin | UZ-01 |
| `manager@ekvarovi.hr` | Marko Upravić | Manager | UZ-01 |
| `tehnicar1@ekvarovi.hr` | Ivan Strujić | Technician | UZ-01 |
| `tehnicar2@ekvarovi.hr` | Petar Vodić | Technician | SL-01 |
| `tehnicar3@ekvarovi.hr` | Luka Mrežić | Technician | UZ-01 |
| `prijavitelj1@ekvarovi.hr` | Iva Školjić | Reporter | SK-01 |
| `prijavitelj2@ekvarovi.hr` | Sara Zdravković | Reporter | ZD-01 |
| `voditelj.tehnicar@ekvarovi.hr` | Tomislav Dvojić | **Manager + Technician** | UZ-02 |
| `neaktivan@ekvarovi.hr` | Bivši Djelatnik | Reporter (neaktivan) | SK-02 |

Račun `voditelj.tehnicar@ekvarovi.hr` namjerno ima dvije uloge — izbornik i ovlasti rade kao unija.

---

## Poslovna pravila (sažetak)

Sva pravila provjerava API u servisnom sloju, neovisno o UI-u. Kršenje vraća odgovarajući HTTP status (400, 403, 409) s hrvatskom porukom.

- **Prijave** — Reporter uređuje samo vlastitu prijavu u statusu Zaprimljeno; brisanje blokirano ako postoji intervencija; vrstu, prioritet i rok mijenjaju Manager i Admin.
- **Statusni tijek** — Zaprimljeno → Pregledano → Dodijeljeno → U radu → Riješeno → Zatvoreno; zatvaranje zahtijeva barem jednu uspješnu intervenciju i napomenu.
- **Dodjele** — Najviše jedna aktivna dodjela po prijavi; re-dodjela zahtijeva razlog; deaktivacija izvršitelja s aktivnim nalozima vraća 409.
- **Intervencije** — Pokreće ih izvršitelj s aktivne dodjele; neuspjeh ostavlja prijavu u statusu U radu; uspjeh je obavezan prije zatvaranja.
- **Materijali i privici** — Stavke samo na otvorenoj intervenciji; provjera tipa datoteke (potpis, MIME, ekstenzija); ograničen broj privitaka po namjeni.

Potpuni popis (BR-01 do BR-58): [docs/prd.md — poglavlje 8](docs/prd.md).

---

## Objavljivanje na Renderu

Repozitorij sadrži [`render.yaml`](render.yaml) (Render Blueprint) s tri resursa:

| Resurs | Namjena |
|--------|---------|
| PostgreSQL | Baza (free plan) |
| Web Service `ekvarovi-api` | Docker API, disk 1 GB na `/data/uploads` |
| Static Site `ekvarovi-app` | Blazor klijent |

**Koraci:** Render Dashboard → New → Blueprint → poveži repo → Apply.

Napomene:

- Persistent disk za upload zahtijeva **Starter plan** na API servisu (~7 USD/mj.). Bez diska uploadi nestaju pri redeployu.
- Besplatni plan uspava servis nakon neaktivnosti — prvi zahtjev nakon stanke može trajati do ~30 s. Za demo probudi API nekoliko minuta ranije.
- Build skripta za klijent: [`scripts/render-build-app.sh`](scripts/render-build-app.sh)

Detalji: [docs/tech.md — poglavlje 12.2](docs/tech.md).

---

## Dokumentacija

| Datoteka | Sadržaj |
|----------|---------|
| [docs/prd.md](docs/prd.md) | Zahtjevi, user stories, API, poslovna pravila |
| [docs/tech.md](docs/tech.md) | Arhitektura, konfiguracija, implementacijski obrasci |
| [docs/shema.md](docs/shema.md) | Model baze, seed podaci, DBML |
| [docs/plan.md](docs/plan.md) | Plan implementacije korak po korak |

## Build

```powershell
dotnet build
```
