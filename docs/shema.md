# Shema baze podataka — e-Kvarovi Županije

| | |
|---|---|
| **Verzija** | 1.0 |
| **Datum** | 9. rujna 2026. |
| **Sustav baze** | PostgreSQL 16 |
| **Pristup** | EF Core 9 (Npgsql), Code First, konvencija imenovanja `snake_case` |
| **Prateći dokumenti** | [prd.md](prd.md) — zahtjevi · [tech.md](tech.md) — tehnička realizacija |

Model ima **17 tablica**: 7 šifrarnika i 10 tablica jezgre.

---

## Sadržaj

1. [DBML dijagram](#1-dbml-dijagram)
2. [Opis tablica](#2-opis-tablica)
3. [Indeksi](#3-indeksi)
4. [Ograničenja i pravila integriteta](#4-ograničenja-i-pravila-integriteta)
5. [SQL DDL za PostgreSQL](#5-sql-ddl-za-postgresql)
6. [Seed podaci](#6-seed-podaci)
7. [Napomene o konvencijama](#7-napomene-o-konvencijama)

---

## 1. DBML dijagram

Kod je spreman za lijepljenje u [dbdiagram.io](https://dbdiagram.io).

```dbml
Project ekvarovi {
  database_type: 'PostgreSQL'
  Note: 'e-Kvarovi Županije — prijava i obrada kvarova u županijskim objektima'
}

// ───────────────────────── ŠIFRARNICI ─────────────────────────

Table location_types {
  id            integer      [pk, increment]
  name          varchar(100) [not null, unique]
  is_active     boolean      [not null, default: true]
  sort_order    integer      [not null, default: 0]
  Note: 'Upravna zgrada, Škola, Zdravstvena ustanova, Skladište'
}

Table fault_types {
  id            integer      [pk, increment]
  name          varchar(100) [not null, unique]
  is_active     boolean      [not null, default: true]
  sort_order    integer      [not null, default: 0]
  Note: 'Elektrika, Voda, Grijanje, Mreža, Građevinski radovi, Ostalo'
}

Table fault_priorities {
  id                        integer      [pk, increment]
  name                      varchar(50)  [not null, unique]
  default_resolution_hours  integer      [not null]
  color_hex                 varchar(7)   [not null]
  is_active                 boolean      [not null, default: true]
  sort_order                integer      [not null, default: 0]
  Note: 'Nizak 168h, Srednji 72h, Visok 24h, Kritičan 4h — zadani rok se predlaže pri kategorizaciji'
}

Table fault_statuses {
  id              integer      [pk, increment]
  name            varchar(50)  [not null, unique]
  is_closed_state boolean      [not null, default: false]
  is_active       boolean      [not null, default: true]
  sort_order      integer      [not null, default: 0]
  Note: 'Zaprimljeno(1) → Pregledano(2) → Dodijeljeno(3) → U radu(4) → Riješeno(5) → Zatvoreno(6)'
}

Table intervention_statuses {
  id            integer      [pk, increment]
  name          varchar(50)  [not null, unique]
  is_active     boolean      [not null, default: true]
  sort_order    integer      [not null, default: 0]
  Note: 'Planirana(1), U tijeku(2), Završena(3), Neuspješna(4)'
}

Table material_units {
  id            integer      [pk, increment]
  name          varchar(50)  [not null, unique]
  abbreviation  varchar(10)  [not null]
  is_active     boolean      [not null, default: true]
  sort_order    integer      [not null, default: 0]
  Note: 'Komad(kom), Metar(m), Litra(l), Kilogram(kg), Paket(pak)'
}

Table roles {
  id            integer      [pk, increment]
  code          varchar(20)  [not null, unique]
  name          varchar(50)  [not null]
  description   varchar(200)
  Note: 'code se koristi kao vrijednost role claima u JWT-u: Admin, Manager, Technician, Reporter'
}

// ───────────────────────── KORISNICI ─────────────────────────

Table users {
  id                  integer      [pk, increment]
  first_name          varchar(50)  [not null]
  last_name           varchar(50)  [not null]
  email               varchar(150) [not null, unique]
  password_hash       varchar(200) [not null]
  phone               varchar(30)
  specialization      varchar(100)
  home_location_id    integer
  is_active           boolean      [not null, default: true]
  deactivated_at      timestamptz
  created_at          timestamptz  [not null]
  created_by_user_id  integer      [not null]
  updated_at          timestamptz
  updated_by_user_id  integer
  is_deleted          boolean      [not null, default: false]
  deleted_at          timestamptz
  Note: 'Jedna tablica za sve uloge. Specijalizacija je relevantna za izvršitelje, matična lokacija za prijavitelje.'
}

Table user_roles {
  user_id       integer      [not null]
  role_id       integer      [not null]
  assigned_at   timestamptz  [not null]

  indexes {
    (user_id, role_id) [pk]
  }
  Note: 'Jedan račun može imati više uloga — ovlasti se u aplikaciji zbrajaju.'
}

// ───────────────────────── LOKACIJE ─────────────────────────

Table locations {
  id                  integer      [pk, increment]
  code                varchar(20)  [not null, unique]
  name                varchar(150) [not null]
  address             varchar(200) [not null]
  city                varchar(100) [not null]
  postal_code         varchar(10)
  location_type_id    integer      [not null]
  contact_person      varchar(100)
  contact_phone       varchar(30)
  is_active           boolean      [not null, default: true]
  created_at          timestamptz  [not null]
  created_by_user_id  integer      [not null]
  updated_at          timestamptz
  updated_by_user_id  integer
  is_deleted          boolean      [not null, default: false]
  deleted_at          timestamptz
}

// ───────────────────────── PRIJAVE ─────────────────────────

Table fault_reports {
  id                    integer       [pk, increment]
  report_number         varchar(20)   [not null, unique]
  title                 varchar(200)  [not null]
  description           varchar(2000) [not null]
  location_id           integer       [not null]
  fault_type_id         integer
  fault_priority_id     integer
  fault_status_id       integer       [not null]
  due_date              timestamptz
  reported_by_user_id   integer       [not null]
  reported_at           timestamptz   [not null]
  reviewed_by_user_id   integer
  reviewed_at           timestamptz
  resolved_at           timestamptz
  closed_by_user_id     integer
  closed_at             timestamptz
  closing_note          varchar(1000)
  created_at            timestamptz   [not null]
  created_by_user_id    integer       [not null]
  updated_at            timestamptz
  updated_by_user_id    integer
  is_deleted            boolean       [not null, default: false]
  deleted_at            timestamptz

  Note: '''
  Vrsta, prioritet i rok su prazni dok Manager ne obavi kategorizaciju.
  Broj prijave: KV-{godina}-{6 znamenki}.
  Kašnjenje se NE sprema — računa se kao due_date < now() uz status koji nije Riješeno ni Zatvoreno.
  '''
}

// ───────────────────── RADNI NALOZI (POVIJEST DODJELA) ─────────────────────

Table work_assignments {
  id                    integer       [pk, increment]
  fault_report_id       integer       [not null]
  technician_user_id    integer       [not null]
  assigned_by_user_id   integer       [not null]
  assigned_at           timestamptz   [not null]
  is_active             boolean       [not null, default: true]
  unassigned_at         timestamptz
  reassign_reason       varchar(500)
  note                  varchar(1000)
  created_at            timestamptz   [not null]
  created_by_user_id    integer       [not null]
  updated_at            timestamptz
  updated_by_user_id    integer
  is_deleted            boolean       [not null, default: false]
  deleted_at            timestamptz

  indexes {
    (fault_report_id) [unique, name: 'ux_work_assignments_active_per_report', note: 'PARCIJALNI: WHERE is_active AND NOT is_deleted — najviše jedna aktivna dodjela po prijavi']
    (technician_user_id, is_active)
  }

  Note: '''
  Povijest dodjela. Re-dodjela deaktivira prethodni redak (is_active = false, unassigned_at = now)
  i umeće novi — stari zapis se NIKAD ne briše.
  Nalog može postojati bez ijedne intervencije i tijekom vremena ih imati više.
  '''
}

// ───────────────────────── INTERVENCIJE ─────────────────────────

Table interventions {
  id                      integer       [pk, increment]
  work_assignment_id      integer       [not null]
  fault_report_id         integer       [not null]
  technician_user_id      integer       [not null]
  intervention_status_id  integer       [not null]
  started_at              timestamptz
  finished_at             timestamptz
  note                    varchar(2000)
  failure_reason          varchar(500)
  created_at              timestamptz   [not null]
  created_by_user_id      integer       [not null]
  updated_at              timestamptz
  updated_by_user_id      integer
  is_deleted              boolean       [not null, default: false]
  deleted_at              timestamptz

  Note: '''
  fault_report_id je denormaliziran (izvediv preko naloga) radi filtriranja i provjere pravila
  "najviše jedna intervencija U tijeku po prijavi".
  technician_user_id je snimka izvršitelja u trenutku pokretanja.
  Neuspješna intervencija ostaje u povijesti; na istoj prijavi može se otvoriti nova.
  '''
}

// ───────────────────────── MATERIJALI ─────────────────────────

Table materials {
  id                  integer        [pk, increment]
  code                varchar(20)    [not null, unique]
  name                varchar(150)   [not null]
  material_unit_id    integer        [not null]
  unit_price          decimal(10,2)  [not null, default: 0]
  is_active           boolean        [not null, default: true]
  created_at          timestamptz    [not null]
  created_by_user_id  integer        [not null]
  updated_at          timestamptz
  updated_by_user_id  integer
  is_deleted          boolean        [not null, default: false]
  deleted_at          timestamptz
  Note: 'Cijena u eurima. Promjena cjenika ne utječe na ranije evidentirane utroške.'
}

Table intervention_materials {
  id                    integer        [pk, increment]
  intervention_id       integer        [not null]
  material_id           integer        [not null]
  quantity              decimal(10,2)  [not null]
  unit_price_snapshot   decimal(10,2)  [not null]
  created_at            timestamptz    [not null]
  created_by_user_id    integer        [not null]

  indexes {
    (intervention_id, material_id) [unique, name: 'ux_intervention_materials_unique_item']
  }
  Note: 'Trošak stavke = quantity × unit_price_snapshot. Količina mora biti > 0.'
}

// ───────────────────────── PRIVICI ─────────────────────────

Table attachments {
  id                    integer       [pk, increment]
  purpose               smallint      [not null]
  fault_report_id       integer
  intervention_id       integer
  original_file_name    varchar(255)  [not null]
  stored_file_name      varchar(100)  [not null]
  relative_path         varchar(400)  [not null]
  content_type          varchar(100)  [not null]
  size_bytes            bigint        [not null]
  uploaded_by_user_id   integer       [not null]
  uploaded_at           timestamptz   [not null]
  is_deleted            boolean       [not null, default: false]
  deleted_at            timestamptz

  Note: '''
  purpose: 1 = fotografija prije (veže se na prijavu)
           2 = fotografija poslije (veže se na intervenciju)
           3 = dokument (veže se na prijavu)
  CHECK ck_attachments_purpose_target osigurava da je popunjena točno odgovarajuća veza.
  Datoteke se čuvaju izvan wwwroot; u bazi su samo metapodaci.
  '''
}

// ───────────────────── POVIJEST PROMJENA (VREMENSKA CRTA) ─────────────────────

Table fault_report_histories {
  id                  integer       [pk, increment]
  fault_report_id     integer       [not null]
  change_type         smallint      [not null]
  old_value           varchar(200)
  new_value           varchar(200)
  note                varchar(1000)
  changed_by_user_id  integer       [not null]
  changed_at          timestamptz   [not null]

  Note: '''
  change_type: 1 Created, 2 StatusChanged, 3 PriorityChanged, 4 TypeChanged, 5 DueDateChanged,
               6 Assigned, 7 Reassigned, 8 InterventionStarted, 9 InterventionFinished,
               10 Reopened, 11 Closed
  Zapisi se stvaraju u istoj transakciji kao i sama promjena. Nikad se ne mijenjaju ni brišu.
  '''
}

// ───────────────────────── VEZE ─────────────────────────

Ref: locations.location_type_id           > location_types.id
Ref: users.home_location_id               > locations.id

Ref: user_roles.user_id                   > users.id      [delete: cascade]
Ref: user_roles.role_id                   > roles.id

Ref: fault_reports.location_id            > locations.id
Ref: fault_reports.fault_type_id          > fault_types.id
Ref: fault_reports.fault_priority_id      > fault_priorities.id
Ref: fault_reports.fault_status_id        > fault_statuses.id
Ref: fault_reports.reported_by_user_id    > users.id
Ref: fault_reports.reviewed_by_user_id    > users.id
Ref: fault_reports.closed_by_user_id      > users.id

Ref: work_assignments.fault_report_id     > fault_reports.id
Ref: work_assignments.technician_user_id  > users.id
Ref: work_assignments.assigned_by_user_id > users.id

Ref: interventions.work_assignment_id     > work_assignments.id
Ref: interventions.fault_report_id        > fault_reports.id
Ref: interventions.technician_user_id     > users.id
Ref: interventions.intervention_status_id > intervention_statuses.id

Ref: materials.material_unit_id           > material_units.id
Ref: intervention_materials.intervention_id > interventions.id
Ref: intervention_materials.material_id   > materials.id

Ref: attachments.fault_report_id          > fault_reports.id
Ref: attachments.intervention_id          > interventions.id
Ref: attachments.uploaded_by_user_id      > users.id

Ref: fault_report_histories.fault_report_id    > fault_reports.id
Ref: fault_report_histories.changed_by_user_id > users.id
```

---

## 2. Opis tablica

### 2.1 Zajednička polja

Sedam glavnih entiteta (`users`, `locations`, `fault_reports`, `work_assignments`, `interventions`, `materials`, `attachments`) nosi polja praćenja:

| Stupac | Tip | Značenje |
|---|---|---|
| `created_at` | `timestamptz` | Vrijeme nastanka, popunjava se automatski |
| `created_by_user_id` | `integer` | Tko je zapis stvorio |
| `updated_at` | `timestamptz` null | Vrijeme zadnje izmjene |
| `updated_by_user_id` | `integer` null | Tko je zadnji mijenjao |
| `is_deleted` | `boolean` | Oznaka logičkog brisanja, uz globalni filtar upita |
| `deleted_at` | `timestamptz` null | Kada je logički obrisano |

Tablica `attachments` koristi `uploaded_at` i `uploaded_by_user_id` umjesto para `created_*`, jer je pojam nastanka ovdje istovjetan uploadu.

### 2.2 Šifrarnici

Svih sedam šifrarnika ima `id`, `name`, `is_active` i `sort_order`. Vrijednosti se seedaju fiksnim identifikatorima kako bi se na njih moglo referencirati iz koda. Admin ih smije dodavati, preimenovati i deaktivirati, ali ne i fizički brisati.

| Tablica | Posebni stupci | Uloga posebnog stupca |
|---|---|---|
| `fault_priorities` | `default_resolution_hours`, `color_hex` | Predlaganje roka pri kategorizaciji i bojanje značke u UI-u |
| `fault_statuses` | `is_closed_state` | Razlikovanje završnog stanja bez tvrdo kodiranog identifikatora |
| `material_units` | `abbreviation` | Kratki prikaz uz količinu (5 kom, 12,5 m) |
| `roles` | `code`, `description` | `code` je vrijednost role claima u JWT-u |

### 2.3 `users`

| Stupac | Tip | Obavezno | Napomena |
|---|---|---|---|
| `id` | `integer` | da | Primarni ključ |
| `first_name`, `last_name` | `varchar(50)` | da | |
| `email` | `varchar(150)` | da | Jedinstven, služi kao korisničko ime |
| `password_hash` | `varchar(200)` | da | BCrypt, sadrži i sol |
| `phone` | `varchar(30)` | ne | |
| `specialization` | `varchar(100)` | ne | Struka izvršitelja, npr. Elektrotehnika |
| `home_location_id` | `integer` | ne | Matična lokacija, predodabrana pri prijavi kvara |
| `is_active` | `boolean` | da | Neaktivan korisnik se ne može prijaviti |
| `deactivated_at` | `timestamptz` | ne | Kada je deaktiviran |

Zasebne tablice za Reportera i Technicijana namjerno ne postoje. Uloga se čita iz `user_roles`, a veze prema prijavama i nalozima idu izravno na korisnika, čime nema dvostruke evidencije ni potrebe za sinkronizacijom dviju tablica.

### 2.4 `fault_reports`

| Stupac | Tip | Obavezno | Napomena |
|---|---|---|---|
| `report_number` | `varchar(20)` | da | Jedinstven, `KV-2026-000042` |
| `title` | `varchar(200)` | da | 5–200 znakova (BR-02) |
| `description` | `varchar(2000)` | da | 10–2000 znakova (BR-02) |
| `location_id` | `integer` | da | Mora biti aktivna lokacija (BR-01) |
| `fault_type_id` | `integer` | **ne** | Prazno do kategorizacije |
| `fault_priority_id` | `integer` | **ne** | Prazno do kategorizacije |
| `fault_status_id` | `integer` | da | Pri kreiranju uvijek 1 (Zaprimljeno) |
| `due_date` | `timestamptz` | ne | Obavezan kad je prioritet Kritičan (BR-04) |
| `reported_by_user_id` | `integer` | da | Podnositelj |
| `reported_at` | `timestamptz` | da | Vrijeme prijave |
| `reviewed_by_user_id`, `reviewed_at` | | ne | Tko je i kada pregledao |
| `resolved_at` | `timestamptz` | ne | Postavlja se uspješnom intervencijom |
| `closed_by_user_id`, `closed_at` | | ne | Tko je i kada zatvorio |
| `closing_note` | `varchar(1000)` | ne | Obavezna pri zatvaranju (BR-14), na razini aplikacije |

Kašnjenje nije stupac. Računa se kao `due_date < now()` uz status koji nije Riješeno ni Zatvoreno, i to i u SQL upitu i u DTO projekciji, čime nema rizika od zastarjele spremljene vrijednosti.

### 2.5 `work_assignments`

| Stupac | Tip | Obavezno | Napomena |
|---|---|---|---|
| `fault_report_id` | `integer` | da | Prijava |
| `technician_user_id` | `integer` | da | Izvršitelj, mora imati ulogu Technician i biti aktivan (BR-16) |
| `assigned_by_user_id` | `integer` | da | Manager ili Admin koji je dodijelio |
| `assigned_at` | `timestamptz` | da | |
| `is_active` | `boolean` | da | Oznaka aktivne dodjele |
| `unassigned_at` | `timestamptz` | ne | Popunjava se pri deaktivaciji |
| `reassign_reason` | `varchar(500)` | ne | Obavezan pri re-dodjeli (BR-17) |
| `note` | `varchar(1000)` | ne | |

Ovo je tablica koja nosi cijelu povijest dodjela. Aktivna dodjela prepoznaje se po `is_active`, a jedinstvenost je zajamčena parcijalnim indeksom (poglavlje 3). Stari zapisi se ne brišu ni ne mijenjaju osim postavljanja `is_active`, `unassigned_at` i razloga.

### 2.6 `interventions`

| Stupac | Tip | Obavezno | Napomena |
|---|---|---|---|
| `work_assignment_id` | `integer` | da | Nalog kojem intervencija pripada |
| `fault_report_id` | `integer` | da | Denormalizirano radi filtriranja |
| `technician_user_id` | `integer` | da | Snimka izvršitelja |
| `intervention_status_id` | `integer` | da | Planirana, U tijeku, Završena, Neuspješna |
| `started_at` | `timestamptz` | ne | Prazno dok je Planirana |
| `finished_at` | `timestamptz` | ne | Obavezno pri završetku (BR-28) |
| `note` | `varchar(2000)` | ne | Obavezna pri završetku, najmanje 10 znakova (BR-28) |
| `failure_reason` | `varchar(500)` | ne | Obavezan pri neuspješnom ishodu (BR-29) |

### 2.7 `materials` i `intervention_materials`

Katalog nosi šifru, naziv, mjernu jedinicu i jediničnu cijenu u eurima. Stavka utroška nosi količinu i snimku cijene, pa naknadna promjena cjenika ne mijenja povijesne troškove (BR-38). Jedinstveni indeks nad parom intervencije i materijala sprječava dvostruki unos istog materijala (BR-36).

### 2.8 `attachments`

Jedna tablica pokriva sve tri namjene, uz dvije opcionalne veze i uvjet koji osigurava da je popunjena točno ona koja odgovara namjeni. Metapodaci obuhvaćaju izvorni naziv koji korisnik vidi pri preuzimanju, naziv pod kojim je datoteka spremljena (GUID), relativnu putanju, MIME tip i veličinu.

### 2.9 `fault_report_histories`

Zapisi vremenske crte su samo za upis i čitanje. Za svaku promjenu bilježe vrstu, staru i novu vrijednost kao tekst, napomenu (primjerice razlog re-dodjele ili obrazloženje vraćanja u rad), korisnika i vrijeme.

---

## 3. Indeksi

### 3.1 Jedinstveni indeksi

| Naziv | Tablica | Stupci | Napomena |
|---|---|---|---|
| `ux_users_email` | `users` | `email` | |
| `ux_locations_code` | `locations` | `code` | |
| `ux_materials_code` | `materials` | `code` | |
| `ux_roles_code` | `roles` | `code` | |
| `ux_fault_reports_report_number` | `fault_reports` | `report_number` | Zaštita od dvostrukog broja pri istovremenom upisu |
| **`ux_work_assignments_active_per_report`** | `work_assignments` | `fault_report_id` | **Parcijalni:** `WHERE is_active AND NOT is_deleted` |
| `ux_intervention_materials_unique_item` | `intervention_materials` | `intervention_id, material_id` | |

Parcijalni indeks nad dodjelama je najvažnije ograničenje u shemi. Bez njega bi pravilo o najviše jednoj aktivnoj dodjeli ovisilo isključivo o kodu i palo bi kod dva istovremena zahtjeva.

```sql
CREATE UNIQUE INDEX ux_work_assignments_active_per_report
    ON work_assignments (fault_report_id)
    WHERE is_active AND NOT is_deleted;
```

### 3.2 Indeksi za pretragu i filtriranje

| Tablica | Stupci | Zašto |
|---|---|---|
| `fault_reports` | `fault_status_id` | Najčešći filtar i grupiranje na dashboardu |
| `fault_reports` | `location_id` | Filtar po lokaciji i metrika vodećih lokacija |
| `fault_reports` | `fault_priority_id` | Filtar i metrika otvorenih po prioritetu |
| `fault_reports` | `fault_type_id` | Raspodjela po vrsti kvara |
| `fault_reports` | `reported_at DESC` | Zadano sortiranje popisa |
| `fault_reports` | `due_date` | Izračun prekoračenja roka |
| `fault_reports` | `reported_by_user_id` | Scenarij `/faultreports/mine` |
| `work_assignments` | `technician_user_id, is_active` | Scenarij `/workassignments/mine` |
| `work_assignments` | `fault_report_id` | Povijest dodjela na profilu prijave |
| `interventions` | `fault_report_id` | Popis intervencija na prijavi |
| `interventions` | `work_assignment_id` | |
| `interventions` | `technician_user_id` | Filtar po izvršitelju |
| `interventions` | `intervention_status_id` | Provjera pravila o jednoj intervenciji u tijeku |
| `interventions` | `started_at DESC` | Zadano sortiranje popisa |
| `attachments` | `fault_report_id`, `intervention_id` | Dohvat privitaka po nadređenom zapisu |
| `fault_report_histories` | `fault_report_id, changed_at` | Vremenska crta u kronološkom redoslijedu |
| `users` | `home_location_id` | |
| `intervention_materials` | `material_id` | Provjera je li materijal u upotrebi prije deaktivacije |

EF Core sam stvara indekse nad stranim ključevima, pa se u konfiguraciji izrijekom navode samo oni koji to nisu — sortirajući indeksi i složeni indeksi.

---

## 4. Ograničenja i pravila integriteta

### 4.1 Uvjeti u bazi

| Naziv | Tablica | Uvjet |
|---|---|---|
| `ck_attachments_purpose_target` | `attachments` | Namjena 1 i 3 traže popunjenu prijavu i praznu intervenciju; namjena 2 obrnuto |
| `ck_attachments_purpose_range` | `attachments` | `purpose BETWEEN 1 AND 3` |
| `ck_intervention_materials_quantity` | `intervention_materials` | `quantity > 0 AND quantity <= 99999.99` |
| `ck_materials_unit_price` | `materials` | `unit_price >= 0` |
| `ck_fault_priorities_hours` | `fault_priorities` | `default_resolution_hours > 0` |
| `ck_interventions_times` | `interventions` | `finished_at IS NULL OR started_at IS NULL OR finished_at >= started_at` |
| `ck_work_assignments_unassigned` | `work_assignments` | `is_active = false OR unassigned_at IS NULL` |

### 4.2 Ponašanje pri brisanju

Svi strani ključevi postavljeni su na `ON DELETE RESTRICT`, uz jednu iznimku: `user_roles` kaskadno prati korisnika, jer veza uloge nema smisla bez računa.

U praksi se fizički ne briše ništa. `SaveChanges` presreće stanje brisanja i pretvara ga u postavljanje `is_deleted`, a globalni filtar upita izostavlja takve zapise iz svih dohvata. Povijesni podaci — dodjele, intervencije i vremenska crta — ne brišu se ni logički.

### 4.3 Pravila koja se **ne** provode u bazi

Sljedeća pravila iz PRD-a ovise o vrijednostima šifrarnika ili o kontekstu pozivatelja i zato žive isključivo u servisnom sloju:

| Pravilo | Zašto ne u bazi |
|---|---|
| Najviše jedna intervencija u statusu U tijeku po prijavi (BR-25) | Uvjet ovisi o identifikatoru statusa iz šifrarnika koji se može mijenjati |
| Dopušteni statusni prijelazi (BR-09, BR-11) | Zahtijeva poznavanje prethodnog stanja i uloge pozivatelja |
| Zatvaranje traži barem jednu uspješnu intervenciju (BR-13) | Agregatna provjera nad drugom tablicom |
| Obavezan rok kod kritičnog prioriteta (BR-04) | Ovisi o vrijednosti iz šifrarnika |
| Ograničenje broja privitaka po namjeni (BR-42) | Agregatna provjera |
| Vidljivost zapisa po ulozi (BR-47, BR-48) | Ovisi o identitetu iz JWT-a |

---

## 5. SQL DDL za PostgreSQL

Skripta odgovara stanju nakon prve EF Core migracije. Služi kao referenca i za ručno postavljanje baze bez pokretanja aplikacije.

```sql
-- ═══════════════════════ ŠIFRARNICI ═══════════════════════

CREATE TABLE location_types (
    id          integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name        varchar(100) NOT NULL UNIQUE,
    is_active   boolean      NOT NULL DEFAULT true,
    sort_order  integer      NOT NULL DEFAULT 0
);

CREATE TABLE fault_types (
    id          integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name        varchar(100) NOT NULL UNIQUE,
    is_active   boolean      NOT NULL DEFAULT true,
    sort_order  integer      NOT NULL DEFAULT 0
);

CREATE TABLE fault_priorities (
    id                        integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name                      varchar(50) NOT NULL UNIQUE,
    default_resolution_hours  integer     NOT NULL,
    color_hex                 varchar(7)  NOT NULL,
    is_active                 boolean     NOT NULL DEFAULT true,
    sort_order                integer     NOT NULL DEFAULT 0,
    CONSTRAINT ck_fault_priorities_hours CHECK (default_resolution_hours > 0)
);

CREATE TABLE fault_statuses (
    id               integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name             varchar(50) NOT NULL UNIQUE,
    is_closed_state  boolean     NOT NULL DEFAULT false,
    is_active        boolean     NOT NULL DEFAULT true,
    sort_order       integer     NOT NULL DEFAULT 0
);

CREATE TABLE intervention_statuses (
    id          integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name        varchar(50) NOT NULL UNIQUE,
    is_active   boolean     NOT NULL DEFAULT true,
    sort_order  integer     NOT NULL DEFAULT 0
);

CREATE TABLE material_units (
    id            integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    name          varchar(50) NOT NULL UNIQUE,
    abbreviation  varchar(10) NOT NULL,
    is_active     boolean     NOT NULL DEFAULT true,
    sort_order    integer     NOT NULL DEFAULT 0
);

CREATE TABLE roles (
    id           integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    code         varchar(20) NOT NULL UNIQUE,
    name         varchar(50) NOT NULL,
    description  varchar(200)
);

-- ═══════════════════════ LOKACIJE I KORISNICI ═══════════════════════

CREATE TABLE locations (
    id                  integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    code                varchar(20)  NOT NULL UNIQUE,
    name                varchar(150) NOT NULL,
    address             varchar(200) NOT NULL,
    city                varchar(100) NOT NULL,
    postal_code         varchar(10),
    location_type_id    integer      NOT NULL REFERENCES location_types (id) ON DELETE RESTRICT,
    contact_person      varchar(100),
    contact_phone       varchar(30),
    is_active           boolean      NOT NULL DEFAULT true,
    created_at          timestamptz  NOT NULL,
    created_by_user_id  integer      NOT NULL,
    updated_at          timestamptz,
    updated_by_user_id  integer,
    is_deleted          boolean      NOT NULL DEFAULT false,
    deleted_at          timestamptz
);

CREATE TABLE users (
    id                  integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    first_name          varchar(50)  NOT NULL,
    last_name           varchar(50)  NOT NULL,
    email               varchar(150) NOT NULL UNIQUE,
    password_hash       varchar(200) NOT NULL,
    phone               varchar(30),
    specialization      varchar(100),
    home_location_id    integer REFERENCES locations (id) ON DELETE RESTRICT,
    is_active           boolean      NOT NULL DEFAULT true,
    deactivated_at      timestamptz,
    created_at          timestamptz  NOT NULL,
    created_by_user_id  integer      NOT NULL,
    updated_at          timestamptz,
    updated_by_user_id  integer,
    is_deleted          boolean      NOT NULL DEFAULT false,
    deleted_at          timestamptz
);

CREATE TABLE user_roles (
    user_id      integer     NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    role_id      integer     NOT NULL REFERENCES roles (id) ON DELETE RESTRICT,
    assigned_at  timestamptz NOT NULL,
    PRIMARY KEY (user_id, role_id)
);

-- ═══════════════════════ PRIJAVE ═══════════════════════

CREATE TABLE fault_reports (
    id                   integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    report_number        varchar(20)   NOT NULL UNIQUE,
    title                varchar(200)  NOT NULL,
    description          varchar(2000) NOT NULL,
    location_id          integer       NOT NULL REFERENCES locations (id)        ON DELETE RESTRICT,
    fault_type_id        integer                REFERENCES fault_types (id)      ON DELETE RESTRICT,
    fault_priority_id    integer                REFERENCES fault_priorities (id) ON DELETE RESTRICT,
    fault_status_id      integer       NOT NULL REFERENCES fault_statuses (id)   ON DELETE RESTRICT,
    due_date             timestamptz,
    reported_by_user_id  integer       NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    reported_at          timestamptz   NOT NULL,
    reviewed_by_user_id  integer                REFERENCES users (id) ON DELETE RESTRICT,
    reviewed_at          timestamptz,
    resolved_at          timestamptz,
    closed_by_user_id    integer                REFERENCES users (id) ON DELETE RESTRICT,
    closed_at            timestamptz,
    closing_note         varchar(1000),
    created_at           timestamptz   NOT NULL,
    created_by_user_id   integer       NOT NULL,
    updated_at           timestamptz,
    updated_by_user_id   integer,
    is_deleted           boolean       NOT NULL DEFAULT false,
    deleted_at           timestamptz
);

-- ═══════════════════════ RADNI NALOZI ═══════════════════════

CREATE TABLE work_assignments (
    id                    integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    fault_report_id       integer      NOT NULL REFERENCES fault_reports (id) ON DELETE RESTRICT,
    technician_user_id    integer      NOT NULL REFERENCES users (id)         ON DELETE RESTRICT,
    assigned_by_user_id   integer      NOT NULL REFERENCES users (id)         ON DELETE RESTRICT,
    assigned_at           timestamptz  NOT NULL,
    is_active             boolean      NOT NULL DEFAULT true,
    unassigned_at         timestamptz,
    reassign_reason       varchar(500),
    note                  varchar(1000),
    created_at            timestamptz  NOT NULL,
    created_by_user_id    integer      NOT NULL,
    updated_at            timestamptz,
    updated_by_user_id    integer,
    is_deleted            boolean      NOT NULL DEFAULT false,
    deleted_at            timestamptz,
    CONSTRAINT ck_work_assignments_unassigned
        CHECK (is_active = false OR unassigned_at IS NULL)
);

-- NAJVAŽNIJE OGRANIČENJE SHEME: najviše jedna aktivna dodjela po prijavi
CREATE UNIQUE INDEX ux_work_assignments_active_per_report
    ON work_assignments (fault_report_id)
    WHERE is_active AND NOT is_deleted;

-- ═══════════════════════ INTERVENCIJE ═══════════════════════

CREATE TABLE interventions (
    id                      integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    work_assignment_id      integer       NOT NULL REFERENCES work_assignments (id)      ON DELETE RESTRICT,
    fault_report_id         integer       NOT NULL REFERENCES fault_reports (id)         ON DELETE RESTRICT,
    technician_user_id      integer       NOT NULL REFERENCES users (id)                 ON DELETE RESTRICT,
    intervention_status_id  integer       NOT NULL REFERENCES intervention_statuses (id) ON DELETE RESTRICT,
    started_at              timestamptz,
    finished_at             timestamptz,
    note                    varchar(2000),
    failure_reason          varchar(500),
    created_at              timestamptz   NOT NULL,
    created_by_user_id      integer       NOT NULL,
    updated_at              timestamptz,
    updated_by_user_id      integer,
    is_deleted              boolean       NOT NULL DEFAULT false,
    deleted_at              timestamptz,
    CONSTRAINT ck_interventions_times
        CHECK (finished_at IS NULL OR started_at IS NULL OR finished_at >= started_at)
);

-- ═══════════════════════ MATERIJALI ═══════════════════════

CREATE TABLE materials (
    id                  integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    code                varchar(20)   NOT NULL UNIQUE,
    name                varchar(150)  NOT NULL,
    material_unit_id    integer       NOT NULL REFERENCES material_units (id) ON DELETE RESTRICT,
    unit_price          numeric(10,2) NOT NULL DEFAULT 0,
    is_active           boolean       NOT NULL DEFAULT true,
    created_at          timestamptz   NOT NULL,
    created_by_user_id  integer       NOT NULL,
    updated_at          timestamptz,
    updated_by_user_id  integer,
    is_deleted          boolean       NOT NULL DEFAULT false,
    deleted_at          timestamptz,
    CONSTRAINT ck_materials_unit_price CHECK (unit_price >= 0)
);

CREATE TABLE intervention_materials (
    id                   integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    intervention_id      integer       NOT NULL REFERENCES interventions (id) ON DELETE RESTRICT,
    material_id          integer       NOT NULL REFERENCES materials (id)     ON DELETE RESTRICT,
    quantity             numeric(10,2) NOT NULL,
    unit_price_snapshot  numeric(10,2) NOT NULL,
    created_at           timestamptz   NOT NULL,
    created_by_user_id   integer       NOT NULL,
    CONSTRAINT ck_intervention_materials_quantity
        CHECK (quantity > 0 AND quantity <= 99999.99)
);

CREATE UNIQUE INDEX ux_intervention_materials_unique_item
    ON intervention_materials (intervention_id, material_id);

-- ═══════════════════════ PRIVICI ═══════════════════════

CREATE TABLE attachments (
    id                   integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    purpose              smallint     NOT NULL,
    fault_report_id      integer               REFERENCES fault_reports (id) ON DELETE RESTRICT,
    intervention_id      integer               REFERENCES interventions (id) ON DELETE RESTRICT,
    original_file_name   varchar(255) NOT NULL,
    stored_file_name     varchar(100) NOT NULL,
    relative_path        varchar(400) NOT NULL,
    content_type         varchar(100) NOT NULL,
    size_bytes           bigint       NOT NULL,
    uploaded_by_user_id  integer      NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    uploaded_at          timestamptz  NOT NULL,
    is_deleted           boolean      NOT NULL DEFAULT false,
    deleted_at           timestamptz,
    CONSTRAINT ck_attachments_purpose_range CHECK (purpose BETWEEN 1 AND 3),
    CONSTRAINT ck_attachments_purpose_target CHECK (
        (purpose IN (1, 3) AND fault_report_id IS NOT NULL AND intervention_id IS NULL)
        OR
        (purpose = 2 AND intervention_id IS NOT NULL AND fault_report_id IS NULL)
    )
);

-- ═══════════════════════ POVIJEST PROMJENA ═══════════════════════

CREATE TABLE fault_report_histories (
    id                  integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    fault_report_id     integer      NOT NULL REFERENCES fault_reports (id) ON DELETE RESTRICT,
    change_type         smallint     NOT NULL,
    old_value           varchar(200),
    new_value           varchar(200),
    note                varchar(1000),
    changed_by_user_id  integer      NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    changed_at          timestamptz  NOT NULL
);

-- ═══════════════════════ INDEKSI ZA FILTRIRANJE ═══════════════════════

CREATE INDEX ix_fault_reports_status         ON fault_reports (fault_status_id);
CREATE INDEX ix_fault_reports_location       ON fault_reports (location_id);
CREATE INDEX ix_fault_reports_priority       ON fault_reports (fault_priority_id);
CREATE INDEX ix_fault_reports_type           ON fault_reports (fault_type_id);
CREATE INDEX ix_fault_reports_reported_at    ON fault_reports (reported_at DESC);
CREATE INDEX ix_fault_reports_due_date       ON fault_reports (due_date);
CREATE INDEX ix_fault_reports_reported_by    ON fault_reports (reported_by_user_id);

CREATE INDEX ix_work_assignments_technician  ON work_assignments (technician_user_id, is_active);
CREATE INDEX ix_work_assignments_report      ON work_assignments (fault_report_id);

CREATE INDEX ix_interventions_report         ON interventions (fault_report_id);
CREATE INDEX ix_interventions_assignment     ON interventions (work_assignment_id);
CREATE INDEX ix_interventions_technician     ON interventions (technician_user_id);
CREATE INDEX ix_interventions_status         ON interventions (intervention_status_id);
CREATE INDEX ix_interventions_started_at     ON interventions (started_at DESC);

CREATE INDEX ix_attachments_report           ON attachments (fault_report_id);
CREATE INDEX ix_attachments_intervention     ON attachments (intervention_id);

CREATE INDEX ix_histories_report_changed     ON fault_report_histories (fault_report_id, changed_at);

CREATE INDEX ix_users_home_location          ON users (home_location_id);
CREATE INDEX ix_intervention_materials_mat   ON intervention_materials (material_id);
```

---

## 6. Seed podaci

Seed se izvodi pri pokretanju API-ja, nakon primjene migracija, i to samo ako je konfiguracijom omogućen. Podijeljen je u dva dijela: šifrarnici (uvijek) i demo podaci (samo kad je `Seed:DemoData` uključen).

### 6.1 Šifrarnici

Umeću se s fiksnim identifikatorima kroz `HasData`, kako bi se na njih moglo referencirati iz koda i seed podataka.

```sql
INSERT INTO location_types (id, name, sort_order) VALUES
    (1, 'Upravna zgrada', 1), (2, 'Škola', 2),
    (3, 'Zdravstvena ustanova', 3), (4, 'Skladište', 4);

INSERT INTO fault_types (id, name, sort_order) VALUES
    (1, 'Elektrika', 1), (2, 'Voda', 2), (3, 'Grijanje', 3),
    (4, 'Mreža', 4), (5, 'Građevinski radovi', 5), (6, 'Ostalo', 6);

INSERT INTO fault_priorities (id, name, default_resolution_hours, color_hex, sort_order) VALUES
    (1, 'Nizak',    168, '#6c757d', 1),
    (2, 'Srednji',   72, '#0d6efd', 2),
    (3, 'Visok',     24, '#fd7e14', 3),
    (4, 'Kritičan',   4, '#dc3545', 4);

INSERT INTO fault_statuses (id, name, is_closed_state, sort_order) VALUES
    (1, 'Zaprimljeno', false, 1), (2, 'Pregledano',  false, 2),
    (3, 'Dodijeljeno', false, 3), (4, 'U radu',      false, 4),
    (5, 'Riješeno',    false, 5), (6, 'Zatvoreno',   true,  6);

INSERT INTO intervention_statuses (id, name, sort_order) VALUES
    (1, 'Planirana', 1), (2, 'U tijeku', 2),
    (3, 'Završena', 3), (4, 'Neuspješna', 4);

INSERT INTO material_units (id, name, abbreviation, sort_order) VALUES
    (1, 'Komad', 'kom', 1), (2, 'Metar', 'm', 2), (3, 'Litra', 'l', 3),
    (4, 'Kilogram', 'kg', 4), (5, 'Paket', 'pak', 5);

INSERT INTO roles (id, code, name, description) VALUES
    (1, 'Admin',      'Administrator', 'Pun pristup svim podacima i postavkama'),
    (2, 'Manager',    'Upravitelj',    'Pregled svih prijava, kategorizacija, dodjela i zatvaranje'),
    (3, 'Technician', 'Izvršitelj',    'Rad na vlastitim nalozima i intervencijama'),
    (4, 'Reporter',   'Prijavitelj',   'Prijava kvarova i pregled vlastitih prijava');
```

> **Nakon umetanja s izričitim identifikatorima** sekvence identiteta treba pomaknuti, inače prvi sljedeći automatski umetnuti redak pada na sukobu ključa:
>
> ```sql
> SELECT setval(pg_get_serial_sequence('fault_types', 'id'),
>               (SELECT MAX(id) FROM fault_types));
> ```
>
> Ponoviti za svaki šifrarnik. EF Core seed kroz `HasData` to radi sam, pa je napomena bitna samo pri ručnom postavljanju baze.

### 6.2 Lokacije (6)

| Šifra | Naziv | Vrsta | Grad |
|---|---|---|---|
| `UZ-01` | Zgrada Županijske uprave | Upravna zgrada | Varaždin |
| `UZ-02` | Ispostava Ludbreg | Upravna zgrada | Ludbreg |
| `SK-01` | OŠ Ivana Kukuljevića | Škola | Varaždin |
| `SK-02` | Gimnazija Varaždin | Škola | Varaždin |
| `ZD-01` | Dom zdravlja Novi Marof | Zdravstvena ustanova | Novi Marof |
| `SL-01` | Središnje skladište | Skladište | Varaždin |

### 6.3 Korisnici

Lozinka za sve demo račune je `Test123!`, a hash se generira pri seedu, ne upisuje unaprijed.

| E-mail | Ime | Uloge | Matična lokacija |
|---|---|---|---|
| `admin@ekvarovi.hr` | Ana Adminović | Admin | UZ-01 |
| `manager@ekvarovi.hr` | Marko Upravić | Manager | UZ-01 |
| `tehnicar1@ekvarovi.hr` | Ivan Strujić (Elektrotehnika) | Technician | UZ-01 |
| `tehnicar2@ekvarovi.hr` | Petar Vodić (Vodoinstalacije) | Technician | SL-01 |
| `tehnicar3@ekvarovi.hr` | Luka Mrežić (Informatika) | Technician | UZ-01 |
| `prijavitelj1@ekvarovi.hr` | Iva Školjić | Reporter | SK-01 |
| `prijavitelj2@ekvarovi.hr` | Sara Zdravković | Reporter | ZD-01 |
| `voditelj.tehnicar@ekvarovi.hr` | Tomislav Dvojić | **Manager + Technician** | UZ-02 |
| `neaktivan@ekvarovi.hr` | Bivši Djelatnik | Reporter (neaktivan) | SK-02 |

Račun s dvije uloge postoji namjerno — na njemu se pokazuje da izbornik i ovlasti rade kao unija, što je zahtjev iz PRD-a.

### 6.4 Materijali (10)

| Šifra | Naziv | Jedinica | Cijena (EUR) |
|---|---|---|---|
| `MAT-001` | Žarulja LED 10W | Komad | 3,50 |
| `MAT-002` | Osigurač automatski 16A | Komad | 6,20 |
| `MAT-003` | Kabel NYM 3×1,5 | Metar | 1,10 |
| `MAT-004` | Brtva gumena 1/2" | Komad | 0,45 |
| `MAT-005` | Slavina jednoručna | Komad | 28,90 |
| `MAT-006` | Cijev PPR 20 mm | Metar | 2,30 |
| `MAT-007` | Termostatska glava | Komad | 15,40 |
| `MAT-008` | UTP kabel Cat6 | Metar | 0,85 |
| `MAT-009` | Silikon sanitarni | Komad | 4,60 |
| `MAT-010` | Gips-karton ploča | Komad | 9,80 |

### 6.5 Prijave (20)

Prijave se generiraju s datumima raspoređenima kroz zadnjih 90 dana, kako bi filtar razdoblja na dashboardu imao smisla. Raspodjela po statusima:

| Status | Broj | Što je posebno |
|---|---|---|
| Zaprimljeno | 4 | Bez vrste, prioriteta i roka — spremne za kategorizaciju |
| Pregledano | 2 | Kategorizirane, čekaju dodjelu |
| Dodijeljeno | 3 | Imaju aktivnu dodjelu, još bez intervencije |
| U radu | 4 | Jedna ima intervenciju u tijeku, jedna ima neuspješnu pa novu pokrenutu |
| Riješeno | 3 | Uspješna intervencija, čekaju provjeru upravitelja |
| Zatvoreno | 4 | S napomenom provjere i punom poviješću |

Dodatni scenariji ugrađeni u seed, kako bi se svaka netrivijalna funkcionalnost mogla demonstrirati bez ručne pripreme:

- **Re-dodjela** — dvije prijave imaju po dvije dodjele, od kojih je starija neaktivna s upisanim razlogom i vremenom deaktivacije.
- **Neuspješna pa uspješna intervencija** — jedna zatvorena prijava ima tri intervencije: neuspješnu, pa neuspješnu, pa uspješnu.
- **Nalog bez intervencije** — tri prijave u statusu Dodijeljeno pokazuju da dodjela postoji neovisno o intervenciji.
- **Prekoračeni rokovi** — tri otvorene prijave imaju rok u prošlosti, pa metrika kašnjenja na dashboardu nije nula.
- **Kritični prioritet** — dvije prijave, obje s postavljenim rokom, u skladu s pravilom BR-04.
- **Utrošak materijala** — svaka završena intervencija ima dvije do tri stavke materijala, pa profil prijave prikazuje ukupni trošak.
- **Vremenska crta** — za svaku prijavu upisuju se odgovarajući povijesni zapisi, pa bonus prikaz ima sadržaj od prve minute.

Privici se u seedu ne stvaraju, jer bi zahtijevali stvarne datoteke u repozitoriju. Fotografije se dodaju ručno pri demonstraciji.

---

## 7. Napomene o konvencijama

**Imenovanje.** U C#-u su entiteti i svojstva u PascalCase, a u bazi sve u `snake_case`, što prevodi konvencija imenovanja iz paketa `EFCore.NamingConventions`. Zahvaljujući tome identifikatori u PostgreSQL-u nikad ne trebaju navodnike, pa ručno pisani SQL izrazi — poput filtra parcijalnog indeksa — rade bez zamki s velikim i malim slovima.

**Vrijeme.** Svi vremenski stupci su `timestamptz` i sadrže UTC. Npgsql zahtijeva da vrijednosti tipa `DateTime` koje se upisuju imaju `Kind` postavljen na `Utc`, inače baca iznimku. Zato se u cijelom kodu koristi isključivo `DateTime.UtcNow`, a pretvorba u lokalno vrijeme radi se tek pri prikazu u pregledniku.

**Novac.** `numeric(10,2)`, valuta euro. Zaokruživanje se ne radi u bazi nego pri prikazu.

**Tekstualni stupci.** Duljine su namjerno ograničene i odgovaraju validacijama iz poglavlja 8 PRD-a, kako bi baza bila zadnja linija obrane ako validacija u servisu ikad zakaže.

**Migracije.** Jedna migracija po zaokruženoj promjeni modela, s opisnim nazivom. Prva migracija nosi cijelu shemu i seed šifrarnika. Migracije se primjenjuju automatski pri pokretanju API-ja, što je prihvatljivo za projekt ove veličine — u produkcijskom sustavu to bi bio odvojen korak isporuke.
