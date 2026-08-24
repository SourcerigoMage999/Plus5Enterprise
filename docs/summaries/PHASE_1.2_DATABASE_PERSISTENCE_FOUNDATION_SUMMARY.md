# Phase 1.2 — Database/persistence foundation

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-24`

## Cilj faze

Uvesti minimalni SQL Server + EF Core persistence temelj, reproducibilne kontrolirane migracije, siguran connection-string boundary i database-aware readiness bez preuranjenih business tablica.

## Implementirano

- EF Core SQL Server i Design 10.0.11
- scoped `Plus5DbContext` u Infrastructure sloju
- validacija obaveznog encrypted SQL Server connection stringa bez ispisivanja secret vrijednosti
- `TrustServerCertificate=True` ograničen na eksplicitni Development boundary
- prazna `InitialPersistenceFoundation` migracija bez business tablica
- lokalno zaključan `dotnet-ef` 10.0.11 alat
- database readiness koji provjerava konekciju i pending migracije
- SQL Server 2025 Compose service s loopback-only portom i persistent named volumeom
- odvojeni `sa`, `plus5_migrator` i least-privilege `plus5_app` identiteti
- idempotentni SQL init i odvojeni one-shot migration container prije API starta
- API `alpine-extra` runtime s ICU podrškom potrebnom SQL klijentu
- connection-string i persistence configuration testovi
- canonical `PERSISTENCE.md`, konfiguracijska i development dokumentacija

## Namjerno nije implementirano

- business entiteti, tablice, PK/FK/indeksi ili seed podaci iz Phase 2+
- generic repository/unit-of-work wrapper preko EF Corea
- automatsko migriranje u API startupu
- produkcijski SQL topology, backup/restore, RPO/RTO ili production credentials
- API error contract, logging/telemetry, auth ili frontend feature promjene

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `.config/dotnet-tools.json` | added | reproducibilni dotnet-ef 10.0.11 alat |
| `.env.example` | added | lokalni Compose secret contract bez vrijednosti |
| `README.md` | changed | SQL/Compose/migration development workflow |
| `backend/src/Plus5.Api/Dockerfile` | changed | digest-pinnani ICU-capable non-root runtime za SQL klijent |
| `backend/src/Plus5.Api/Plus5.Api.csproj` | changed | službeni EF Core DbContext health-check paket |
| `backend/src/Plus5.Api/Program.cs` | changed | persistence composition i schema-aware readiness |
| `backend/src/Plus5.Api/appsettings.json` | changed | prazni canonical connection-string key bez secreta |
| `backend/src/Plus5.Infrastructure/Plus5.Infrastructure.csproj` | changed | EF Core SQL Server + Design paketi |
| `backend/src/Plus5.Infrastructure/Dockerfile.migrations` | added | odvojeni non-root one-shot migration image |
| `backend/src/Plus5.Infrastructure/Persistence/Plus5DbContext.cs` | added | scoped EF Core context foundation |
| `backend/src/Plus5.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` | added | SQL registration i secure connection validation |
| `backend/src/Plus5.Infrastructure/Persistence/Plus5DbContextDesignTimeFactory.cs` | added | environment-driven design/migration context |
| `backend/src/Plus5.Infrastructure/Persistence/Migrations/20260824104132_InitialPersistenceFoundation.cs` | added | početna prazna migration granica |
| `backend/src/Plus5.Infrastructure/Persistence/Migrations/20260824104132_InitialPersistenceFoundation.Designer.cs` | added | EF migration metadata |
| `backend/src/Plus5.Infrastructure/Persistence/Migrations/Plus5DbContextModelSnapshot.cs` | added | canonical EF model snapshot |
| `backend/tests/Plus5.Api.Tests/Configuration/PersistenceConfigurationTests.cs` | added | connection security i DbContext lifetime testovi |
| `docker-compose.yml` | changed | SQL, init, migration, API i frontend dependency redoslijed |
| `docker/sqlserver/init.sql` | added | idempotentna baza/login/role inicijalizacija |
| `docs/CONFIGURATION.md` | changed | DB secret i migration config contract |
| `docs/PERSISTENCE.md` | added | canonical persistence/migration dokumentacija |
| `docs/ROADMAP.md` | changed | Phase 1.2 označena DONE nakon svih gateova |
| `docs/summaries/PHASE_1.2_DATABASE_PERSISTENCE_FOUNDATION_SUMMARY.md` | added | završni phase handoff i dokaz provjera |

## Domain / database promjene

- Domain model: nema promjene.
- Migracija: `InitialPersistenceFoundation`; prazni `Up`/`Down`, samo EF migration history pri applyju.
- Business schema: nema tablica ni podataka.
- Runtime identitet nema DDL ovlasti; migration identitet je odvojen.

## API promjene

- Nema novih business endpointa.
- `/health/live` ostaje process-only.
- `/health/ready` sada zahtijeva dostupnu bazu i nula pending migracija.
- API fail-fast odbija nedostajući/neispravan/neencrypted connection string i nepouzdani certifikat izvan Developmenta.

## Frontend promjene

- Nema frontend source, route, state ili API-client promjena.
- Compose frontend i dalje počinje tek nakon healthy API-ja.

## Security / authorization

- SQL lozinke/connection stringovi nisu u Gitu ni image layerima.
- Compose zahtijeva necommitani `.env` i tri različita secreta.
- SQL port je bindan samo na `127.0.0.1`.
- `sa` nije API runtime identitet.
- Produkcija ne smije koristiti Development untrusted-certificate override.

## Ovisnosti

- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.11 — službeni Microsoft SQL Server provider, zahtijevan zaključanim ADR-0003.
- `Microsoft.EntityFrameworkCore.Design` 10.0.11 — design-time migration tooling, `PrivateAssets=all`.
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.11 — službeni DbContext readiness probe.
- lokalni `dotnet-ef` 10.0.11 tool manifest — usklađen s runtime/design paketima.
- Nema alternativnog ORM-a, third-party health-check paketa ili nove frontend ovisnosti.

## Testovi dosad

| Naredba / suite | Rezultat |
|---|---|
| `dotnet restore .\Plus5Enterprise.sln` | PASS |
| `dotnet tool restore` | PASS — dotnet-ef 10.0.11 |
| `dotnet build .\Plus5Enterprise.sln --no-restore --configuration Release` | PASS — 0 warnings, 0 errors |
| `dotnet test .\Plus5Enterprise.sln --no-build --configuration Release` | PASS — 29/29 |
| `dotnet ef migrations has-pending-model-changes` | PASS — nema pending model promjena |
| `dotnet ef migrations script --idempotent` | PASS — history table, migration ID i idempotent guard potvrđeni |
| `docker compose config --quiet` uz privremene testne env vrijednosti | PASS |
| `dotnet format --verify-no-changes` | PASS nakon LF/UTF-8 normalizacije EF-generirane migracije |
| `dotnet list .\Plus5Enterprise.sln package --vulnerable --include-transitive` | PASS — nema poznatih ranjivih paketa |
| frontend test + lint + typecheck/build | PASS — 4/4 testa, bez lint/type grešaka, production build uspješan |
| stvarni SQL Server clean migration + drugi idempotent apply | PASS — migration history sadrži `20260824104132_InitialPersistenceFoundation`; drugi apply javlja da je baza ažurna |
| SQL named-volume restart | PASS — baza healthy, migration history očuvan, readiness se vratio na HTTP 200 |
| API/SQL readiness i frontend smoke test | PASS — live 200, ready 200, frontend 200 |
| least-privilege runtime identitet | PASS — reader=1, writer=1, owner=0, create-table=0 |
| non-root container korisnici | PASS — API UID 1654, migrations UID 1654, frontend `nginx` |

## Self-review

- [x] scope nije proširen izvan persistence foundationa
- [x] nema nedokumentiranih business modela
- [x] build i unit/config/architecture testovi prolaze
- [x] migracija i idempotent script su statički pregledani
- [x] connection string je secret i transport encryption je obavezan
- [x] migracije nisu API startup side effect
- [x] stvarni SQL Server clean/restart/idempotent runtime test
- [x] stvarni readiness i runtime permission test
- [x] ROADMAP status `DONE`

## Arhitekturne odluke

- Implementiran postojeći ADR-0003 (SQL Server + EF Core migrations).
- Nema novog ADR-a; lokalni SQL Server 2025 image i odvojeni migration container implementacijski su detalji postojećeg zaključanog baselinea.

## Poznati rizici / tehnički dug

- `2025-latest` tag je dodatno pinnan manifest digestom; kontrolirano osvježavanje imagea ostaje security maintenance zadatak.
- Početna migracija namjerno nema business tablice; constraint/index/delete/concurrency provjere postaju obavezne sa stvarnim modelom u Phase 2+.

## Otvorena pitanja

- Nema product/business pitanja za Phase 1.2.
- Nema otvorenog tehničkog blockera.

## Točna početna točka za nastavak

Nakon owner reviewa i zasebnog commit/push odobrenja započeti Phase 1.3 — API conventions, validation & error contract. Ne uvoditi business entitete ili tablice prije odgovarajućih Phase 2 gateova.
