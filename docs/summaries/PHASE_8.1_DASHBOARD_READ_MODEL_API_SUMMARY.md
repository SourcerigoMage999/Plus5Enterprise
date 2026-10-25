# Phase 8.1 — Dashboard read model/API

## Status

**FINAL LOCKED — odobreno 2026-10-25.**

## Implementirano

- Teacher-only `GET /api/v1/dashboard`;
- owner-scoped današnji Sessioni u `Europe/Zagreb` bez otkazanih termina;
- prvi budući `Scheduled` termin kao sljedeći sat;
- Student/Group, grade, Program, title, location/online, status i participant projekcija;
- nearhivirani Student total i canonical Active/OnHold/Inactive brojevi;
- pošten empty rezultat bez fake podataka;
- application port, EF read query, API response mapping i DI/route registracija.

## Sačuvane odluke

- Teacher identitet dolazi samo iz autentificirane sesije;
- Session bilješke, Student/Guardian kontakti i interni ownership podaci nisu izloženi;
- grupni participant count koristi membership valjan na početku Sessiona;
- nema ukupnog readiness postotka ni izvedenih performance bandova;
- nedavno dodani materijali nisu predstavljeni kao korišteni materijali;
- Lesson Plan, Board, notification, messaging, homework, finance i activity feed podaci nisu
  simulirani prije svojih faza.

## Schema i API

- nova ruta: `GET /api/v1/dashboard`;
- schema/migration: nema;
- write/concurrency/CSRF: N/A — endpoint je read-only.

## Validacija

- backend Release build: PASS — 0 warninga i 0 grešaka;
- targeted query/API/auth: PASS — 3/3;
- targeted .NET format: PASS;
- SQL: N/A — nema schema promjene ni novog provider-specific integriteta;
- architecture: N/A — dependency smjer slijedi postojeći Application port → Infrastructure
  adapter → API mapping obrazac;
- frontend i visual acceptance: N/A — Phase 8.1 nema UI;
- full regression: N/A — targeted phase validation is sufficient;
- dependency audit: N/A — dependency graph unchanged;
- Docker infrastructure validation: N/A — infrastructure unchanged.

Sandboxirani `vstest` nije mogao pokrenuti child `testhost`; isti ciljani filter izvan
sandboxa izvršen je uspješno bez promjene testnog scopea.

## Datoteke

- `backend/src/Plus5.Application/Dashboard/TeacherDashboardContracts.cs`;
- `backend/src/Plus5.Infrastructure/Dashboard/EfTeacherDashboardQuery.cs`;
- `backend/src/Plus5.Api/Dashboard/TeacherDashboardEndpoints.cs`;
- `backend/src/Plus5.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`;
- `backend/src/Plus5.Api/Program.cs`;
- `backend/tests/Plus5.Api.Tests/Dashboard/TeacherDashboardQueryTests.cs`;
- `backend/tests/Plus5.Api.Tests/Dashboard/TeacherDashboardApiTests.cs`;
- `docs/TEACHER_DASHBOARD_READ_MODEL.md`;
- `docs/ROADMAP.md`;
- `docs/summaries/PHASE_8.1_DASHBOARD_READ_MODEL_API_SUMMARY.md`.

## Rizici i otvorena pitanja

- nema blocker otvorenog pitanja za 8.1;
- canonical dashboard sekcije bez postojećeg source modula ostaju eksplicitno odgođene,
  bez zamjenskih ili lažnih metrika.

## Sljedeća točka

Phase 8.2 može izraditi Screen 1.1 nad ovim endpointom, uz canonical PNG i minimalni stvarni
desktop/mobile visual gate. Ne smije aktivirati buduće module ili izmišljati njihove podatke.

## Review handoff

Phase 8.1 je Solution Architect odobrio i označena je kao FINAL LOCKED.

## Post-lock operational correction — 2026-10-26

Prvi stvarni Phase 8.2 browser gate otkrio je da je izvorna EF projekcija prolazila InMemory
testove, ali SQL Server nije mogao prevesti korelirane `Single()` podupite. Response contract,
ownership i business semantika nisu promijenjeni. Projekcija sada prvo čita bounded skup
današnjih/next Session redaka, zatim owner-scoped Group/Student/Grade/Program/Location i
membership podatke u batch upitima te mapira rezultat u memoriji.

Targeted backend build prolazi s 0 warninga/grešaka, dashboard testovi 3/3, a stvarni
autentificirani SQL/API poziv vraća HTTP 200. Phase 8.1 ostaje FINAL LOCKED.
