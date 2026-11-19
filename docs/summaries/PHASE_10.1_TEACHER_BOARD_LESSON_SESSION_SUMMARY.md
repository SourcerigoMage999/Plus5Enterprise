# Phase 10.1 — Teacher Board and Lesson Session lifecycle

## Status

**IMPLEMENTED / REVIEW READY — 2026-11-19.**

## Cilj faze

Uvesti trajni Teacher Board runtime koji svako stvarno izvođenje veže uz exact Ready
LessonPlanVersion, sigurno se oporavlja nakon prekida i ne mijenja originalni plan.

## Implementirano

- `LessonSession` aggregate sa scheduled i unscheduled atomarnim startom;
- materializirani Pending runtime timeline i exact activity snapshot;
- exact MaterialVersion i AssessableTaskVersion reference;
- DB-backed operation idempotency, fingerprint i monotona revision concurrency;
- owner-scoped active-session chooser i authoritative recovery read;
- typed timeline reorder mutation sa stale-write konfliktom;
- Teacher-only start/read/mutation API s CSRF zaštitom writeova;
- responsive `/board` i `/board/sessions/:lessonSessionId` UI;
- stvarni desktop/mobile canonical visual acceptance.

## Ključne odluke

- isti Ready plan smije imati više zasebnih izvođenja;
- Teacher smije imati više aktivnih različitih runtime konteksta;
- isti calendar occurrence i isti aktivni unscheduled kontekst ne smiju se duplicirati;
- scheduled start atomarno postavlja kalendarski Session u `InProgress`;
- gubitak veze nije domenski završetak, a refresh/recovery nisu write;
- `Saved` znači tek uspješan SQL commit;
- Phase 10.1 nema Complete/Void, activity execution, Attempt ni Evidence side effect.

## Promijenjene i dodane datoteke

### Backend source i migracija

- `backend/src/Plus5.Api/Program.cs` — registracija Lesson Session endpointa;
- `backend/src/Plus5.Api/LessonSessions/LessonSessionEndpoints.cs` — Teacher-only API;
- `backend/src/Plus5.Application/LessonSessions/LessonSessionContracts.cs` — application
  port, DTO-i i failure contract;
- `backend/src/Plus5.Domain/LessonSessions/LessonSession.cs` — runtime aggregate;
- `backend/src/Plus5.Domain/LessonSessions/LessonSessionActivity.cs` — materializirana
  aktivnost;
- `backend/src/Plus5.Domain/LessonSessions/LessonSessionOperation.cs` — idempotency ledger;
- `backend/src/Plus5.Domain/LessonSessions/LessonSessionReferences.cs` — exact reference i
  audit modeli;
- `backend/src/Plus5.Infrastructure/LessonSessions/EfLessonSessionService.cs` — SQL-backed
  start, recovery i mutation servis;
- `backend/src/Plus5.Infrastructure/LessonPlanning/EfLessonPlanConfirmationService.cs` —
  stvarna Board start granica za Ready plan;
- `backend/src/Plus5.Infrastructure/Persistence/LessonSessionPersistenceConfigurations.cs`
  — EF konfiguracije, indeksi i constrainti;
- `backend/src/Plus5.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`
  — registracija servisa;
- `backend/src/Plus5.Infrastructure/Persistence/Plus5DbContext.cs` — novi DbSetovi;
- `backend/src/Plus5.Infrastructure/Persistence/Migrations/20261115173224_AddLessonSessionRuntime.cs`
  — schema, indeksi i SQL triggeri;
- `backend/src/Plus5.Infrastructure/Persistence/Migrations/20261115173224_AddLessonSessionRuntime.Designer.cs`
  — EF migration metadata;
- `backend/src/Plus5.Infrastructure/Persistence/Migrations/Plus5DbContextModelSnapshot.cs`
  — aktualni EF model snapshot.

### Backend testovi

- `backend/tests/Plus5.Api.Tests/LessonSessions/LessonSessionRuntimeTests.cs` — domain,
  API i stvarni SQL runtime gate;
- `backend/tests/Plus5.Api.Tests/LessonPlanning/LessonPlanConfirmationTests.cs` — Board
  availability regresija za Ready plan.

### Frontend source i testovi

- `frontend/src/board/boardApi.ts` — typed Board API client;
- `frontend/src/board/BoardLandingPage.tsx` — active-session recovery chooser;
- `frontend/src/board/TeacherBoardPage.tsx` — authoritative runtime ekran;
- `frontend/src/board/TeacherBoardPage.css` — responsive Board layout;
- `frontend/src/api/apiClient.ts` — Lesson Session error mapping;
- `frontend/src/app/AppRoutes.tsx` — `/board` rute;
- `frontend/src/app/AppShell.tsx` — Board business header integracija;
- `frontend/src/lessonPlanning/LessonPlanConfirmationPage.tsx` — stvarni start i prijelaz
  na Board;
- `frontend/tests/TeacherBoardPage.test.tsx` — targeted Board UI testovi;
- `frontend/tests/LessonPlanConfirmation.test.tsx` — start navigation regresija.

### Dokumentacija i visual evidence

- `docs/LESSON_SESSION_RUNTIME.md` — canonical Phase 10.1 contract;
- `docs/DECISION_LOG.md` — ADR-0033;
- `docs/OPEN_QUESTIONS.md` — zatvoren 10.1 gate i zadržane buduće odluke;
- `docs/PERSISTENCE.md` — runtime persistence i SQL invarianti;
- `docs/ROADMAP.md` — Phase 10.1 status i acceptance;
- `docs/summaries/PHASE_10.1_TEACHER_BOARD_LESSON_SESSION_SUMMARY.md` — phase evidence;
- `docs/visual-acceptance/phase-10.1/README.md` — visual gate;
- `docs/visual-acceptance/phase-10.1/measurements.json` — viewport i overflow mjerenja;
- `docs/visual-acceptance/phase-10.1/canonical.png` — canonical izvor;
- `docs/visual-acceptance/phase-10.1/teacher-board-desktop-1536x1024.png` — desktop evidence;
- `docs/visual-acceptance/phase-10.1/teacher-board-mobile-390x844.png` — mobile evidence.

## Persistence i integritet

- migracija `20261115173224_AddLessonSessionRuntime`;
- nove tablice za session, activities, exact reference junctione, operations i audit events;
- composite owner FK-ovi, restrictive delete, CHECK i filtered unique zaštite;
- SQL triggeri za lifecycle immutability, exact reference, immutable operation ledger i
  append-only audit;
- serializable start transakcija bez djelomično vidljivog Boarda.

## API i sigurnost

- `POST /api/v1/lesson-plans/{lessonPlanId}/lesson-sessions/start`;
- `GET /api/v1/lesson-sessions/active`;
- `GET /api/v1/lesson-sessions/{lessonSessionId}`;
- `POST /api/v1/lesson-sessions/{lessonSessionId}/mutations`;
- Teacher identitet isključivo iz autentificirane sesije, owner scope prije rezultata,
  indistinguishable missing/foreign `404` i CSRF na writeovima.

## Validation

| Gate | Rezultat |
|---|---|
| Release build | PASS — 0 warninga, 0 grešaka |
| Targeted backend/API/SQL | PASS — 7/7 |
| EF drift / idempotent migration script | PASS |
| Architecture | PASS — 4/4 |
| Frontend targeted | PASS — 4/4 |
| Frontend lint/typecheck/build | PASS |
| Visual acceptance | PASS — desktop 1536×1024, mobile 390×844, overflow 0, browser errors 0 |
| Full regression | N/A — targeted phase validation is sufficient |
| Dependency audit | N/A — dependency graph i lockfileovi nisu promijenjeni |
| Docker infrastructure validation | N/A — infrastructure unchanged |

Windows Application Control blokirao je host xUnit DLL; unaprijed poznati lokalni .NET 10
Linux SDK container korišten je kao jedini alternativni runner. Ciljani testovi koristili su
stvarni disposable SQL Server. Dockerfile, Compose i runtime identity nisu ponovno
certificirani jer nisu promijenjeni.

## Visual acceptance

Stvarna Teacher prijava, stvarni Ready plan, stvarni start write i stvarni recovery API
prošli su bez auth bypassa, API interceptiona ili DOM mutationa. Canonical, desktop/mobile
screenshotovi i mjerenja nalaze se u `docs/visual-acceptance/phase-10.1/`.

## Namjerno nije implementirano

- activity start/pause/complete/partial/skip, ad-hoc activity i Attempt — Phase 10.2;
- live diagnostic — Phase 10.3;
- plan-vs-executed summary — Phase 10.4;
- explicit closure, Completed void i Evidence finalization/invalidation — Phase 10.5/11;
- attendance, billing, homework ili Mastery/Readiness mutation.

## Self-review

- [x] scope nije proširen izvan Phase 10.1
- [x] SA odluke i exact Ready granica su očuvane
- [x] owner/CSRF/concurrency/idempotency zaštite postoje
- [x] stvarni SQL i minimalni browser evidence prolaze
- [x] nema Attempt/Evidence ili terminal lifecycle scope creepa
- [x] dokumentacija, persistence contract i ROADMAP su ažurirani

## Otvorena pitanja

- Nema otvorenog Phase 10.1 business ponašanja.

## Točna početna točka za sljedeću fazu

Nakon SA reviewa i FINAL LOCK-a Phase 10.1, Phase 10.2 uvodi universal activity runtime,
ad-hoc activity i Attempt capture bez retroaktivne izmjene Ready plana ili Phase 10.1
idempotency/recovery contracta.
