# Phase 1.4 — Logging/telemetry foundation

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-24`

## Cilj faze

Uvesti siguran, container-friendly i vendor-neutral observability temelj prije business endpointa: strukturirane logove, stabilnu request korelaciju te standardne traces/metrics primitive bez preuranjenog vanjskog servisa.

## Implementirano

- JSON console logger s UTC timestampom, scopeovima te W3C trace/span poljima
- `X-Trace-Id` response header povezan s `ProblemDetails.traceId`, logom i server traceom
- source-generated request completion event `2000` s metodom, route predloškom, statusom, trajanjem i trace ID-em
- zaštita od logiranja concrete patha, queryja, headera, bodyja i authorization vrijednosti
- OpenTelemetry ASP.NET Core trace/metric i .NET runtime metric instrumentacija
- resource identitet za service name/version/instance i deployment environment
- parent-based ratio sampling: `0.1` default i `1.0` u Developmentu
- opcionalni OTLP exporter samo uz valjani endpoint; HTTPS obavezan izvan Developmenta
- uklanjanje full URL/path/query/user-agent trace tagova prije exporta
- potiskivanje uspješnog `/health/live` completion loga i server tracea
- startup validation i testovi konfiguracije, korelacije, structured polja i sanitizacije
- canonical `OBSERVABILITY.md` i ADR-0007

## Namjerno nije implementirano

- business endpointi, domenski događaji ili analytics
- authentication/authorization, user enrichment ili audit trail
- collector, dashboard, alerting, retention ili vendor observability servis
- OpenTelemetry log exporter; container logovi ostaju JSON stdout
- exporter credentials u sourceu ili endpoint URI-ju
- request/response body, query, header, cookie, PII ili secret logging
- frontend telemetry SDK

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `README.md` | changed | status i link na canonical observability contract |
| `backend/src/Plus5.Api/Plus5.Api.csproj` | changed | službeni OpenTelemetry hosting, instrumentation i OTLP paketi |
| `backend/src/Plus5.Api/Program.cs` | changed | observability service i middleware composition |
| `backend/src/Plus5.Api/appsettings.json` | changed | production-safe sampling i disabled-export defaulti |
| `backend/src/Plus5.Api/appsettings.Development.json` | changed | puni lokalni trace sampling |
| `backend/src/Plus5.Api/Conventions/ApiProblemDetailsDefaults.cs` | changed | canonical W3C trace ID helper |
| `backend/src/Plus5.Api/Conventions/GlobalExceptionHandler.cs` | changed | isti canonical trace ID u sigurnom error logu |
| `backend/src/Plus5.Api/Observability/*` | added | options, validation, JSON logging, tracing, metrics, sanitizacija i request middleware |
| `backend/src/Plus5.Api/Properties/AssemblyInfo.cs` | added | internal observability unit test pristup |
| `backend/tests/Plus5.Api.Tests/Observability/*` | added | options, correlation, privacy i logging contract testovi |
| `docs/OBSERVABILITY.md` | added | canonical logging/telemetry contract |
| `docs/CONFIGURATION.md` | changed | observability configuration contract |
| `docs/DECISION_LOG.md` | changed | ADR-0007 |
| `docs/README.md` | changed | observability u obaveznom tehničkom čitanju |
| `docs/ROADMAP.md` | changed | Phase 1.4 status nakon svih provjera |
| `docs/summaries/PHASE_1.4_LOGGING_TELEMETRY_FOUNDATION_SUMMARY.md` | added | završni phase handoff |

## Domain / database promjene

- Novi entiteti/value objecti: nema.
- Business pravila: nema promjene.
- EF model/migracije: nema promjene; model consistency provjera prolazi.
- Backfill/data migration: nema.

## API promjene

- Svaki API odgovor dobiva `X-Trace-Id`; valjani dolazni W3C `traceparent` nastavlja isti trace.
- `ProblemDetails.traceId` sada koristi isti W3C ID kada observability pipeline postoji.
- Nema novog business endpointa ni promjene postojećih health status contracta.
- Promjena je additive za budući API; header nije authentication niti idempotency token.

## Frontend promjene

- Nema frontend source, dependency, route, state ili UI promjena.
- Frontend kasnije može prikazati/priložiti trace ID u support flowu, ali takav UX nije izmišljen u ovoj fazi.

## Security / authorization

- Logovi koriste route predložak ili `unmatched`, nikad concrete path/query.
- Query, authorization marker i user-agent/full URL telemetry tagovi pokriveni su testovima sanitizacije i stvarnim container log pregledom.
- Startup validation ne reflektira neispravnu endpoint vrijednost.
- OTLP endpoint ne smije sadržavati credentials/query/fragment; HTTPS je obavezan izvan Developmenta.
- PII, payload, cookie, token, connection string i exception message enrichment ostaju zabranjeni.
- Auth/authorization nije uveden prije Phase 1.6 dokumentacijskog gatea.

## Ovisnosti

- `OpenTelemetry.Extensions.Hosting` 1.18.0
- `OpenTelemetry.Instrumentation.AspNetCore` 1.18.0
- `OpenTelemetry.Instrumentation.Runtime` 1.18.0
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.18.0
- Nema vendor-specific SDK-a ili observability backend containera.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `dotnet restore .\Plus5Enterprise.sln` | PASS |
| `dotnet build .\Plus5Enterprise.sln --configuration Release --no-restore` | PASS — 0 warnings, 0 errors |
| `dotnet test .\Plus5Enterprise.sln --configuration Release --no-restore` | PASS — API 50/50, architecture 4/4, ukupno 54/54 |
| observability integration/unit suite | PASS — options, W3C propagation, header/log korelacija, route template, health suppression i tag sanitizacija |
| `dotnet format .\Plus5Enterprise.sln --verify-no-changes --no-restore` | PASS |
| EF pending-model provjera | PASS — nema pending model promjena |
| NuGet vulnerable audit | PASS — nema poznatih ranjivih paketa |
| `npm ci` / audit | PASS — 0 ranjivosti |
| frontend lint + typecheck + test + production build | PASS — 4/4 testa |
| izolirani Docker Compose build/start | PASS — database/init/migrations/API/frontend healthy |
| container HTTP smoke | PASS — live 200, ready 200, frontend 200, unmatched 404 |
| stvarni container JSON log contract | PASS — event 2000, `unmatched`, propagated trace ID, bez query/token markera, bez live completion buke |
| API/frontend container user | PASS — API UID 1654, frontend `nginx` |
| izolirani container/network/volume cleanup | PASS; locally built imageovi ostavljeni |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build prolazi bez upozorenja
- [x] relevantni unit/integration/architecture/frontend testovi prolaze
- [x] migracija nije dodana i EF model consistency je provjeren
- [x] log/trace privacy boundary je testiran
- [x] auth ili user identity enrichment nije izmišljen
- [x] dependency auditi prolaze
- [x] stvarni Docker JSON output i non-root runtime su provjereni
- [x] dokumentacija i ROADMAP su ažurirani

## Arhitekturne odluke

- ADR-0007 — JSON stdout i vendor-neutral OpenTelemetry temelj (`Accepted`).
- ADR-0006 W3C-compatible `traceId` error contract ostaje usklađen.

## Poznati rizici / tehnički dug

- Collector, production endpoint/auth, retention, alerting, dashboardi i SLO-ovi ovise o budućoj deployment topologiji i stvarnim operativnim zahtjevima.
- Default sampling ratio je siguran početni izbor, ali mora se podesiti prema prometu, trošku i incident potrebama prije produkcije.
- Metrics exporter nije aktivan bez OTLP endpointa; health endpointi ostaju trenutni neposredni operational signal.
- Business/user enrichment nije dopušten dok auth, privacy i cardinality pravila nisu zaključana.

## Otvorena pitanja

- Nema otvorenog pitanja koje blokira Phase 1.4 ili Phase 1.5.
- Odabir produkcijskog telemetry backenda i retention/SLO politika ostaje release/deployment odluci.

## Točna početna točka za sljedeću fazu

Nakon owner reviewa i zasebnog commit/push odobrenja otvoriti Phase 1.5 — Frontend app shell, routing & design tokens. Koristiti postojeći frontend public-config boundary i standarde, bez uvođenja auth ekrana ili nedokumentiranih feature flowova; Phase 1.6 ostaje blokirana dok se ne definira auth/business contract.
