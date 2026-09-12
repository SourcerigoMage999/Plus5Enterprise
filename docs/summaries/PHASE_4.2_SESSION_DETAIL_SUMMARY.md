# Phase 4.2 — Screen 3.2 Session detail

## Status i datum

**DONE — implementacija i svi interni gateovi završeni, 2026-09-12.** Spremno za završni
SA review; nije samostalno proglašeno SA-approved/LOCKED. Phase 4.3 nije započeta.

## Cilj i rezultat

Implementiran je read-only detalj jednog konkretnog Sessiona na `/schedule/:sessionId`.
Calendar kartice i sljedeći podsjetnici sada vode na detalj; breadcrumb vraća na datum i
povezuje stvarni Group ili Student kontekst. Ekran prikazuje canonical session činjenice,
bilješku, status i stvarne audit/series signale.

Grupni roster računa članstva koja vrijede na početku termina, a individualni termin prikazuje
samo svog Studenta. Svaki participant vodi na postojeći digitalni dosje. Missing i cross-owner
Session vraćaju isti 404.

## API i sigurnost

Dodan je Teacher-only `GET /api/v1/schedule/{sessionId}`. Owner se uzima isključivo iz
provjerene sesije. Response ne izlaže Teacher ID, EF entitet, rowversion ni online meeting URL.
Upit je bounded jednim Sessionom i kontekstom, a roster se projicira jednim set-based upitom.

Nema write endpointa, CSRF promjene, nove migracije, entiteta ili dependencyja.

## Frontend i visual acceptance

Screen 3.2 slijedi canonical breadcrumb/header, session sažetak, roster, temu/cilj, materijale,
homework, napomene, akcije, povezano/povijest i donji info strip. Stvarni podaci prikazani su
samo gdje canonical model postoji. LessonPlan/Knowledge, attendance/evidence, materials,
homework i notifications koriste neutralna stanja i disabled kontrole; edit/cancel ostaju za
Phase 4.4.

[Visual acceptance zapis i dvije snimke](../visual-acceptance/phase-4.2/README.md) potvrđuju
desktop 1536×1024 i mobile 390×844, stvarni Calendar → detail flow, 0 browser grešaka i 0
document overflowa. Prvi mobile run otkrio je i ispravio globalni overflow roster tablice.

## Glavne datoteke

| Područje | Datoteke |
|---|---|
| Contract | `docs/SESSION_DETAIL.md` |
| Application | `Scheduling/ScheduleSessionDetailContracts.cs` |
| Persistence | `Scheduling/EfScheduleSessionDetailQuery.cs`, persistence DI |
| API | `Scheduling/ScheduleCalendarEndpoints.cs` |
| Backend testovi | `ScheduleCalendarSqlTests.cs`, `AuthenticationApiTests.cs` |
| Frontend | `ScheduleSessionDetailPage.tsx/.css`, `scheduleApi.ts`, calendar grid/sidebar/routes |
| Frontend testovi | `ScheduleSessionDetail.test.tsx`, `tests/visual/phase42.mjs` |
| Evidence | `docs/visual-acceptance/phase-4.2/` |

## Testovi i završni rezultati

| Provjera | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Backend/API/domain/persistence sa stvarnim lokalnim SQL-om | PASS — 141/141, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| Phase 4.2 SQL ownership/context/temporal-roster projekcija | PASS |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| Frontend component testovi | PASS — 51/51 |
| Frontend lint, TypeScript i production build | PASS |
| Canonical desktop/mobile browser gate | PASS — 2 snimke, 0 pageerrora, bez document overflowa |
| Docker rebuild, migracije i runtime | PASS — API/frontend/database healthy |
| API live/ready i frontend HTTP | PASS — 200/200/200 |
| Non-root runtime | PASS — API UID 1654, frontend `nginx` |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Novi dependencyji / migracije | nema |

## Namjerno nije implementirano

Phase 4.3 create, Phase 4.4 edit/cancel, duplicate, recurrence write/replenishment, attendance,
LessonPlan, live lesson, Evidence, Material, Homework i Notification modeli.

## Točna sljedeća točka

Završni SA review Phase 4.2, zatim zasebno odobrenje commita/pusha. Nakon prihvaćanja
sljedeća je Phase 4.3 Create session; ova predaja je ne pokreće automatski.
