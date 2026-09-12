# Phase 4.3 — Screen 3.3 Create session

## Status i datum

**DONE — implementacija i svi interni gateovi završeni, 2026-09-12.** Spremno za završni
SA review; nije samostalno proglašeno SA-approved/LOCKED. Phase 4.4 nije započeta.

## Cilj i rezultat

Implementiran je Teacher-only Screen 3.3 na `/schedule/new` i atomski Session create write.
Teacher može stvoriti dodatni jednokratni grupni termin, jednokratni individualni termin ili
tjednu individualnu seriju. Otvorena individualna serija materializira zaključani početni
horizont od 12 tjedana. Redoviti raspored grupe ostaje isključivo Group workflow.

Calendar `+ Novi termin` prenosi odabrani datum. Session detail `Dupliciraj termin` unaprijed
popunjava create obrazac, ali ne klonira recurrence. Uspješan write vodi na novostvoreni detalj.

## Backend, transakcija i sigurnost

Dodan je CSRF-protected `POST /api/v1/schedule` s `201 Created`, Location headerom, prvim
Session ID-em i brojem stvorenih instanci. Teacher dolazi isključivo iz provjerene sesije;
Group/Student/Location su owner-scoped, a cross-owner i missing context imaju isti not-found
ishod.

Write je Serializable i atomically sprema Session ili individualnu seriju sa svim početnim
Sessionima. Server ponovno provjerava kontekst, lokalno Europe/Zagreb vrijeme, DST,
Teacher/location intervalne konflikte, recurrence konflikte i concurrent write. Nema conflict
overridea, djelomičnog uspjeha, nove migracije ni dependencyja.

## Frontend i visual acceptance

Obrazac sadrži canonical osnovne podatke, datum/vrijeme, recurrence, lokaciju, sažetak, akcije,
loading/empty/error/retry/saving stanja i dirty-navigation guard. Naziv i napomena su stvarni
Session writeovi. Boja, reminders i notifications pošteno ostaju disabled.

[Visual acceptance zapis i dvije snimke](../visual-acceptance/phase-4.3/README.md) potvrđuju
canonical desktop 1536×1024 i mobile 390×844, stvarni owner-scoped kontekst, 0 browser grešaka
i 0 document overflowa. Dokumentirano je namjerno odstupanje: grupna recurrence iz PNG-a je
disabled jer tekstualni source i zaključani model redoviti raspored grupe drže u 2.8/2.9.

## Glavne datoteke

| Područje | Datoteke |
|---|---|
| Contract | `docs/SESSION_CREATION.md` |
| Application | `Scheduling/ScheduleCreationContracts.cs` |
| Persistence | `Scheduling/EfScheduleCreationService.cs`, persistence DI |
| API | `Scheduling/ScheduleCalendarEndpoints.cs` |
| Backend testovi | `ScheduleCreationSqlTests.cs`, `AuthenticationApiTests.cs` |
| Frontend | `ScheduleCreatePage.tsx/.css`, `scheduleApi.ts`, calendar/detail/routes |
| Frontend testovi | `ScheduleCreate.test.tsx`, calendar/detail regresije, `tests/visual/phase43.mjs` |
| Evidence | `docs/visual-acceptance/phase-4.3/` |

## Testovi i završni rezultati

| Provjera | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Backend/API/domain/persistence sa stvarnim lokalnim SQL-om | PASS — 142/142, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| Phase 4.3 SQL ownership/atomicity/recurrence/conflict/concurrency | PASS |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| Frontend component testovi | PASS — 55/55 |
| Frontend lint, TypeScript i production build | PASS |
| Canonical desktop/mobile browser gate | PASS — 2 snimke, 0 pageerrora, bez document overflowa |
| Docker rebuild, migracije i runtime | PASS — API/frontend/database healthy |
| API live/ready i frontend HTTP | PASS — 200/200/200 |
| Non-root runtime | PASS — API UID 1654, frontend `nginx` |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Novi dependencyji / migracije | nema |

## Namjerno nije implementirano

Phase 4.4 edit/cancel, promjena jedne instance ili buduće serije, recurrence replenishment,
arbitrary/mjesečna recurrence, reminders, notifications, attendance, LessonPlan, live lesson,
Evidence, Material i Homework.

## Točna sljedeća točka

Završni SA review Phase 4.3, zatim zasebno odobrenje commita/pusha. Nakon prihvaćanja sljedeća
je Phase 4.4 Edit session; ova predaja je ne pokreće automatski.
