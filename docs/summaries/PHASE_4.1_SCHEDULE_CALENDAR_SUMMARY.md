# Phase 4.1 — Screen 3.1 Calendar

## Status i datum

**DONE — implementacija i svi interni gateovi završeni, 2026-09-12.** Spremno za završni
SA review; nije samostalno proglašeno SA-approved/LOCKED. Phase 4.2 nije započeta.

## Cilj i rezultat

Implementiran je prvi stvarni Raspored ekran nad postojećim canonical `Session` modelom.
Teacher dobiva owner-scoped tjedni ili dnevni prikaz, URL navigaciju datumom, Danas,
mini-kalendar, Group/Program/Location filtre, sljedeća dva podsjetnika i precizan sažetak.
Cancelled, cross-owner i zapisi izvan raspona ne ulaze u rezultat.

Calendar nije nova baza rasporeda: read model projicira postojeće Sessione, Group/Student,
Program, Location i aktivna članstva. Backend lokalne granice datuma računa u
`Europe/Zagreb`; frontend UTC timestampove prikazuje u istoj canonical zoni.

Umjesto dvosmislenog broja učenika contract razlikuje `Jedinstvenih učenika` i
`Planiranih dolazaka`. Slobodna mjesta računaju se za svaki vidljivi grupni termin prema
capacityju i aktivnim članstvima na početku Sessiona.

## API i sigurnost

Dodani Teacher-only `GET /api/v1/schedule` zahtijeva uključivi `from`, isključivi `to` i
maksimalno 31 dan. Opcionalni Group, Program i Location GUID filteri ne mijenjaju server-side
ownership. Nevaljan raspon/prazan GUID daje kontrolirani 400, anonimni poziv 401.

Response koristi eksplicitne DTO-e i ne izlaže EF entitete, Teacher ID, bilješke ili tajne.
SQL se prvo ograničava ownerom, vremenom i statusom; nema N+1 upita. Nema write endpointa,
CSRF promjene, nove migracije, entiteta ili dependencyja.

## Frontend i visual acceptance

`/schedule` sada prikazuje canonical naslov/podnaslov, Week/Day/Danas toolbar, centralni
time-grid, programsku legendu i desne mini-calendar/filter/summary/reminder kartice. Loading,
error/retry i empty stanje su eksplicitni. Tjedni grid na mobitelu ostaje lokalno pomičan,
dok dokument nema horizontalni overflow.

Novi termin, Session detail, detaljni izvještaj i notification centar vizualno su prisutni
zbog canonical hijerarhije, ali su stvarno disabled s faznim objašnjenjem. Nema lažnih
klikova, client-only writea ili preuranjene funkcionalnosti.

[Visual acceptance zapis i četiri snimke](../visual-acceptance/phase-4.1/README.md) potvrđuju
desktop 1536×1024 i mobile 390×844, tjedan/dan, stvarne API podatke, aktivni Raspored,
precizne metrike, 0 browser grešaka i 0 document overflowa.

## Glavne datoteke

| Područje | Datoteke |
|---|---|
| Contract | `docs/SCHEDULE_CALENDAR.md` |
| Application | `Scheduling/ScheduleCalendarContracts.cs` |
| Persistence | `Scheduling/EfScheduleCalendarQuery.cs`, persistence DI |
| API | `Scheduling/ScheduleCalendarEndpoints.cs`, `Program.cs` |
| Backend testovi | `ScheduleCalendarSqlTests.cs`, `AuthenticationApiTests.cs` |
| Frontend | `scheduleApi.ts`, `calendarDate.ts`, calendar page/grid/sidebar/CSS, route/shell |
| Frontend testovi | `ScheduleCalendar.test.tsx`, `tests/visual/phase41.mjs` |
| Evidence | `docs/visual-acceptance/phase-4.1/` |

## Testovi i završni rezultati

| Provjera | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Backend/API/domain/persistence sa stvarnim lokalnim SQL-om | PASS — 141/141, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| Phase 4.1 SQL ownership/range/filter/metrics/translacija | PASS |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| Frontend component testovi | PASS — 46/46 |
| Frontend lint, TypeScript i production build | PASS |
| Canonical desktop/mobile week/day browser gate | PASS — 4 snimke, 0 pageerrora, bez document overflowa |
| Docker rebuild, migracije i runtime | PASS — API/frontend/database healthy |
| API live/ready i frontend HTTP | PASS — 200/200/200 |
| Non-root runtime | PASS — API UID 1654, frontend `nginx` |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Novi dependencyji / migracije | nema |

Tijekom implementacije izdvojeni stvarni SQL test otkrio je neprevodivo sortiranje nakon
record projekcije; sortiranje je premješteno prije projekcije. Prvi puni SQL pokušaj pokrenut
je dok se upravo startani SQL Server još inicijalizirao i završio je timeoutom. Nakon stvarnog
readinessa, završni izdvojeni i puni run prolaze; tablica iznad navodi samo završne rezultate.

## Namjerno nije implementirano

Phase 4.2 Session detail, Phase 4.3 create, Phase 4.4 edit, recurrence/series write semantika,
replenishment, conflict override, notification delivery, detaljni report, attendance/Evidence,
Knowledge Model i dodatni demo podaci.

## Točna sljedeća točka

Završni SA review Phase 4.1, zatim zasebno odobrenje commita/pusha. Nakon prihvaćanja
sljedeća je Phase 4.2 Session detail prema ROADMAP-u; ova predaja je ne pokreće automatski.
