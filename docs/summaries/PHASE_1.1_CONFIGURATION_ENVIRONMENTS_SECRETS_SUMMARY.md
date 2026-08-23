# Phase 1.1 — Configuration, environments & secrets

## Status

`DONE`

Commit/push gate: `AWAITING OWNER REVIEW`

## Datum

`2026-08-23`

## Cilj faze

Zaključati siguran, environment-driven konfiguracijski temelj za postojeći API, frontend i lokalni Docker Compose, s fail-fast validacijom i jasnom granicom između javne konfiguracije i secreta.

## Implementirano

- canonical `CONFIGURATION.md` za Development, Staging i Production contract, precedence i secret pravila
- API allowlist za podržane ASP.NET Core environmente; nepoznata vrijednost prekida startup
- obavezni eksplicitni ASP.NET Core `AllowedHosts` allowlist bez `*`/`+` wildcarda
- strongly typed `FrontendOptions` za obavezni `Frontend:PublicOrigin`
- startup validator koji dopušta samo apsolutni HTTP(S) origin bez credentialsa, patha, queryja ili fragmenta
- Development vrijednost `http://localhost:5173` i Compose override `http://localhost:8081`
- .NET user-secrets identitet `plus5-enterprise-api`, bez dodavanja stvarnih ili placeholder runtime secreta
- centralni frontend public-config parser za `VITE_API_BASE_URL`
- sigurni relative default `/api/v1`, uz validaciju opcionalnog apsolutnog HTTP(S) URL-a
- tipizirani Vite environment contract i startup učitavanje validacije
- `frontend/.env.example` koji eksplicitno upozorava da su sve `VITE_*` vrijednosti javne
- novi backend API test projekt i frontend Node test suite za pozitivne i negativne konfiguracijske slučajeve
- root README i ROADMAP usklađeni s implementacijom

## Namjerno nije implementirano

- SQL Server, EF Core, DbContext, migracije, connection string ili DB secret; pripada Phase 1.2
- API versioned business endpointi, validation/error envelope ili CORS policy; pripada Phase 1.3 i kasnijim security/auth fazama
- logging/telemetry infrastruktura; pripada Phase 1.4
- frontend API client, routing, app shell ili feature UI; pripada Phase 1.5+
- authentication, role, permissions, signing keyevi ili auth secret contract; Phase 1.6 ostaje blokiran business zahtjevima
- Staging/Production konkretni hostovi, TLS/reverse-proxy topology ili VPS secret store; to ostaje deployment/release gate

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `Plus5Enterprise.sln` | changed | dodan API configuration test projekt |
| `README.md` | changed | development configuration i secret upute |
| `backend/src/Plus5.Api/Plus5.Api.csproj` | changed | lokalni .NET user-secrets identitet |
| `backend/src/Plus5.Api/Program.cs` | changed | registracija validirane konfiguracije prije startupa |
| `backend/src/Plus5.Api/Configuration/ConfigurationExtensions.cs` | added | environment allowlist i options registration |
| `backend/src/Plus5.Api/Configuration/FrontendOptions.cs` | added | strongly typed backend konfiguracija |
| `backend/src/Plus5.Api/Configuration/FrontendOptionsValidator.cs` | added | fail-fast origin validacija |
| `backend/src/Plus5.Api/appsettings.json` | changed | environment-neutral obavezna sekcija bez production vrijednosti |
| `backend/src/Plus5.Api/appsettings.Development.json` | changed | lokalni javni frontend origin |
| `backend/tests/Plus5.Api.Tests/Plus5.Api.Tests.csproj` | added | API configuration test suite |
| `backend/tests/Plus5.Api.Tests/Configuration/ConfigurationValidationTests.cs` | added | 15 environment/host/origin validation testova |
| `docker-compose.yml` | changed | Compose public frontend origin override |
| `frontend/.env.example` | added | javni lokalni config primjer bez secreta |
| `frontend/package.json` | changed | frontend test naredba bez nove dependency ovisnosti |
| `frontend/src/config/publicEnvironment.ts` | added | centralni parser i validator javne konfiguracije |
| `frontend/src/config/environment.ts` | added | Vite config binding boundary |
| `frontend/src/main.tsx` | changed | fail-fast frontend config učitavanje pri startupu |
| `frontend/src/vite-env.d.ts` | added | tipizirani Vite environment key |
| `frontend/tests/publicEnvironment.test.ts` | added | četiri public-config testa |
| `frontend/tsconfig.json` | changed | uključen test TypeScript projekt |
| `frontend/tsconfig.test.json` | added | strict Node test typecheck contract |
| `docs/CONFIGURATION.md` | added | canonical environment/config/secrets dokument |
| `docs/ROADMAP.md` | changed | Phase 1.1 označena `DONE` nakon provjera |
| `docs/summaries/PHASE_1.1_CONFIGURATION_ENVIRONMENTS_SECRETS_SUMMARY.md` | added | completion handoff |

## Domain / database promjene

- Novi entiteti/value objects: nema.
- Promijenjena business pravila: nema.
- Migracije: nema.
- Backfill/data migration: nema.
- Connection string/secrets: nisu uvedeni prije Phase 1.2.

## API promjene

- Nema novih endpointa niti promjene HTTP contracta.
- Postojeći `/health/live` i `/health/ready` ostaju HTTP 200 uz valjanu konfiguraciju.
- API sada fail-fast prekida startup za nepodržan environment, nedostajući/wildcard `AllowedHosts` ili neispravan/nedostajući `Frontend:PublicOrigin`.
- Promjena je operativni startup contract, ne javni business API contract.

## Frontend promjene

- Nema novog routea, ekrana, server statea ni API poziva.
- Definiran je centralni `VITE_API_BASE_URL` boundary s defaultom `/api/v1`.
- Javna konfiguracija validira se pri inicijalizaciji; credentials, protocol-relative URL, ne-HTTP(S) scheme, query i fragment se odbijaju.
- Loading/empty/error UI nije primjenjiv jer app shell i API data flow još nisu uvedeni.

## Security / authorization

- Nema secreta u source konfiguraciji, Composeu, primjerima ili frontend bundle contractu.
- Host filtering više nema bootstrap wildcard; svaki environment mora dati eksplicitni host allowlist.
- `VITE_*` je eksplicitno klasificiran kao javna konfiguracija.
- Backend Development secrets imaju standardni user-secrets boundary; Staging/Production koriste deployment environment/secrets sloj.
- Validacijske poruke ne ispisuju vrijednost potencijalnog secreta.
- Auth/authorization nije nagađan niti uveden prije business gatea.

## Ovisnosti

- Nema nove production NuGet ili npm dependency ovisnosti.
- Novi API test projekt ponovno koristi iste zaključane Microsoft.NET.Test.Sdk, xUnit, runner i coverlet verzije kao postojeći architecture test projekt.
- Frontend testovi koriste ugrađeni Node.js test runner.
- Nije potreban novi ADR; implementacija primjenjuje postojeći security/configuration baseline bez promjene stacka ili topologyja.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `dotnet restore .\Plus5Enterprise.sln` | PASS |
| `dotnet build .\Plus5Enterprise.sln --no-restore --configuration Release` | PASS — 0 warnings, 0 errors |
| `dotnet test .\Plus5Enterprise.sln --no-build --configuration Release` | PASS — 19/19 (15 configuration + 4 architecture) |
| API Production startup bez `Frontend:PublicOrigin` | PASS — očekivani fail-fast `OptionsValidationException` prije posluživanja prometa |
| lokalni Development API `/health/live` i `/health/ready` | PASS — HTTP 200 / `Healthy` |
| `dotnet format .\Plus5Enterprise.sln --verify-no-changes --no-restore` | PASS |
| `dotnet list .\Plus5Enterprise.sln package --vulnerable --include-transitive` | PASS — nema poznatih ranjivih paketa |
| `npm ci` | PASS — 0 vulnerabilities |
| `npm run test` | PASS — 4/4 |
| `npm run lint` | PASS |
| `npm run build` | PASS — strict TypeScript + Vite production build |
| `npm audit --audit-level=high` | PASS — 0 vulnerabilities |
| `docker compose config --quiet` | PASS |
| `docker compose build` | PASS — API i frontend image |
| Docker Compose runtime health | PASS — API live/ready i frontend HTTP 200 |
| container user inspection | PASS — API `1654`, frontend `nginx` |

## Self-review

- [x] scope nije proširen izvan Phase 1.1
- [x] nema nedokumentiranih business pretpostavki
- [x] build prolazi bez warninga
- [x] configuration i architecture testovi prolaze
- [x] frontend test/typecheck/lint/build prolaze
- [x] migracije i persistence nisu uvedeni
- [x] auth/authorization nisu proizvoljno definirani
- [x] nema secreta u sourceu ili frontend public configu
- [x] Docker runtime ostaje non-root i environment-driven
- [x] dokumentacija je usklađena s implementacijom

## Arhitekturne odluke

- Nema novog ADR-a.
- Primijenjeni su postojeći `ARCHITECTURE_BASELINE.md`, `BACKEND_ENGINEERING_STANDARD.md`, `FRONTEND_ENGINEERING_STANDARD.md`, `SECURITY_ENGINEERING_STANDARD.md` i `DOCKER_DEPLOYMENT_STANDARD.md` configuration/secrets zahtjevi.

## Poznati rizici / tehnički dug

- `Frontend:PublicOrigin` priprema siguran server-side origin contract; stvarna restriktivna CORS policy uvodi se tek uz API/security contract, bez preuranjenog ponašanja u ovoj fazi.
- Frontend koristi prenosivi relative `/api/v1`; reverse-proxy/API client routing još ne postoji jer nema business API-ja ni frontend data flowa.
- Konkretne Staging/Production vrijednosti i secret delivery ovise o kasnijem deployment topologyju.
- Windows sandbox tijekom očekivanog startup failurea nije mogao pisati u Event Log; sama options validacija i fail-fast ponašanje izvršeni su prije listenera, a container/Development runtime provjere prolaze.

## Otvorena pitanja

- Nema novih pitanja koja blokiraju Phase 1.2.
- Postojeći auth, material storage, readiness i schedule gateovi ostaju nepromijenjeni u `OPEN_QUESTIONS.md`.

## Točna početna točka za sljedeću fazu

Otvoriti Phase 1.2 Database/persistence foundation. Uvesti SQL Server + EF Core prema ADR-0003 i `DATABASE_DESIGN_STANDARD.md`: DbContext/persistence composition, versioniranu inicijalnu migraciju, environment-driven connection string secret, readiness dependency check i integration/migration provjere sa stvarnim SQL Serverom. Ne uvoditi Student/Group ili druge business tablice prije njihovih Phase 2 podfaza, niti API error contract iz Phase 1.3.
