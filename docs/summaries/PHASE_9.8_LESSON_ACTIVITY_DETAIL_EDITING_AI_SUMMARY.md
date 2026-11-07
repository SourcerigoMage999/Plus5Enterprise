# Phase 9.8 — Lesson Activity detail/edit and regeneration

## Status

**FINAL LOCKED — odobreno 2026-11-07.**

## Cilj faze

Implementirati canonical Screen 5.5 i sigurnu Draft authoring granicu za detalje jedne
`LessonActivity`, zajedno s duplicate/delete/reorder operacijama i Phase 9.2-compatible,
default-disabled AI suggestion lifecycleom bez obavezne dodatne naplate.

## Implementirano

- owner-scoped detail/read i manualni activity edit;
- `WorkMode`, description/purpose description, Teacher/Student instructions, Notes i tri
  Board visibility flaga;
- activity-level KC subset iz već zaključanih plan Knowledge Componenta;
- deep duplicate exact KB/KC/MaterialVersion referenci;
- Draft-only delete i atomic contiguous reorder;
- optimistic parent rowversion i serializable write transakcije;
- Pending/Accepted/Edited/Rejected/Replaced/Expired AI suggestion lifecycle;
- strict šest-field AI schema, 24-hour TTL, stale-write zaštita i content purge;
- privacy-minimal context builder koji isključuje identitete, Notes, materijale i raw
  Evidence/history;
- AI default OFF, manualni workflow bez providera/troška i bez PLUS 5 in-app naplate;
- Teacher-scoped 10/10-min rate limit i 50/day durable limit;
- responsive `/lesson-plans/:lessonPlanId/activities/:activityId` UI s dirty-form zaštitom;
- canonical desktop/mobile visual acceptance.

## Namjerno nije implementirano

- Confirm/Ready lifecycle — Phase 9.9;
- novi AssessableTask attachment authoring model;
- Board/LessonSession runtime, Attempt ili Evidence;
- live OpenAI poziv u acceptanceu, mandatory AI, credits ili in-app kupnja;
- cross-plan template authoring ili automatske history mutacije.

## Promijenjene / dodane datoteke

| Datoteka / grupa | Vrsta promjene | Razlog |
|---|---|---|
| `backend/src/Plus5.Domain/LessonPlanning/LessonActivity.cs` | changed | bounded detail fields, WorkMode, Board flags, AI-safe apply i duplicate |
| `backend/src/Plus5.Domain/LessonPlanning/LessonActivityAiSuggestion.cs` | added | suggestion lifecycle i content purge |
| `backend/src/Plus5.Application/LessonPlanning/LessonActivityEditingContracts.cs` | added | typed editing/AI ports i DTO-i |
| `backend/src/Plus5.Infrastructure/LessonPlanning/EfLessonActivityEditingService.cs` | added | owner-scoped transactional activity writes |
| `backend/src/Plus5.Infrastructure/LessonPlanning/EfLessonActivityAiSuggestionService.cs` | added | suggestion lifecycle, validation, TTL i daily limit |
| `backend/src/Plus5.Infrastructure/LessonPlanning/EfLessonBuilderAiContextBuilder.cs` | added | privacy-minimal provider context |
| `backend/src/Plus5.Infrastructure/LessonPlanning/LessonBuilderAiOptions.cs` | added | locked provider/model/timeout options |
| `backend/src/Plus5.Infrastructure/LessonPlanning/OpenAiLessonBuilderAiAssistant.cs` | added | optional Responses API adapter |
| `backend/src/Plus5.Api/LessonPlanning/LessonPlanEditorEndpoints.cs` | changed | detail/edit/duplicate/delete/reorder/KC/AI endpoints |
| `backend/src/Plus5.Api/Configuration/*`, `IdentityServiceExtensions.cs`, `Program.cs`, `appsettings.json` | changed/added | startup validation, DI, HttpClient i rate limit |
| `backend/src/Plus5.Infrastructure/Persistence/*` | changed | mappings, DbSet, services i model snapshot |
| `backend/src/Plus5.Infrastructure/Persistence/Migrations/20261106221708_AddLessonActivityDetailEditingAi.*` | added | schema/backfill/trigger migracija |
| `backend/tests/Plus5.Api.Tests/LessonPlanning/LessonActivityDetailEditingAi*.cs` | added | targeted domain/config/SQL gateovi |
| `frontend/src/lessonPlanning/LessonActivityDetailPage.tsx` / `.css` | added | canonical Screen 5.5 |
| `frontend/src/lessonPlanning/LessonPlanEditorPage.tsx`, `lessonPlanningApi.ts`, `app/AppRoutes.tsx` | changed | detail link, reorder i typed API |
| `frontend/tests/LessonActivityDetail.test.tsx` | added | targeted UI/manual-write gate |
| `.env.example`, `docker-compose.yml`, `docs/CONFIGURATION.md` | changed | default-off Lesson Builder AI deployment contract |
| `docs/LESSON_ACTIVITY_DETAIL_EDITING_AI.md`, ADR-0032, ROADMAP, summary | added/changed | source-of-truth contract i acceptance trail |
| `docs/visual-acceptance/phase-9.8/` | added | canonical, desktop/mobile i measurements |

## Domain / database promjene

- `LessonActivity` dobiva `WorkMode`, description, purpose description, Teacher/Student
  instructions, Notes i tri Board visibility flaga.
- Novi aggregate child `LessonActivityAiSuggestion` sa šest statusa i 24-hour TTL-om.
- Migracija `20261106221708_AddLessonActivityDetailEditingAi` backfilla postojeće activity
  retke, dodaje `LessonActivityAiSuggestions`, restriktivne FK/CHECK/indexe i SQL trigger
  `TR_LessonActivityAiSuggestions_Validate` (`51340`).
- Clean i upgrade ponašanje provjereno je na stvarnom SQL Serveru; ponovljeni migrate je
  idempotentan i EF nema pending model changes.

## API promjene

- GET/PUT activity detail;
- POST duplicate i DELETE activity;
- PUT atomic reorder i PUT activity KC subset;
- POST/GET/resolve AI suggestions;
- writeovi: Teacher auth + CSRF + rowversion; AI create dodatno 10/10-min rate limit;
- stabilni 400/404/409/422/429/503 ProblemDetails codeovi bez ownership oraclea.

Promjena je aditivna; postojeći Phase 9.5–9.7 endpointi ostaju kompatibilni.

## Frontend promjene

- nova ruta i responsive detail forma;
- page-level akcije koriste zajedničku business-shell spacing granicu i ne ulaze u
  globalnu notification/profile zonu;
- loading/error, save, dirty navigation guard, duplicate/delete i KC/Board/Notes states;
- editor dobiva detail link i reorder kontrole;
- AI-off state jasno potvrđuje da manualni workflow radi bez vanjske usluge i troška;
- StrictMode abort je ispravno ignoriran bez prikaza lažne load greške.

## Security / authorization

- Teacher identity dolazi samo iz autentificirane sesije;
- owner + non-archived + Draft scope primjenjuje se prije read/writea;
- missing/foreign/archived dijele `404 lesson_activity_not_found`;
- svi writeovi imaju CSRF i optimistic concurrency;
- AI output je strict/bounded, Notes i identiteti nisu provider context;
- AI je defaultno OFF; API key ostaje server secret;
- nema automatskih poziva, provider memoryja, PLUS 5 AI billing/creditsa ni in-app naplate.

## Testovi

| Gate | Rezultat |
|---|---|
| API Release build | PASS — 0 compile warninga, 0 grešaka |
| Targeted backend Phase 9.8 | PASS — 6/6 |
| Stvarni SQL migration/backfill/trigger | PASS — 1/1 |
| EF pending model changes | PASS — nema drifta |
| Architecture | PASS — 4/4 |
| Targeted .NET format | PASS — sve Phase 9.8 C# datoteke |
| Frontend targeted Phase 9.8 | PASS — 4/4 |
| Frontend lint/typecheck/build | PASS |
| Visual acceptance | PASS — 1536×1024 i 390×844; profile/action overlap false; overflow 0; browser errors 0 |
| Full regression | N/A — targeted phase validation is sufficient |
| Dependency audit | N/A — dependency graph i lockfileovi nisu promijenjeni |
| Compose configuration | PASS |
| Docker image/runtime infrastructure | N/A za phase acceptance — Dockerfile/runtime identity nisu promijenjeni |

Pokušani puni image build, koji nije bio potreban prema validation budgetu, zaustavljen je
warnings-as-errors pravilom zbog novih advisories na postojećim
`System.Security.Cryptography.Pkcs`/`System.Security.Cryptography.Xml` dependencyjima.
Phase 9.8 nije promijenio dependency graph niti su advisories uvedeni ovim diffom; upgrade je
zabilježen kao zaseban maintenance/security finding i nije opportunistički uvučen u feature.

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build prolazi
- [x] ciljani testovi prolaze
- [x] migracija i SQL integrity provjereni su na stvarnom SQL Serveru
- [x] auth, CSRF, ownership, rate limit i rowversion granice postoje
- [x] dokumentacija i visual evidence su ažurirani

## Arhitekturne odluke

- ADR-0028 — versioned Lesson Plan/Draft activity graph;
- ADR-0029 — privacy-minimized default-disabled Lesson Builder AI;
- ADR-0032 — activity editing i bounded AI regeneration.

## Poznati rizici / tehnički dug

- zasebno procijeniti i nadograditi postojeće cryptography dependencyje zbog novih advisories;
- live provider smoke test nije acceptance gate i nije napravljen kako se ne bi generirao
  trošak niti slao stvarni instructional context;
- AssessableTask attachment authoring čeka zaseban zaključani contract.

## Otvorena pitanja

- Nema otvorenog Phase 9.8 business ponašanja.

## Točna početna točka za sljedeću fazu

Phase 9.8 je FINAL LOCKED. Sljedeća faza je Phase 9.9: Confirmed Lesson Plan / Ready state.
Phase 9.9 mora validirati cijeli Draft graph i atomarno potvrditi immutable Ready verziju bez
stvaranja LessonSessiona, Attempta ili Evidencea.
