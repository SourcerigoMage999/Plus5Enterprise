# Phase 1.3 — API conventions, validation & error contract

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-24`

## Cilj faze

Zaključati zajednički, versionirani i sigurni HTTP trust boundary prije prvog business endpointa: server validation, konzistentni machine-readable error odgovori, sigurno globalno exception ponašanje i bounded pagination.

## Implementirano

- canonical `/api/v1` route group i `v1` group name
- built-in .NET 10 Minimal API validation kroz `AddValidation()`
- RFC `ProblemDetails`/`HttpValidationProblemDetails` contract
- stabilni snake_case `code`, `traceId` i path-only `instance` na automatskim problem odgovorima
- default kodovi za validation, 400/401/403/404/405/409/413/415/429/500/503 grane
- globalni `IExceptionHandler` koji ne vraća exception poruku, tip, stack trace ili query string
- source-generated strukturirani error log samo s exception tipom i trace ID-em
- bounded `PaginationQuery` s defaultom 25 i maksimumom 100
- generički `PagedResponse<T>` s provjerenim argumentima i sigurnim izračunom ukupnog broja stranica
- integration/contract testovi s in-memory ASP.NET Core TestServerom
- canonical `API_CONVENTIONS.md` i ADR-0006

## Namjerno nije implementirano

- business endpointi, requesti ili resursi iz Phase 2+
- authentication/authorization contract iz blokirane Phase 1.6
- CORS, rate limiting ili abuse policy bez konkretnog javnog/auth endpointa
- OpenAPI/Swagger paket ili UI
- third-party validation/versioning/error framework
- logging/telemetry enrichment, exporter ili observability backend iz Phase 1.4
- frontend API client/error UI iz Phase 1.5 i feature faza

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `README.md` | changed | repository status i link na canonical API contract |
| `backend/src/Plus5.Api/Program.cs` | changed | registracija i middleware za API conventions |
| `backend/src/Plus5.Api/Contracts/PagedResponse.cs` | added | standardni bounded list response contract |
| `backend/src/Plus5.Api/Contracts/PaginationQuery.cs` | added | validirani page/pageSize query contract |
| `backend/src/Plus5.Api/Conventions/ApiConventionsExtensions.cs` | added | service, middleware i `/api/v1` route-group composition |
| `backend/src/Plus5.Api/Conventions/ApiProblemCodes.cs` | added | canonical default machine error codeovi |
| `backend/src/Plus5.Api/Conventions/ApiProblemDetailsDefaults.cs` | added | centralna ProblemDetails normalizacija |
| `backend/src/Plus5.Api/Conventions/ApiRoutes.cs` | added | version prefix i group-name konstante |
| `backend/src/Plus5.Api/Conventions/GlobalExceptionHandler.cs` | added | sigurni globalni unexpected-exception boundary |
| `backend/tests/Plus5.Api.Tests/Plus5.Api.Tests.csproj` | changed | službeni ASP.NET Core integration-test host |
| `backend/tests/Plus5.Api.Tests/Conventions/ApiConventionsTests.cs` | added | HTTP contract, validation, security i pagination testovi |
| `docs/API_CONVENTIONS.md` | added | canonical API versioning/validation/error/pagination contract |
| `docs/DECISION_LOG.md` | changed | ADR-0006 za trajni API boundary izbor |
| `docs/README.md` | changed | API contract u obaveznom tehničkom čitanju |
| `docs/ROADMAP.md` | changed | Phase 1.3 označena DONE nakon provjera |
| `docs/summaries/PHASE_1.3_API_CONVENTIONS_VALIDATION_ERROR_CONTRACT_SUMMARY.md` | added | završni phase handoff |

## Domain / database promjene

- Novi domain entiteti/value objecti: nema.
- Business pravila: nema promjene.
- EF model/migracije: nema promjene; `has-pending-model-changes` prolazi.
- Backfill/data migration: nema.

## API promjene

- Nema novog business endpointa.
- Budući javni business endpointi koriste `/api/v1` preko `MapVersionOneApi()`.
- Operativni `/health/live` i `/health/ready` ostaju neversionirani.
- Automatski status-code odgovori bez bodyja postaju `application/problem+json` sa stabilnim `code` i `traceId` poljima.
- Validation failure vraća HTTP 400, `code=validation_failed` i `errors` mapu.
- Neočekivani exception vraća HTTP 500, `code=internal_error`, bez internih detalja.
- Ovo je novi foundation contract, ne breaking promjena postojećeg business API-ja jer business endpointi još ne postoje.

## Frontend promjene

- Nema frontend source, route, state ili dependency promjena.
- Postojeći default `/api/v1` base URL sada je usklađen s backend canonical route groupom.
- Feature-specific error/loading UI ostaje fazama koje uvode stvarne API pozive.

## Security / authorization

- Svaki external input ostaje server-side validation boundary.
- 500 response ne sadrži exception poruku/tip/stack trace ni query string.
- `instance` sadrži samo path kako query parametri ili njihove potencijalno osjetljive vrijednosti ne bi bili reflektirani.
- Error log u ovoj fazi sadrži exception type i trace ID, bez request payloada, queryja ili exception poruke.
- Bounded page size sprječava neograničene list requestove.
- Auth/authorization nije proizvoljno uveden prije Phase 1.6 gatea.

## Ovisnosti

- Produkcijski API nema novu package ovisnost; `AddValidation`, `ProblemDetails` i `IExceptionHandler` dolaze iz .NET 10 Web SDK/frameworka.
- `Microsoft.AspNetCore.Mvc.Testing` 10.0.11 dodan je samo test projektu za službeni in-memory ASP.NET Core integration host.
- Nisu uvedeni FluentValidation, Asp.Versioning, Swagger/OpenAPI generator ni custom serialization framework.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `dotnet restore .\Plus5Enterprise.sln` | PASS |
| `dotnet build .\Plus5Enterprise.sln --no-restore --configuration Release` | PASS — 0 warnings, 0 errors |
| `dotnet test .\Plus5Enterprise.sln --no-build --configuration Release` | PASS — API 32/32, architecture 4/4, ukupno 36/36 |
| API conventions integration suite | PASS — `/api/v1`, validation, 404, 405, malformed JSON, safe 500 i pagination |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| `dotnet ef migrations has-pending-model-changes` | PASS — nema pending model promjena |
| `dotnet list ... package --vulnerable --include-transitive` | PASS — nema poznatih ranjivih paketa |
| frontend test | PASS — 4/4 |
| frontend lint + typecheck + production build | PASS |
| `npm audit --audit-level=high` | PASS — 0 ranjivosti |
| izolirani Docker Compose build/start | PASS — database/init/migrations/API/frontend lanac healthy |
| container health smoke test | PASS — live 200, ready 200, frontend 200 |
| stvarni container 404 ProblemDetails | PASS — 404, `not_found`, canonical URN, path-only instance |
| API container user | PASS — UID 1654 |
| izolirani container/network/volume cleanup | PASS |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build prolazi bez upozorenja
- [x] relevantni integration/contract/architecture/frontend testovi prolaze
- [x] migracija nije dodana i EF model consistency je provjeren
- [x] validation i sigurno error ponašanje provjereni su
- [x] auth nije izmišljen prije dokumentacijskog gatea
- [x] dependency audit prolazi
- [x] Docker runtime contract je provjeren
- [x] dokumentacija i ROADMAP su ažurirani

## Arhitekturne odluke

- ADR-0006 — Versionirani Minimal API i ProblemDetails contract (`Accepted`).
- Postojeći ADR-0002 (.NET backend) ostaje nepromijenjen.

## Poznati rizici / tehnički dug

- Default error codeovi pokrivaju cross-cutting HTTP grane; svaki budući business-specific code mora biti zasebno dokumentiran i testiran.
- Page/pageSize odgovara početnim list use caseovima. Cursor pagination nije uveden bez stvarnog query/stability zahtjeva.
- Phase 1.4 mora nadograditi correlation/logging/telemetry bez promjene javnog `traceId` contracta i bez logiranja sensitive podataka.

## Otvorena pitanja

- Nema otvorenog pitanja koje blokira Phase 1.3.
- Auth, permissions, CORS credential model i abuse-prone auth rate limits ostaju Phase 1.6 gateu.

## Točna početna točka za sljedeću fazu

Nakon owner reviewa i zasebnog commit/push odobrenja otvoriti Phase 1.4 — Logging/telemetry foundation. Nadogradnja mora koristiti postojeći `traceId` i globalni exception boundary, definirati strukturirana polja, environment log razine i health/telemetry granice bez dodavanja business endpointa, auth modela ili vanjskog observability servisa bez dokumentirane potrebe.
