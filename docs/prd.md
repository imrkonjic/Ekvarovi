# PRD — e-Kvarovi Županije

| | |
|---|---|
| **Verzija** | 1.0 |
| **Datum** | 9. rujna 2026. |
| **Autor** | Ivan M. |
| **Tip projekta** | Kolegijski projekt, samostalni rad |
| **Rok** | manje od 7 radnih dana |
| **Prateći dokumenti** | [tech.md](tech.md) — tehnička realizacija · [shema.md](shema.md) — baza podataka |

---

## Sadržaj

1. [Sažetak i ciljevi](#1-sažetak-i-ciljevi)
2. [Korisničke uloge i user stories](#2-korisničke-uloge-i-user-stories)
3. [Funkcionalni zahtjevi po modulu](#3-funkcionalni-zahtjevi-po-modulu)
4. [Nefunkcionalni zahtjevi](#4-nefunkcionalni-zahtjevi)
5. [Podatkovni model — opisno](#5-podatkovni-model--opisno)
6. [API — popis endpointa](#6-api--popis-endpointa)
7. [Blazor stranice po ulozi](#7-blazor-stranice-po-ulozi)
8. [Poslovna pravila (validacije)](#8-poslovna-pravila-validacije)
9. [Plan faza implementacije](#9-plan-faza-implementacije)
10. [Bonus: AI podrška](#10-bonus-ai-podrška)

---

## 1. Sažetak i ciljevi

### 1.1 Svrha

e-Kvarovi Županije je web aplikacija za prijavu, dodjelu i obradu kvarova u objektima u vlasništvu županije — upravnim zgradama, školama, zdravstvenim ustanovama i skladištima. Aplikacija zamjenjuje neformalnu komunikaciju (telefon, e-mail, papirnati nalozi) jedinstvenim digitalnim tokom u kojem svaka prijava ima sljedivu povijest: tko ju je prijavio, tko ju je pregledao i kategorizirao, kome je dodijeljena, koje su intervencije provedene, koji je materijal utrošen i tko je prijavu na kraju zatvorio.

### 1.2 Poslovni ciljevi

1. **Jedinstvena ulazna točka** za sve prijave kvarova sa svih lokacija županije.
2. **Sljedivost i odgovornost** — svaka promjena statusa, prioriteta i dodjele bilježi se s korisnikom i vremenom, uključujući neuspjele pokušaje popravka.
3. **Kontrola rokova** — svaka prijava dobiva rok izveden iz prioriteta, a prekoračenja su upravitelju vidljiva na jednom mjestu.
4. **Dokazivost izvedenog rada** — fotografija stanja prije i poslije, bilješka intervencije i evidencija utrošenog materijala s troškom u eurima.

### 1.3 Akademski ciljevi

Demonstrirati punu vertikalu jedne poslovne aplikacije: relacijski model (DBML dijagram → EF Core migracije nad PostgreSQL-om), REST API s JWT autorizacijom po ulogama, poslovna pravila validirana na poslužitelju, te Blazor WebAssembly klijent s poslužiteljskim filtriranjem, sortiranjem i straničenjem.

### 1.4 Mjerila uspjeha (Definition of Done)

| # | Kriterij |
|---|---|
| 1 | Sve četiri uloge se mogu prijaviti i vide točno one ekrane i podatke koje smiju |
| 2 | Prijava prolazi cijeli tijek Zaprimljeno → Zatvoreno, uključujući scenarij re-dodjele i scenarij neuspješne pa uspješne intervencije |
| 3 | Svako poslovno pravilo iz poglavlja 8 vraća ispravan HTTP status i hrvatsku poruku kad se prekrši, provjereno izravno kroz Swagger, a ne samo kroz UI |
| 4 | Popis prijava i popis intervencija filtriraju, sortiraju i straniče na poslužitelju, s funkcionalnim resetom filtera |
| 5 | Fotografije prije/poslije i PDF dokumenti se uploadaju, preuzimaju i brišu uz provjeru ovlasti |
| 6 | Dashboard prikazuje pet definiranih metrika s filtrom razdoblja |
| 7 | Aplikacija se pokreće s `docker compose up` uz seed podatke i README |

### 1.5 Izvan opsega u verziji 1

Notifikacije (e-mail, SMS, push), pozadinski poslovi i planirano održavanje, automatizirani testovi, refresh tokeni i tok zaboravljene lozinke, višejezičnost, izvoz u Excel ili PDF, skladišno poslovanje sa stanjem zaliha, native mobilna aplikacija.

### 1.6 Tehnološki stack

| Sloj | Odabir |
|---|---|
| Backend | ASP.NET Core Web API, .NET 9 — projekt `EKvarovi.Api` |
| Frontend | Blazor WebAssembly (standalone) + MudBlazor — projekt `EKvarovi.App` |
| Dijeljeni ugovori | Razredna biblioteka s DTO-ima i enumima — projekt `EKvarovi.Shared` |
| ORM | Entity Framework Core 9 s Npgsql pružateljem |
| Baza | PostgreSQL 16 |
| Autentikacija | JWT Bearer, vlastite tablice korisnika i uloga, BCrypt |
| Spremište datoteka | Lokalni disk iza `IFileStorage` apstrakcije |
| Isporuka | Docker Compose lokalno, Render u oblaku |

---

## 2. Korisničke uloge i user stories

### 2.1 Uloge

| Uloga | Opis | Opseg podataka |
|---|---|---|
| **Admin** | Tehnički i sadržajni administrator | Sve, bez ograničenja |
| **Manager** | Upravitelj tehničke službe županije | Sve prijave, nalozi i intervencije |
| **Technician** | Izvršitelj, majstor ili serviser | Samo prijave na kojima ima ili je imao dodjelu |
| **Reporter** | Djelatnik na lokaciji | Samo prijave koje je sam podnio |

Jedan korisnički račun smije nositi bilo koju kombinaciju uloga, primjerice Manager i Technician istovremeno. Ovlasti se zbrajaju — korisnik vidi uniju ekrana i podataka svih svojih uloga.

### 2.2 User stories

#### Reporter

| ID | Priča |
|---|---|
| US-R1 | Kao djelatnik želim prijaviti kvar unosom naslova, opisa i lokacije, uz mogućnost prilaganja fotografije, kako bi tehnička služba znala što je pokvareno. Moja matična lokacija je unaprijed odabrana, ali smijem odabrati i drugu. |
| US-R2 | Kao djelatnik želim vidjeti popis svojih prijava s trenutnim statusom, kako bih znao je li se netko primio posla. |
| US-R3 | Kao djelatnik želim urediti ili obrisati vlastitu prijavu dok je još u statusu Zaprimljeno, kako bih ispravio pogrešku pri unosu. |
| US-R4 | Kao djelatnik želim na profilu prijave vidjeti tko je zadužen i što je napravljeno, uključujući fotografiju završnog stanja. |

#### Manager

| ID | Priča |
|---|---|
| US-M1 | Kao upravitelj želim vidjeti sve prijave s filtrima po lokaciji, vrsti, prioritetu, statusu i razdoblju, kako bih odredio prioritete rada. |
| US-M2 | Kao upravitelj želim prijavi odrediti vrstu kvara, prioritet i rok, pri čemu mi se rok automatski predlaže na temelju prioriteta. |
| US-M3 | Kao upravitelj želim prijavu dodijeliti izvršitelju, čime nastaje radni nalog. |
| US-M4 | Kao upravitelj želim prijavu re-dodijeliti drugom izvršitelju uz obavezan razlog, pri čemu prethodna dodjela ostaje trajno zapisana u povijesti. |
| US-M5 | Kao upravitelj želim nakon uspješne intervencije provjeriti rad i zatvoriti prijavu uz napomenu provjere. |
| US-M6 | Kao upravitelj želim riješenu prijavu vratiti u rad ako provjera nije zadovoljila, uz obavezno obrazloženje. |
| US-M7 | Kao upravitelj želim na dashboardu vidjeti raspodjelu po statusu i prioritetu, koliko prijava kasni, koje lokacije najviše opterećuju službu i koje vrste kvarova prevladavaju. |

#### Technician

| ID | Priča |
|---|---|
| US-T1 | Kao izvršitelj želim vidjeti popis svojih aktivnih naloga sortiran po hitnosti, kako bih znao što raditi sljedeće. |
| US-T2 | Kao izvršitelj želim pokrenuti intervenciju na svom nalogu, čime se bilježi vrijeme početka, a prijava prelazi u status U radu. |
| US-T3 | Kao izvršitelj želim evidentirati utrošeni materijal s količinom, kako bi trošak bio vidljiv. |
| US-T4 | Kao izvršitelj želim dodati fotografiju završnog stanja i završiti intervenciju kao uspješnu ili neuspješnu, uz obaveznu bilješku. |
| US-T5 | Kao izvršitelj želim nakon neuspješne intervencije otvoriti novu na istoj prijavi, jer nalog i dalje stoji na meni. |
| US-T6 | Kao izvršitelj želim vidjeti povijest svojih završenih naloga, kako bih se mogao pozvati na ranije radove. |

#### Admin

| ID | Priča |
|---|---|
| US-A1 | Kao administrator želim upravljati korisnicima, njihovim ulogama i matičnim lokacijama. |
| US-A2 | Kao administrator želim deaktivirati izvršitelja koji odlazi, pri čemu me sustav prisiljava da njegove aktivne naloge prvo prebacim na zamjenika. |
| US-A3 | Kao administrator želim upravljati lokacijama, katalogom materijala i šifrarnicima. |

---

## 3. Funkcionalni zahtjevi po modulu

### 3.1 Autentikacija i korisnici

| ID | Zahtjev |
|---|---|
| FR-AUTH-01 | Prijava e-mailom i lozinkom; uspješna prijava vraća JWT i podatke o korisniku (ime, uloge, matična lokacija) |
| FR-AUTH-02 | Lozinke se pohranjuju kao BCrypt hash s faktorom rada 11; izvorna lozinka se nigdje ne bilježi |
| FR-AUTH-03 | JWT vrijedi 8 sati; nakon isteka klijent preusmjerava na prijavu, bez refresh tokena |
| FR-AUTH-04 | Neaktivan ili logički obrisan korisnik ne može se prijaviti |
| FR-AUTH-05 | Admin kreira, uređuje, aktivira i deaktivira korisnike te im dodjeljuje jednu ili više uloga |
| FR-AUTH-06 | Admin resetira lozinku korisniku na novu vrijednost, bez e-mail toka |
| FR-AUTH-07 | Deaktivacija izvršitelja s aktivnim nalozima blokirana je dok se nalozi ne prebace na zamjenika |

### 3.2 Lokacije

| ID | Zahtjev |
|---|---|
| FR-LOC-01 | Puni CRUD nad lokacijama za Admina; Manager ima pravo čitanja |
| FR-LOC-02 | Lokacija ima šifru, naziv, adresu, grad, poštanski broj, vrstu lokacije, kontakt osobu i telefon |
| FR-LOC-03 | Popis lokacija podržava tekstualnu pretragu, filtar po vrsti i po aktivnosti, sortiranje i straničenje |
| FR-LOC-04 | Brisanje lokacije je logičko; lokacija s postojećim prijavama ne može se obrisati, samo deaktivirati |
| FR-LOC-05 | Deaktivirana lokacija ne pojavljuje se u padajućem izborniku pri kreiranju nove prijave |

### 3.3 Prijave kvarova

| ID | Zahtjev |
|---|---|
| FR-FR-01 | Reporter kreira prijavu s naslovom, opisom i lokacijom (matična predodabrana), uz opcionalne fotografije stanja prije |
| FR-FR-02 | Sustav pri kreiranju dodjeljuje jedinstveni broj prijave u formatu `KV-{godina}-{6 znamenki}` i status Zaprimljeno |
| FR-FR-03 | Vrsta kvara, prioritet i rok ostaju prazni do pregleda; popunjava ih Manager |
| FR-FR-04 | Pri odabiru prioriteta klijent predlaže rok kao trenutno vrijeme uvećano za `DefaultResolutionHours` tog prioriteta, a Manager ga smije pregaziti |
| FR-FR-05 | Popis prijava filtrira se po slobodnom tekstu (broj prijave, naslov, opis), lokaciji, vrsti, prioritetu, statusu, izvršitelju i rasponu datuma prijave, uz poseban prekidač za prijave koje kasne |
| FR-FR-06 | Popis nudi reset svih filtera u jednoj akciji i vraćanje na zadano sortiranje |
| FR-FR-07 | Profil prijave prikazuje sve podatke, aktivnu i povijesne dodjele, sve intervencije s materijalom, sve privitke i vremensku crtu |
| FR-FR-08 | Reporter uređuje i logički briše samo vlastitu prijavu i samo u statusu Zaprimljeno |
| FR-FR-09 | Prijava koja ima ijednu intervenciju ne briše se ni logički ni fizički |
| FR-FR-10 | `GET /api/faultreports/mine` vraća prijave prijavljenog korisnika, s identitetom isključivo iz JWT-a |

### 3.4 Radni nalozi

| ID | Zahtjev |
|---|---|
| FR-ASG-01 | Manager i Admin dodjeljuju prijavu izvršitelju, čime nastaje radni nalog s oznakom aktivnosti |
| FR-ASG-02 | Dodjela u jednoj transakciji prevodi prijavu iz Zaprimljeno preko Pregledano u Dodijeljeno |
| FR-ASG-03 | U svakom trenutku postoji najviše jedna aktivna dodjela po prijavi, osigurano parcijalnim jedinstvenim indeksom u bazi |
| FR-ASG-04 | Re-dodjela deaktivira prethodnu dodjelu i upisuje vrijeme deaktivacije, zatim stvara novu uz obavezan razlog |
| FR-ASG-05 | Radni nalog može postojati bez ijedne intervencije i tijekom vremena imati više intervencija |
| FR-ASG-06 | Masovni prijenos: Admin jednim pozivom prebacuje sve aktivne naloge jednog izvršitelja na zamjenika, s razlogom „Deaktivacija izvršitelja” |
| FR-ASG-07 | `GET /api/workassignments/mine` vraća naloge prijavljenog izvršitelja s identitetom iz JWT-a, uz izbor aktivnih ili svih |

### 3.5 Intervencije

| ID | Zahtjev |
|---|---|
| FR-INT-01 | Izvršitelj pokreće intervenciju na svom aktivnom nalogu; bilježi se vrijeme početka, status U tijeku, a prijava prelazi u U radu |
| FR-INT-02 | Po prijavi smije postojati najviše jedna intervencija u statusu U tijeku |
| FR-INT-03 | Izvršitelj tijekom intervencije uređuje bilješku, evidentira materijal i dodaje fotografije završnog stanja |
| FR-INT-04 | Završetak intervencije zahtijeva ishod (uspješna ili neuspješna), vrijeme završetka i bilješku |
| FR-INT-05 | Uspješna intervencija prevodi prijavu u Riješeno i bilježi vrijeme rješavanja |
| FR-INT-06 | Neuspješna intervencija ostaje u povijesti; prijava ostaje U radu, dodjela ostaje aktivna, moguće je otvoriti novu intervenciju |
| FR-INT-07 | Popis intervencija filtrira se po slobodnom tekstu (broj prijave, bilješka), statusu, izvršitelju i rasponu datuma, uz sortiranje, straničenje i reset |
| FR-INT-08 | Intervencija se ne briše, a završena se više ne smije uređivati |

### 3.6 Materijali

| ID | Zahtjev |
|---|---|
| FR-MAT-01 | CRUD nad katalogom materijala za Admina: šifra, naziv, mjerna jedinica, jedinična cijena u eurima |
| FR-MAT-02 | Izvršitelj dodaje stavku materijala na svoju intervenciju u tijeku odabirom materijala i unosom količine |
| FR-MAT-03 | Pri dodavanju stavke bilježi se snimka jedinične cijene, pa naknadna promjena cjenika ne mijenja povijesne troškove |
| FR-MAT-04 | Isti materijal ne smije se dva puta pojaviti na istoj intervenciji; umjesto toga mijenja se količina |
| FR-MAT-05 | Intervencija prikazuje ukupni trošak materijala, a profil prijave zbroj troškova svih intervencija |
| FR-MAT-06 | Materijal korišten na intervenciji ne briše se, samo deaktivira |

### 3.7 Privici

| ID | Zahtjev |
|---|---|
| FR-ATT-01 | Tri namjene privitka: fotografija prije (na prijavi), fotografija poslije (na intervenciji), dokument (na prijavi) |
| FR-ATT-02 | Slike jpg, jpeg, png i webp do 5 MB; dokumenti pdf do 10 MB |
| FR-ATT-03 | Validacija u tri koraka: ekstenzija, MIME tip iz zaglavlja i potpis datoteke |
| FR-ATT-04 | Najviše 5 fotografija prije po prijavi, 5 fotografija poslije po intervenciji i 5 dokumenata po prijavi |
| FR-ATT-05 | Datoteke se čuvaju izvan `wwwroot`, a preuzimaju isključivo kroz autorizirani endpoint |
| FR-ATT-06 | Privitak logički briše onaj tko ga je učitao, Manager ili Admin; fizička datoteka ostaje na disku |

### 3.8 Šifrarnici

| ID | Zahtjev |
|---|---|
| FR-LKP-01 | Svaki šifrarnik dostupan je kroz vlastiti GET endpoint i vraća `LookupDto` |
| FR-LKP-02 | Zbirni endpoint vraća sve šifrarnike odjednom, radi jednog poziva pri pokretanju klijenta |
| FR-LKP-03 | Klijent šifrarnike dohvaća jednom po sesiji i drži ih u memoriji |
| FR-LKP-04 | Admin smije dodavati stavke, mijenjati nazive i deaktivirati ih, ali ne i fizički brisati |

### 3.9 Dashboard

| ID | Zahtjev |
|---|---|
| FR-DSH-01 | Metrika 1 — broj prijava po statusu |
| FR-DSH-02 | Metrika 2 — broj otvorenih prijava po prioritetu, gdje je otvoreno sve osim statusa Zatvoreno |
| FR-DSH-03 | Metrika 3 — broj prijava koje kasne, odnosno imaju rok u prošlosti a nisu Riješeno ni Zatvoreno |
| FR-DSH-04 | Metrika 4 — pet lokacija s najviše prijava u odabranom razdoblju |
| FR-DSH-05 | Metrika 5 — raspodjela prijava po vrsti kvara |
| FR-DSH-06 | Filtar razdoblja: 7 dana, 30 dana, godina ili prilagođeni raspon; zadano je 30 dana |
| FR-DSH-07 | Podaci se učitavaju pri ulasku na stranicu i osvježavaju gumbom; nema automatskog osvježavanja ni push kanala |
| FR-DSH-08 | Opseg podataka ovisi o ulozi — Admin i Manager vide sve, Technician svoje naloge, Reporter svoje prijave |

---

## 4. Nefunkcionalni zahtjevi

### 4.1 Sigurnost

- Sav pristup osim prijave zahtijeva valjani JWT; nedostatak tokena vraća 401, nedovoljne ovlasti 403.
- Identitet za sve `/mine` scenarije i za sve provjere vlasništva čita se isključivo iz `sub` claima. Identifikator korisnika poslan u tijelu zahtjeva ili u query stringu se ignorira.
- Autorizacija se provodi na razini API-ja neovisno o tome što UI prikazuje; skrivanje gumba nije sigurnosna mjera.
- Lozinke: BCrypt s faktorom rada 11, minimalna duljina 8 znakova.
- CORS dopušta samo poznatu adresu Blazor klijenta.
- Upload odbija datoteke po ekstenziji, MIME tipu i potpisu. Naziv datoteke na disku je GUID, a izvorni naziv čuva se samo u bazi.

### 4.2 Performanse

- Sve liste straniče se na poslužitelju; API nikad ne vraća neograničen skup podataka.
- Ciljani odziv liste do 500 ms, dashboarda do 1 s na razvojnom računalu sa seed podacima.
- Indeksi na svim stranim ključevima te na statusu, roku i datumu prijave.
- Upiti nad listama koriste projekciju u DTO bez učitavanja cijelih entiteta.

### 4.3 Pouzdanost i podaci

- Operacije koje mijenjaju više entiteta — dodjela, re-dodjela, završetak intervencije i masovni prijenos naloga — izvode se u jednoj transakciji.
- Brisanje je logičko na svim glavnim entitetima, uz globalni filtar upita u EF Core.
- Vremenske oznake se u bazi čuvaju kao `timestamptz` u UTC-u, a u UI-u prikazuju u lokalnom vremenu u formatu `dd.MM.yyyy HH:mm`.

### 4.4 Upotrebljivost

- Cijeli UI je na hrvatskom jeziku, uključujući poruke o greškama. Kod, nazivi entiteta i API rute su na engleskom.
- Sučelje je responzivno; forma za prijavu kvara i ekran izvršitelja upotrebljivi su na širini od 360 piksela.
- Svaka radnja koja mijenja podatke daje povratnu informaciju, a razorne radnje traže potvrdu.

### 4.5 Održivost

- Slojevita struktura: kontroleri bez poslovne logike, servisi s poslovnim pravilima, EF Core kontekst za pristup podacima.
- Pristup datotečnom sustavu apstrahiran je sučeljem, pa je zamjena spremišta lokalna promjena.
- Format greške je ujednačen: `{ "message": "...", "errors": { "polje": ["poruka"] } }`, uz odgovarajući HTTP status.

---

## 5. Podatkovni model — opisno

> Potpuni DBML dijagram, popis stupaca s tipovima, indeksi i SQL DDL nalaze se u [shema.md](shema.md).

### 5.1 Pregled

Model ima 17 tablica: 7 šifrarnika i 10 tablica jezgre.

**Šifrarnici:** `LocationType`, `FaultType`, `FaultPriority`, `FaultStatus`, `InterventionStatus`, `MaterialUnit`, `Role`

**Jezgra:** `User`, `UserRole`, `Location`, `FaultReport`, `WorkAssignment`, `Intervention`, `Material`, `InterventionMaterial`, `Attachment`, `FaultReportHistory`

### 5.2 Zajednička polja

Glavni entiteti (`User`, `Location`, `FaultReport`, `WorkAssignment`, `Intervention`, `Material`, `Attachment`) nose polja za praćenje nastanka i izmjene te logičkog brisanja: `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId`, `IsDeleted`, `DeletedAt`. Šifrarnici umjesto toga nose samo `IsActive` i `SortOrder`.

### 5.3 Šifrarnici i njihov sadržaj

| Šifrarnik | Vrijednosti | Dodatna polja |
|---|---|---|
| LocationType | Upravna zgrada, Škola, Zdravstvena ustanova, Skladište | — |
| FaultType | Elektrika, Voda, Grijanje, Mreža, Građevinski radovi, Ostalo | — |
| FaultPriority | Nizak, Srednji, Visok, Kritičan | `DefaultResolutionHours` (168, 72, 24, 4), `ColorHex` |
| FaultStatus | Zaprimljeno, Pregledano, Dodijeljeno, U radu, Riješeno, Zatvoreno | `IsClosedState` |
| InterventionStatus | Planirana, U tijeku, Završena, Neuspješna | — |
| MaterialUnit | Komad, Metar, Litra, Kilogram, Paket | `Abbreviation` (kom, m, l, kg, pak) |
| Role | Admin, Manager, Technician, Reporter | `Code` (koristi se u JWT claimovima) |

Svi šifrarnici seedaju se fiksnim identifikatorima kako bi se na njih moglo pouzdano referencirati iz koda i seed podataka.

### 5.4 Ključni entiteti

**User** — jedinstveni e-mail, hash lozinke, ime, prezime, telefon, specijalizacija (relevantna za izvršitelje), matična lokacija i oznaka aktivnosti. Nema zasebnih tablica za Reportera i Technicijana; uloga određuje ponašanje, a veze prema prijavama i nalozima idu izravno na korisnika. Time nema dvostruke evidencije ni potrebe za sinkronizacijom.

**UserRole** — spojna tablica sa složenim primarnim ključem korisnika i uloge, uz vrijeme dodjele. Omogućuje višestruke uloge po računu.

**Location** — šifra, naziv, adresa, grad, poštanski broj, vrsta lokacije, kontakt osoba i telefon, oznaka aktivnosti.

**FaultReport** — središnji entitet. Uz broj prijave, naslov i opis nosi obaveznu lokaciju i status te opcionalnu vrstu, prioritet i rok. Bilježi tko je i kada prijavio, tko je i kada pregledao, kada je riješeno te tko je, kada i uz koju napomenu zatvorio. Oznaka kašnjenja se ne sprema nego računa u letu, kao rok u prošlosti uz status koji nije Riješeno ni Zatvoreno.

**WorkAssignment** — povijest dodjela. Nosi prijavu, izvršitelja, tko je i kada dodijelio, oznaku aktivnosti, vrijeme deaktivacije, razlog re-dodjele i napomenu. Ključno ograničenje je parcijalni jedinstveni indeks koji dopušta najviše jednu aktivnu dodjelu po prijavi. Dodjela postoji neovisno o intervencijama i tijekom vremena ih može imati nula, jednu ili više.

**Intervention** — veže se na radni nalog, a radi lakšeg filtriranja i provjere pravila o najviše jednoj intervenciji u tijeku nosi i izravnu vezu na prijavu te snimku izvršitelja. Uz status bilježi vrijeme početka i završetka, bilješku i razlog neuspjeha.

**Material** i **InterventionMaterial** — katalog materijala s mjernom jedinicom i cijenom u eurima, te stavke utroška s količinom i snimkom jedinične cijene u trenutku evidentiranja. Isti materijal ne smije se pojaviti dvaput na istoj intervenciji.

**Attachment** — jedna tablica za sve tri namjene, s poljem namjene i dvjema opcionalnim vezama. Fotografija prije i dokument vežu se na prijavu, fotografija poslije na intervenciju; to je osigurano uvjetom u bazi i provjerom u servisu. Uz metapodatke o datoteci bilježi se tko je i kada učitao.

**FaultReportHistory** — vremenska crta. Za svaku promjenu bilježi vrstu promjene, staru i novu vrijednost, napomenu, korisnika i vrijeme. Zapisi se stvaraju u istoj transakciji kao i sama promjena i nikad se ne mijenjaju ni brišu.

### 5.5 Kardinalnosti

```
LocationType      1 ──< Location 1 ──< FaultReport
User (podnositelj) 1 ──< FaultReport
FaultType         1 ──< FaultReport      (opcionalno)
FaultPriority     1 ──< FaultReport      (opcionalno)
FaultStatus       1 ──< FaultReport
FaultReport       1 ──< WorkAssignment   (najviše jedan aktivan)
User (izvršitelj) 1 ──< WorkAssignment
WorkAssignment    1 ──< Intervention     (0..n tijekom vremena)
FaultReport       1 ──< Intervention     (denormalizirano)
Intervention      1 ──< InterventionMaterial >── 1 Material
Material          n ──> 1 MaterialUnit
FaultReport       1 ──< Attachment       (fotografija prije, dokument)
Intervention      1 ──< Attachment       (fotografija poslije)
FaultReport       1 ──< FaultReportHistory
User              1 ──< UserRole >── 1 Role
```

### 5.6 Ponašanje pri brisanju

Svi strani ključevi postavljeni su na zabranu kaskadnog brisanja, osim veze korisnika i uloge koja kaskadno prati korisnika. U praksi se briše logički, uz globalni filtar upita. Povijesni zapisi — dodjele, intervencije i vremenska crta — nikad se ne uklanjaju.

---

## 6. API — popis endpointa

**Osnovica:** `/api` · **Autorizacija:** zaglavlje `Authorization: Bearer <jwt>` na svemu osim prijave

### 6.1 Autentikacija

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| POST | `/auth/login` | anonimno | `LoginDto` | `AuthResponseDto` |
| GET | `/auth/me` | bilo koja | — | `CurrentUserDto` |

- `LoginDto` — e-mail, lozinka
- `AuthResponseDto` — token, vrijeme isteka, `CurrentUserDto`
- `CurrentUserDto` — id, ime, prezime, e-mail, popis šifri uloga, matična lokacija

### 6.2 Korisnici

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| GET | `/users` | Admin | `search`, `roleId`, `locationId`, `isActive`, straničenje, sortiranje | `PagedResult<UserListDto>` |
| GET | `/users/{id}` | Admin | — | `UserDetailDto` |
| POST | `/users` | Admin | `UserSaveDto` | `UserDetailDto` |
| PUT | `/users/{id}` | Admin | `UserSaveDto` | `UserDetailDto` |
| PUT | `/users/{id}/password` | Admin | `ResetPasswordDto` | 204 |
| PUT | `/users/{id}/activate` | Admin | — | 204 |
| PUT | `/users/{id}/deactivate` | Admin | — | 204 ili **409** `ActiveAssignmentsConflictDto` |
| POST | `/users/{id}/transfer-assignments` | Admin | `TransferAssignmentsDto` | `TransferResultDto` |
| GET | `/users/technicians` | Admin, Manager | `search` | `List<LookupDto>` |

- `UserSaveDto` — ime, prezime, e-mail, telefon, lozinka (samo pri kreiranju), specijalizacija, matična lokacija, popis identifikatora uloga
- `ActiveAssignmentsConflictDto` — poruka i popis aktivnih naloga (broj prijave, naslov, prioritet); UI iz toga gradi dijalog za odabir zamjenika
- `TransferAssignmentsDto` — zamjenski izvršitelj, opcionalna napomena
- `TransferResultDto` — broj prebačenih naloga i broj prekinutih intervencija

### 6.3 Lokacije

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| GET | `/locations` | sve uloge | `search`, `locationTypeId`, `isActive`, straničenje, sortiranje | `PagedResult<LocationListDto>` |
| GET | `/locations/{id}` | sve uloge | — | `LocationDetailDto` |
| POST | `/locations` | Admin | `LocationSaveDto` | `LocationDetailDto` |
| PUT | `/locations/{id}` | Admin | `LocationSaveDto` | `LocationDetailDto` |
| DELETE | `/locations/{id}` | Admin | — | 204 ili 409 |

### 6.4 Prijave kvarova

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| GET | `/faultreports` | Admin, Manager | `FaultReportFilterDto` | `PagedResult<FaultReportListDto>` |
| GET | `/faultreports/mine` | sve uloge | isti filtri, identitet iz JWT-a | `PagedResult<FaultReportListDto>` |
| GET | `/faultreports/{id}` | prema pravilu vidljivosti | — | `FaultReportDetailDto` |
| GET | `/faultreports/{id}/timeline` | prema pravilu vidljivosti | — | `List<TimelineEntryDto>` |
| GET | `/faultreports/{id}/assignments` | prema pravilu vidljivosti | — | `List<AssignmentListDto>` |
| POST | `/faultreports` | Reporter, Manager, Admin | `FaultReportCreateDto` | `FaultReportDetailDto` |
| PUT | `/faultreports/{id}` | vlasnik u statusu Zaprimljeno, Manager, Admin | `FaultReportUpdateDto` | `FaultReportDetailDto` |
| PUT | `/faultreports/{id}/triage` | Manager, Admin | `FaultReportTriageDto` | `FaultReportDetailDto` |
| PUT | `/faultreports/{id}/close` | Manager, Admin | `CloseFaultReportDto` | `FaultReportDetailDto` |
| PUT | `/faultreports/{id}/reopen` | Manager, Admin | `ReopenFaultReportDto` | `FaultReportDetailDto` |
| DELETE | `/faultreports/{id}` | vlasnik u statusu Zaprimljeno, Admin | — | 204 ili 409 |

- `FaultReportFilterDto` — `search`, `locationId`, `faultTypeId`, `faultPriorityId`, `faultStatusId`, `technicianUserId`, `reportedFrom`, `reportedTo`, `onlyOverdue`, `page`, `pageSize`, `sortBy`, `sortDir`
- `FaultReportListDto` — id, broj prijave, naslov, lokacija, vrsta, prioritet s bojom, status, rok, oznaka kašnjenja, izvršitelj na aktivnoj dodjeli, datum prijave
- `FaultReportDetailDto` — sva polja, aktivna dodjela i povijest dodjela, intervencije sa sažetkom, privici po namjeni, ukupni trošak materijala
- `FaultReportCreateDto` — naslov, opis, lokacija
- `FaultReportTriageDto` — vrsta, prioritet, rok
- `CloseFaultReportDto` — napomena provjere (obavezno)
- `ReopenFaultReportDto` — obrazloženje (obavezno)

### 6.5 Radni nalozi

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| GET | `/workassignments` | Admin, Manager | `search`, `technicianUserId`, `isActive`, `locationId`, raspon datuma, straničenje | `PagedResult<AssignmentListDto>` |
| GET | `/workassignments/mine` | Technician | `onlyActive` (zadano `true`), ostali filtri; identitet iz JWT-a | `PagedResult<AssignmentListDto>` |
| GET | `/workassignments/{id}` | Admin, Manager, vlasnik naloga | — | `AssignmentDetailDto` |
| POST | `/workassignments` | Manager, Admin | `AssignmentSaveDto` | `AssignmentDetailDto` |
| PUT | `/workassignments/{id}/reassign` | Manager, Admin | `ReassignDto` | `AssignmentDetailDto` (nova aktivna dodjela) |

- `AssignmentSaveDto` — prijava, izvršitelj, napomena
- `ReassignDto` — novi izvršitelj, razlog (obavezno), napomena
- `AssignmentListDto` — id, broj i naslov prijave, lokacija, prioritet, izvršitelj, vrijeme dodjele, oznaka aktivnosti, vrijeme deaktivacije, razlog, broj intervencija, status prijave, rok, oznaka kašnjenja

### 6.6 Intervencije

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| GET | `/interventions` | Admin, Manager | `InterventionFilterDto` | `PagedResult<InterventionListDto>` |
| GET | `/interventions/mine` | Technician | isti filtri, identitet iz JWT-a | `PagedResult<InterventionListDto>` |
| GET | `/interventions/{id}` | prema vidljivosti | — | `InterventionDetailDto` |
| POST | `/interventions` | Technician (vlasnik naloga), Admin | `InterventionCreateDto` | `InterventionDetailDto` |
| PUT | `/interventions/{id}` | vlasnik, dok nije završena | `InterventionUpdateDto` | `InterventionDetailDto` |
| PUT | `/interventions/{id}/finish` | vlasnik, Admin | `InterventionFinishDto` | `InterventionDetailDto` |

- `InterventionFilterDto` — `search` (broj prijave ili bilješka), `interventionStatusId`, `technicianUserId`, `startedFrom`, `startedTo`, straničenje, sortiranje
- `InterventionCreateDto` — radni nalog, početna bilješka
- `InterventionFinishDto` — ishod, vrijeme završetka, bilješka (obavezno), razlog neuspjeha (obavezno kad nije uspješna)
- `InterventionDetailDto` — sva polja, podaci o prijavi i izvršitelju, stavke materijala, fotografije poslije, ukupni trošak

### 6.7 Materijali

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| GET | `/materials` | sve uloge | `search`, `materialUnitId`, `isActive`, straničenje | `PagedResult<MaterialListDto>` |
| GET | `/materials/{id}` | sve uloge | — | `MaterialDetailDto` |
| POST | `/materials` | Admin | `MaterialSaveDto` | `MaterialDetailDto` |
| PUT | `/materials/{id}` | Admin | `MaterialSaveDto` | `MaterialDetailDto` |
| DELETE | `/materials/{id}` | Admin | — | 204 ili 409 |
| GET | `/interventions/{id}/materials` | prema vidljivosti | — | `List<InterventionMaterialDto>` |
| POST | `/interventions/{id}/materials` | vlasnik intervencije, Admin | `InterventionMaterialSaveDto` | `InterventionMaterialDto` |
| PUT | `/interventions/{id}/materials/{itemId}` | vlasnik, Admin | `InterventionMaterialSaveDto` | `InterventionMaterialDto` |
| DELETE | `/interventions/{id}/materials/{itemId}` | vlasnik, Admin | — | 204 |

### 6.8 Privici

| Metoda | Ruta | Uloga | Ulaz | Izlaz |
|---|---|---|---|---|
| POST | `/faultreports/{id}/attachments` | Reporter (vlasnik), Manager, Admin | `multipart/form-data`: datoteka i namjena (prije ili dokument) | `AttachmentDto` |
| POST | `/interventions/{id}/attachments` | vlasnik intervencije, Admin | `multipart/form-data`: datoteka (fotografija poslije) | `AttachmentDto` |
| GET | `/attachments/{id}` | prema vidljivosti nadređene prijave | — | tok datoteke s izvornim nazivom |
| DELETE | `/attachments/{id}` | onaj tko je učitao, Manager, Admin | — | 204 |

`AttachmentDto` — id, izvorni naziv, namjena, MIME tip, veličina, URL za preuzimanje, tko je i kada učitao.

### 6.9 Šifrarnici i dashboard

| Metoda | Ruta | Uloga | Izlaz |
|---|---|---|---|
| GET | `/lookups/all` | sve uloge | `AllLookupsDto` |
| GET | `/lookups/location-types` | sve uloge | `List<LookupDto>` |
| GET | `/lookups/fault-types` | sve uloge | `List<LookupDto>` |
| GET | `/lookups/fault-priorities` | sve uloge | `List<PriorityLookupDto>` |
| GET | `/lookups/fault-statuses` | sve uloge | `List<LookupDto>` |
| GET | `/lookups/intervention-statuses` | sve uloge | `List<LookupDto>` |
| GET | `/lookups/material-units` | sve uloge | `List<LookupDto>` |
| GET | `/lookups/roles` | Admin | `List<LookupDto>` |
| GET | `/dashboard` | sve uloge, opseg po ulozi | `DashboardDto` |

- `LookupDto` — id, naziv, oznaka aktivnosti, redoslijed
- `PriorityLookupDto` — `LookupDto` uvećan za zadani broj sati i boju
- `DashboardDto` — razdoblje, ukupan broj prijava, broj koje kasne, raspodjela po statusu, otvorene po prioritetu, pet vodećih lokacija, raspodjela po vrsti kvara, oznaka opsega (globalni ili osobni)

### 6.10 Konvencija HTTP statusa

| Status | Značenje |
|---|---|
| 200 | Uspjeh s tijelom |
| 201 | Zapis kreiran |
| 204 | Uspjeh bez tijela |
| 400 | Neispravan ulaz ili prekršeno poslovno pravilo |
| 401 | Token nedostaje ili je istekao |
| 403 | Nedovoljne ovlasti ili tuđi zapis |
| 404 | Zapis ne postoji ili nije vidljiv |
| 409 | Sukob stanja (aktivni nalozi, postojeće veze, nepromjenjiv zapis) |
| 413 | Datoteka prevelika |
| 415 | Nedopušten tip datoteke |

---

## 7. Blazor stranice po ulozi

| Ruta | Stranica | Tko vidi | Funkcije |
|---|---|---|---|
| `/` | **Home** | svi | Dashboard prilagođen ulozi: Admin i Manager vide pet metrika s filtrom razdoblja, Technician kartice svojih aktivnih naloga, Reporter sažetak svojih prijava; brze poveznice na najčešće radnje |
| `/login` | **Login** | anonimno | Prijava, pohrana tokena, preusmjeravanje na početnu |
| `/locations` | **Locations** | Admin uređuje, Manager čita | Tablica s pretragom, filtrom vrste i aktivnosti, sortiranjem i straničenjem; dijalog za unos i uređivanje; deaktivacija |
| `/fault-reports` | **FaultReports** | Admin, Manager | Popis svih prijava sa svim filtrima, resetom, sortiranjem i straničenjem; značke prioriteta i statusa u boji; oznaka kašnjenja; brze radnje |
| `/fault-reports/new` | **FaultReportCreate** | Reporter, Manager, Admin | Forma za novu prijavu s predodabranom matičnom lokacijom i uploadom fotografija prije; prilagođena za mobitel |
| `/fault-reports/{id}/edit` | **FaultReportEdit** | vlasnik u statusu Zaprimljeno, Manager, Admin | Uređivanje osnovnih podataka; Manager dodatno vidi vrstu, prioritet i rok s automatskim prijedlogom roka |
| `/fault-reports/{id}` | **FaultReportProfile** | prema pravilu vidljivosti | Središnji ekran: zaglavlje s brojem prijave i statusom, aktivna dodjela i povijest dodjela, intervencije s materijalom i troškom, galerija fotografija prije i poslije, dokumenti, vremenska crta te kontekstualne radnje ovisno o ulozi i statusu |
| `/assignments` | **Assignments** | Admin, Manager | Popis svih radnih naloga s filtrima po izvršitelju, aktivnosti i razdoblju; dijalozi za dodjelu i re-dodjelu s obaveznim razlogom |
| `/my-assignments` | **MyAssignments** | Technician | Vlastiti nalozi, prekidač aktivni/svi, sortiranje po hitnosti, pokretanje intervencije |
| `/interventions` | **Interventions** | Admin i Manager sve, Technician svoje | Popis intervencija s filtrima po tekstu, statusu, izvršitelju i razdoblju |
| `/interventions/{id}` | **InterventionDetail** | vlasnik, Manager, Admin | Bilješka, evidencija materijala, upload fotografija poslije, završetak intervencije s oba ishoda |
| `/materials` | **Materials** | Admin uređuje, ostali čitaju | Katalog materijala s pretragom, mjernom jedinicom i cijenom u eurima |
| `/my-reports` | **MyReports** | svi, ponajviše Reporter | Vlastite prijave sa statusom i napretkom |
| `/users` | **Users** | Admin | Popis korisnika s filtrima, dodjela više uloga, reset lozinke i deaktivacija s dijalogom za odabir zamjenskog izvršitelja |

**Zajedničke komponente:** izbornik koji se gradi iz uloga u tokenu, komponenta filtera s gumbom za reset, tablica sa straničenjem, značke prioriteta i statusa, galerija privitaka, dijalog potvrde, obavijesti i presretač HTTP zahtjeva koji dodaje token te na 401 odjavljuje korisnika.

---

## 8. Poslovna pravila (validacije)

Svako pravilo provjerava se u servisnom sloju API-ja, neovisno o tome što UI dopušta.

### 8.1 Prijave

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-01 | Prijava mora biti vezana uz aktivnu, neizbrisanu lokaciju | 400 |
| BR-02 | Naslov 5–200 znakova, opis 10–2000 znakova | 400 |
| BR-03 | Broj prijave generira poslužitelj; klijentska vrijednost se ignorira | — |
| BR-04 | Prijava s prioritetom Kritičan mora imati rok | 400 |
| BR-05 | Rok ne smije biti u prošlosti u trenutku postavljanja | 400 |
| BR-06 | Reporter uređuje i briše samo vlastitu prijavu i samo u statusu Zaprimljeno | 403 |
| BR-07 | Prijava koja ima ijednu intervenciju ne briše se ni logički ni fizički | 409 |
| BR-08 | Vrstu, prioritet i rok mijenjaju samo Manager i Admin, u svakom statusu osim Zatvoreno, uz zapis u povijest | 403 / 400 |

### 8.2 Statusni tijek

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-09 | Dopušteni prijelazi naprijed su Zaprimljeno → Pregledano → Dodijeljeno → U radu → Riješeno → Zatvoreno; svaki drugi se odbija | 400 |
| BR-10 | Dodjela u jednoj transakciji prevodi prijavu do statusa Dodijeljeno i to je jedini dopušteni preskok | — |
| BR-11 | Jedini prijelaz unatrag je Riješeno → U radu, izvode ga Manager i Admin uz obavezno obrazloženje | 400 / 403 |
| BR-12 | Prijava u statusu Zatvoreno je nepromjenjiva | 409 |
| BR-13 | Prijava se ne smije zatvoriti bez barem jedne intervencije sa statusom Završena | 409 |
| BR-14 | Zatvaranje zahtijeva napomenu provjere od najmanje 5 znakova | 400 |

### 8.3 Dodjele

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-15 | Po prijavi smije postojati najviše jedna aktivna dodjela | 409 |
| BR-16 | Dodijeliti se smije samo aktivnom korisniku s ulogom Technician | 400 |
| BR-17 | Re-dodjela zahtijeva razlog od najmanje 5 znakova; prva dodjela ga ne traži | 400 |
| BR-18 | Re-dodjela na izvršitelja koji već drži aktivnu dodjelu te prijave se odbija | 400 |
| BR-19 | Prethodna dodjela se pri re-dodjeli deaktivira i dobiva vrijeme deaktivacije, ali se nikad ne briše | — |
| BR-20 | Dodjela nije moguća na prijavi bez određene vrste i prioriteta | 400 |
| BR-21 | Deaktivacija izvršitelja s aktivnim nalozima vraća 409 s popisom naloga; prolazi tek nakon uspješnog prijenosa na zamjenika | 409 |
| BR-22 | Pri masovnom prijenosu svaka intervencija u tijeku automatski se zatvara kao neuspješna s bilješkom „Prekinuto — deaktivacija izvršitelja” | — |
| BR-23 | Zamjenski izvršitelj mora biti aktivan, imati ulogu Technician i biti različit od korisnika kojeg se deaktivira | 400 |

### 8.4 Intervencije

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-24 | Intervenciju smije pokrenuti samo izvršitelj s aktivne dodjele ili Admin | 403 |
| BR-25 | Po prijavi smije postojati najviše jedna intervencija u statusu U tijeku | 409 |
| BR-26 | Pokretanje intervencije bilježi vrijeme početka i prevodi prijavu u status U radu | — |
| BR-27 | Vrijeme završetka ne smije biti prije vremena početka ni u budućnosti | 400 |
| BR-28 | Završena intervencija mora imati vrijeme početka, vrijeme završetka i bilješku od najmanje 10 znakova | 400 |
| BR-29 | Neuspješna intervencija dodatno traži razlog neuspjeha | 400 |
| BR-30 | Uspješna intervencija prevodi prijavu u Riješeno i bilježi vrijeme rješavanja | — |
| BR-31 | Neuspješna intervencija ostavlja prijavu u statusu U radu, a dodjelu aktivnom | — |
| BR-32 | Završena intervencija se više ne smije uređivati ni brisati | 409 |
| BR-33 | Izvršitelj mijenja samo intervencije na vlastitoj aktivnoj dodjeli | 403 |

### 8.5 Materijali

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-34 | Količina mora biti strogo veća od nule i najviše 99999,99 | 400 |
| BR-35 | Materijal se dodaje samo na intervenciju koja nije završena | 409 |
| BR-36 | Isti materijal ne smije se pojaviti dvaput na istoj intervenciji | 409 |
| BR-37 | Dodati se smije samo aktivan materijal iz kataloga | 400 |
| BR-38 | Jedinična cijena snima se u trenutku dodavanja i naknadno se ne mijenja | — |
| BR-39 | Materijal upotrijebljen na intervenciji ne briše se, samo deaktivira | 409 |

### 8.6 Privici

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-40 | Slike do 5 MB (jpg, jpeg, png, webp), PDF do 10 MB | 413 / 415 |
| BR-41 | Provjeravaju se ekstenzija, MIME tip i potpis datoteke; neslaganje bilo kojeg odbija upload | 415 |
| BR-42 | Najviše 5 fotografija prije po prijavi, 5 fotografija poslije po intervenciji, 5 dokumenata po prijavi | 400 |
| BR-43 | Fotografija poslije veže se isključivo uz intervenciju, a fotografija prije i dokument isključivo uz prijavu | 400 |
| BR-44 | Preuzimanje privitka dopušteno je samo onome tko smije vidjeti nadređenu prijavu | 403 |
| BR-45 | Privitak briše onaj tko ga je učitao, Manager ili Admin; brisanje je logičko | 403 |

### 8.7 Autorizacija i vidljivost

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-46 | Poziv bez valjanog tokena vraća 401, a s valjanim tokenom uz nedovoljnu ulogu 403 | 401 / 403 |
| BR-47 | Reporter vidi isključivo prijave kojima je podnositelj | 403 / 404 |
| BR-48 | Technician vidi isključivo prijave na kojima ima ili je imao dodjelu, uključujući vlastite zatvorene naloge | 403 / 404 |
| BR-49 | Technician ne smije mijenjati prioritet, dodjeljivati, re-dodjeljivati ni zatvarati prijave | 403 |
| BR-50 | Identitet u `/mine` endpointima uzima se iz `sub` claima; parametar s identifikatorom korisnika se ignorira | — |
| BR-51 | Korisnik s više uloga ima uniju ovlasti svojih uloga | — |
| BR-52 | Neaktivan korisnik ne može se prijaviti niti koristiti već izdani token | 401 |

### 8.8 Liste

| ID | Pravilo | Odgovor |
|---|---|---|
| BR-53 | `pageSize` je ograničen na najviše 100, a nedopuštena vrijednost `sortBy` vraća se na zadanu | 400 |
| BR-54 | Zadano sortiranje prijava je datum prijave silazno, a intervencija vrijeme početka silazno | — |
| BR-55 | Raspon datuma kojem je kraj prije početka se odbija | 400 |

---

## 9. Plan faza implementacije

Sedam radnih dana, samostalni rad. Faze 0 do 7 su obavezne, faze 8 do 10 su bonus i smiju otpasti bez posljedica na obavezni opseg.

### Faza 0 — Priprema (2 h)

Struktura rješenja s projektima `EKvarovi.Api`, `EKvarovi.App` i `EKvarovi.Shared`. Docker Compose s PostgreSQL servisom te imenovanim volumenima za bazu i za uploade. Osnovni README.

### Faza 1 — Model podataka (dan 1, ~6 h)

1. **Prvo DBML dijagram** — sve tablice, veze i indeksi, s izričito označenim parcijalnim jedinstvenim indeksom nad aktivnom dodjelom. Dijagram je ulaz u sve ostalo i ujedno se predaje kao dokumentacija.
2. Entiteti u C#-u prema dijagramu, sa zajedničkom baznom klasom za polja praćenja.
3. `DbContext` s Fluent API konfiguracijama, globalnim filtrima logičkog brisanja, parcijalnim indeksom i uvjetom nad privicima.
4. Seed podaci prema poglavlju o seedu u [shema.md](shema.md).
5. Prva migracija i provjera baze.

> Označeni rizik. Ako parcijalni indeks zadaje probleme, provjeri odgovaraju li nazivi stupaca u izrazu filtra stvarnim nazivima u bazi.

### Faza 2 — Autentikacija (dan 2, ~4 h)

Konfiguracija JWT-a, servis prijave s BCryptom, generiranje tokena s više role claimova, politike autorizacije, pomoćni servis za čitanje identiteta iz konteksta i Swagger s podrškom za Bearer token. Provjera: prijava za svaku ulogu, `/auth/me` i poziv bez tokena koji mora vratiti 401.

> Označeni rizik. Naziv role claima mora odgovarati onome što atribut autorizacije očekuje; detalji su u [tech.md](tech.md).

### Faza 3 — Šifrarnici, lokacije i materijali (dan 2–3, ~4 h)

Endpointi šifrarnika i zbirni dohvat. Puni CRUD za lokacije i materijale s poslužiteljskim filtriranjem i straničenjem. Ovdje nastaje generički obrazac za filtriranje, sortiranje i straničenje koji se dalje koristi svugdje.

### Faza 4 — Prijave (dan 3, ~6 h)

Kreiranje s generiranjem broja prijave, kategorizacija, popis s punim filtrima, detalj, `/mine`, uređivanje i logičko brisanje. Pravila BR-01 do BR-08 i filtri vidljivosti po ulozi.

### Faza 5 — Dodjele i intervencije (dan 4, ~7 h)

Najzahtjevniji dio. Dodjela s automatskim prijelazom statusa, re-dodjela u transakciji, `/mine` za naloge, pokretanje i završetak intervencije s oba ishoda, evidencija materijala, zatvaranje i vraćanje prijave u rad. Pravila BR-09 do BR-39.

### Faza 6 — Privici i dashboard (dan 5, ~5 h)

Implementacija spremišta datoteka nad lokalnim diskom, upload s trostrukom validacijom, autorizirano preuzimanje i logičko brisanje. Dashboard s pet metrika, filtrom razdoblja i opsegom po ulozi.

### Faza 7 — Blazor klijent (dan 5–6, ~10 h)

Postav MudBlazora, presretač s tokenom i odjavom na 401, pohrana tokena i izbornik prema ulogama. Redoslijed izrade ekrana: Login, zatim MyAssignments i MyReports kao najkraći put do demonstracije `/mine` scenarija, pa FaultReports, FaultReportProfile, Assignments, Interventions, Materials i Locations, Users te na kraju Home s dashboardom.

### Faza 8 — Docker, Render i dokumentacija (dan 6–7, ~4 h)

Dockerfile za API i klijent, Compose s bazom i volumenima, objava na Render s trajnim diskom, README s uputama, popisom demo korisnika i opisom poslovnih pravila.

> **Prva stavka koja pada ako kasniš.** Lokalno pokretanje uz README dovoljno je za obranu.

### Faza 9 — Bonus: vremenska crta (dan 7, ~2 h)

Upisi u povijest iz servisa na svakoj promjeni (pravila BR-08, BR-11, BR-19, BR-26, BR-30), endpoint vremenske crte i komponenta na profilu prijave.

### Faza 10 — Bonus: mobilna prilagodba i AI (dan 7, ~4 h)

Responzivne točke preloma na formi prijave i ekranu izvršitelja, kartični prikaz umjesto tablice na uskim zaslonima, atribut za snimanje fotografije mobitelom. Zatim sučelje `IAiService` i lažna implementacija prema poglavlju 10.

### Kontrolne točke

| Trenutak | Što mora biti gotovo |
|---|---|
| Kraj dana 2 | Prijava radi za sve uloge, baza je popunjena seed podacima |
| Kraj dana 4 | Cijeli tijek prijave prolazi kroz Swagger, od kreiranja do zatvaranja |
| Kraj dana 6 | UI pokriva sve obavezne ekrane |

Ako kraj dana 4 nije ispunjen, izbaci faze 8 do 10 i sav preostali čas uloži u UI.

---

## 10. Bonus: AI podrška

**Opseg:** samo lažna implementacija, bez vanjskog davatelja usluge, bez API ključeva, offline i deterministička. Sučelje je postavljeno tako da bi stvarna implementacija bila zamjena jedne registracije u kontejneru ovisnosti.

### 10.1 Sučelje

```csharp
public interface IAiService
{
    Task<AiSuggestionDto> SuggestClassificationAsync(
        AiSuggestionRequestDto request, CancellationToken ct = default);

    Task<AiSummaryDto> SummarizeWorkAssignmentAsync(
        int faultReportId, CancellationToken ct = default);
}
```

- `AiSuggestionRequestDto` — naslov, opis, opcionalno naziv lokacije
- `AiSuggestionDto` — predloženi naslov, predložena vrsta, predloženi prioritet, kratko obrazloženje na hrvatskom, pouzdanost od 0 do 1 i oznaka da je izvor lažna implementacija
- `AiSummaryDto` — tekst sažetka i vrijeme generiranja

### 10.2 Ponašanje lažne implementacije

**Prijedlog klasifikacije** radi po ključnim riječima u opisu:

| Ključne riječi | Predložena vrsta |
|---|---|
| struja, osigurač, rasvjeta, utičnica | Elektrika |
| voda, curenje, slavina, odvod | Voda |
| radijator, kotao, hladno, grijanje | Grijanje |
| internet, mreža, wifi, preklopnik | Mreža |
| zid, strop, prozor, vrata, žbuka | Građevinski radovi |
| ništa od navedenog | Ostalo |

Prioritet se podiže na Kritičan kod riječi poput poplava, dim, iskrenje, kratki spoj ili curenje plina, na Visok kod izraza nema struje, nema grijanja, ne radi ili hitno, a inače ostaje Srednji. Naslov se predlaže kao skraćeni opis do 60 znakova s velikim početnim slovom.

**Sažetak radnog naloga** sastavlja se iz stvarnih podataka po predlošku: broj prijave i naslov, lokacija i vrsta, prioritet i rok, tko je zadužen i od kada, broj intervencija s ishodima, ukupno utrošeno vrijeme, popis materijala s ukupnim troškom te trenutni status.

### 10.3 Endpointi

| Metoda | Ruta | Uloga | Izlaz |
|---|---|---|---|
| POST | `/ai/suggest-classification` | Reporter, Manager, Admin | `AiSuggestionDto` |
| GET | `/ai/summary/{faultReportId}` | Manager, Admin, dodijeljeni izvršitelj | `AiSummaryDto` |

### 10.4 Pravila koja se ne smiju prekršiti

| ID | Pravilo |
|---|---|
| BR-56 | AI endpointi su isključivo čitajući i ne mijenjaju nijedan zapis u bazi |
| BR-57 | Prijedlog se u UI-u prikazuje kao neobvezujuća ploča s vrijednostima i obrazloženjem; tek klik na „Primijeni prijedlog” popunjava polja forme, a korisnik ih i nakon toga smije mijenjati prije spremanja |
| BR-58 | Nijedan zahtjev za spremanje ne smije se poslati automatski kao posljedica AI prijedloga |
| BR-59 | UI jasno označava da je riječ o prijedlogu pomoćnika i da odgovornost za točnost snosi korisnik |
