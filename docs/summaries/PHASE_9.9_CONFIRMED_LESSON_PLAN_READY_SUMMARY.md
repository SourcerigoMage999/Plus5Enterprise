# Phase 9.9 — Confirmed Lesson Plan / Ready state

## Status

**FINAL LOCKED — odobreno 2026-11-13.**

## Cilj faze

Zatvoriti Lesson Builder planiranje atomarnom potvrdom cijelog Draft grapha u immutable,
verzionirani Ready snapshot i prikazati canonical završni pregled bez pokretanja Boarda.

## Implementirano

- `Draft → Ready → Superseded` one-way lifecycle i `CurrentReadyVersionId`;
- serializable confirm transakcija s plan/version rowversion zaštitom;
- Ready validacija cilja, aktivnosti, contiguous redoslijeda, trajanja i Clean exact
  MaterialVersion referenci;
- SQL errori `51350`, `51351` i postojeći immutable `51303`;
- Ready → novi Draft deep clone bez in-place promjene povijesti;
- Teacher-only GET/confirm/edit API s CSRF, 404 isolation, 409 i 422 semantikom;
- responsive `/lesson-plans/:lessonPlanId/confirmation` Draft/Ready UI;
- canonical summary, aktivnosti, materijali, balans i truthful readiness no-data;
- disabled Board CTA do Phase 10 i page-action/profile spacing pravilo;
- canonical desktop/mobile visual acceptance.

## Namjerno nije implementirano

- Board/LessonSession runtime;
- Attempt, Evidence, Mastery/Readiness ili Knowledge Model mutation;
- overall readiness, predicted grade ili drugi nepostojeći izračun;
- novi AssessableTask attachment authoring;
- auto-confirm iz AI prijedloga.

## Promijenjene / dodane datoteke

| Datoteka / grupa | Vrsta | Razlog |
|---|---|---|
| `backend/src/Plus5.Domain/LessonPlanning/LessonPlan*.cs` | changed | lifecycle i ready pointer metode |
| `backend/src/Plus5.Application/LessonPlanning/LessonPlanConfirmationContracts.cs` | added | typed port i DTO-i |
| `backend/src/Plus5.Infrastructure/LessonPlanning/EfLessonPlanConfirmationService.cs` | added | read, confirm i deep-clone transakcije |
| `backend/src/Plus5.Api/LessonPlanning/LessonPlanConfirmationEndpoints.cs` | added | Teacher-only confirmation API |
| `backend/src/Plus5.Infrastructure/Persistence/Migrations/20261112213439_FinalizeLessonPlanReadyState.*` | added | Ready validation/lifecycle trigger |
| `backend/tests/Plus5.Api.Tests/LessonPlanning/LessonPlanConfirmationTests.cs` | added | domain i stvarni SQL gate |
| `frontend/src/lessonPlanning/LessonPlanConfirmationPage.*` | added | canonical Draft/Ready ekran |
| `frontend/src/lessonPlanning/lessonPlanningApi.ts`, editor, routes | changed | typed API i navigation |
| `frontend/tests/LessonPlanConfirmation.test.tsx`, editor test | added/changed | targeted UI gate |
| `docs/LESSON_PLAN_CONFIRMATION_READY.md`, ROADMAP, summary | added/changed | source-of-truth i evidence |
| `docs/visual-acceptance/phase-9.9/` | added | canonical, desktop/mobile, measurements |

## Domain / database promjene

- Nema novih tablica ili kolona.
- SQL-only migracija pojačava postojeći snapshot trigger Ready graph validacijom i
  jednosmjernim lifecycleom; generirani designer i snapshot metadata nemaju model drift.
- Ready edit deep-clonea graph u novu verziju; AI suggestions nisu dio snapshot kopije.

## API promjene

- aditivni confirmation GET/confirm/edit endpointi;
- `401`, indistinguishable `404`, `409` conflict/not-Draft i `422` validation;
- confirm/edit writeovi imaju CSRF i bounded rowversion input.

## Frontend promjene

- editor `Spremi i nastavi` vodi na confirmation ekran;
- loading/error, Draft validation, confirm, Ready state i edit-to-new-Draft flow;
- Board CTA jasno je deferred i disabled;
- mobile stack i document overflow zaštita.

## Security / authorization

- Teacher identitet dolazi samo iz sesije;
- owner/non-archived scope prije čitanja ili writea;
- CSRF, optimistic concurrency, serializable transakcije i SQL integrity guard;
- bez ownership oraclea, implicitnog Board writea ili AI/provider poziva.

## Testovi

| Gate | Rezultat |
|---|---|
| Release build | PASS — 0 warninga, 0 grešaka |
| Targeted backend Phase 9.9 | PASS — 4/4, uključujući 2/2 stvarna SQL testa |
| EF drift / idempotent script generation | PASS |
| Architecture | PASS — 4/4 |
| Frontend targeted | PASS — 4/4 |
| Frontend lint/typecheck/build | PASS |
| Visual acceptance | PASS — stvarni Ready ekran, desktop 1536×1024, mobile 390×844, overflow 0, browser errors 0 |
| Full regression | N/A — targeted phase validation is sufficient |
| Dependency audit | N/A — dependency graph i lockfileovi nisu promijenjeni |
| Docker infrastructure validation | N/A — infrastructure unchanged |

Windows Application Control blokirao je svježi xUnit DLL; isti unaprijed određeni filter
prošao je u već lokalnom .NET 10 Linux SDK containeru. Prvi container restore prijavio je
postojeće cryptography advisories; audit nije phase gate i dependency nije promijenjen, pa
je samo test restore ponovljen s `NuGetAudit=false`, bez opportunističkog upgradea.

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build i ciljani testovi prolaze
- [x] migracija, SQL trigger i model drift provjereni su
- [x] auth, CSRF, ownership i concurrency granice postoje
- [x] dokumentacija i visual evidence su ažurirani

## Arhitekturne odluke

- ADR-0028 — versioned Lesson Plan/Draft activity graph;
- Phase 9.9 zaključava postojeći `LESSON_BUILDER_DOMAIN.md` Ready lifecycle bez nove
  poslovne matematike.

## Poznati rizici / tehnički dug

- postojeći cryptography dependency advisories ostaju zaseban maintenance/security finding;
- Board runtime i stvaranje LessonSessiona počinju tek u Phase 10;
- AssessableTask attachment authoring čeka zaseban contract.

## Otvorena pitanja

- Nema otvorenog Phase 9.9 business ponašanja.

## Točna početna točka za sljedeću fazu

Nakon SA reviewa i FINAL LOCK-a Phase 9.9, sljedeća nezavršena stavka je Phase 10.1:
Teacher Board i Lesson Session lifecycle. Phase 10 mora koristiti exact Ready
`LessonPlanVersion` snapshot i ne smije retroaktivno mijenjati plan.
