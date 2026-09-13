# Phase 4.4 — Screen 3.4 Edit session

## Status i datum

**DONE — implementacija i svi projektni gateovi završeni, 2026-09-13.** Spremno za završni
SA review; nije samostalno proglašeno SA-approved/LOCKED. Phase 4.5 nije započeta.

## Cilj i rezultat

Implementiran je Teacher-only Screen 3.4 na `/schedule/:sessionId/edit` s owner-scoped readom,
autoritativnim conflict previewom, updateom jednog Sessiona ili buduće serije te otkazivanjem
bez brisanja. Detail sada otvara edit/cancel flow, a uspješan update vodi na stvarni odredišni
Session detail.

Jednokratna izmjena čuva recurrence pravilo i označava instancu kao iznimku. Promjena vremena
ili lokacije za buduću seriju supersedira staro pravilo, stvara povezani successor i atomski
materializira zaključani 12-tjedni horizont. Budući stari `Scheduled` zapisi postaju
`Cancelled`; povijest i non-scheduled zapisi ostaju sačuvani.

## Backend, transakcija i sigurnost

Dodan je owner-scoped edit query te CSRF-protected `PUT`, conflict-preview i cancel endpoint.
Writeovi rade u Serializable transakciji, koriste RowVersion i ponovno provjeravaju ownership,
status, scope, Europe/Zagreb DST, Teacher/location/recurrence konflikte i concurrent write.
Cross-owner i missing Session imaju isti privacy-preserving 404 ishod.

Cancel mijenja status samo odabranog Sessiona; nema fizičkog brisanja, cijela-series
otkazivanja ni lažnog notification uspjeha. Nema nove migracije ni dependencyja.

## Frontend i visual acceptance

Obrazac je prefilled stvarnim Session podacima. Kontekst i način rada su zaključani; zadani
scope je samo jedna instanca. Future-series izbor je dostupan samo kada je siguran i dopušta
samo vrijeme/lokaciju. Promjena strukture Group rasporeda vodi u 2.9. Implementirani su
loading/error/retry, inline validacija, live conflict signal, dirty guard, cancel potvrda i
disabled budući color/reminder/notification contracti.

[Visual acceptance zapis i dvije snimke](../visual-acceptance/phase-4.4/README.md) potvrđuju
canonical desktop 1536×1024 i mobile 390×844, stvarni aktivni Session, 0 writeova tijekom
snimanja, 0 browser grešaka i 0 document overflowa.

## Glavne datoteke

| Područje | Datoteke |
|---|---|
| Contract | `docs/SESSION_EDITING.md` |
| Domain/Application | `Session.cs`, `Scheduling/ScheduleEditingContracts.cs` |
| Persistence | `Scheduling/EfScheduleEditingQuery.cs`, `EfScheduleEditingService.cs`, persistence DI |
| API | `Scheduling/ScheduleCalendarEndpoints.cs` |
| Backend testovi | `ScheduleEditingSqlTests.cs`, `AuthenticationApiTests.cs` |
| Frontend | `ScheduleEditPage.tsx/.css`, `scheduleApi.ts`, detail/routes |
| Frontend testovi | `ScheduleEdit.test.tsx`, detail regresije, `tests/visual/phase44.mjs` |
| Evidence | `docs/visual-acceptance/phase-4.4/` |

## Testovi i završni rezultati

| Provjera | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Backend/API/domain/persistence sa stvarnim lokalnim SQL-om | PASS — 143/143, 0 skipped |
| Architecture dependency granice | PASS — 4/4 u Debugu; Release izvršavanje blokira host Windows Application Control nakon uspješnog builda |
| Phase 4.4 SQL ownership/exception/supersession/conflict/concurrency/cancel | PASS |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| Frontend component testovi | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| Canonical desktop/mobile browser gate | PASS — 2 snimke, 0 pageerrora, bez document overflowa |
| Docker rebuild, migracije i runtime | PASS — API/frontend/database healthy |
| API live/ready i frontend HTTP | PASS — 200/200/200 |
| Non-root runtime | PASS — API UID 1654, frontend `nginx` |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Novi dependencyji / migracije | nema |

## Namjerno nije implementirano

Promjena konteksta, promjena dana/strukture redovitog grupnog rasporeda, cancel cijele serije,
replenishment, reminders, notifications, attendance, LessonPlan, live lesson, Evidence,
Material i Homework.

## Točna sljedeća točka

Završni SA review Phase 4.4, zatim zasebno odobrenje commita/pusha. Nakon prihvaćanja sljedeća
je Phase 4.5 Recurrence/series consistency tests; ova predaja je ne pokreće automatski.
