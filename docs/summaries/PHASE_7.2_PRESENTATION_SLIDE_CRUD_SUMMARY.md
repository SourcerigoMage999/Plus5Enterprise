# Phase 7.2 — Presentation slide CRUD/reorder/duplicate

## Status

**FINAL LOCKED — odobreno 2026-10-11.**

## Cilj faze

Implementirati Phase 7.1 PresentationDocument/Slide persistence temelj i owner-only editor za
CRUD/reorder/duplicate praznih slajdova, uz Draft-only lifecycle, document rowversion, SQL
integrity i canonical desktop/mobile dokaz, bez sadržajnih elemenata iz 7.3.

## Implementirano

- `PresentationDocument` i `PresentationSlide` domain modeli;
- aditivna EF migracija `AddPresentationSlideDocumentModel` s constraintima, unique orderom,
  restrictive FK-ovima, rowversionom i Draft/PRESENTATION SQL triggerima;
- kloniranje postojećeg presentation grafa pri Material restore/new Draft verziji;
- owner-scoped query i atomski initialize/add/delete/duplicate/reorder service;
- kontrolirani `400/404/409` contract, CSRF, 16 KiB request body limit i dedicated
  Teacher-scoped limit od 120 writeova u minuti;
- `/materials/:materialId/presentation` start state i canonical P3-derived editor;
- stvarni SQL migration/concurrency/integrity testovi i ponovljivi browser visual gate;
- runtime pronađene i ispravljene granice: explicit DELETE body binding, dvostruki frontend
  `/api/v1` prefix, null/missing rowversion `500` i autentikacija prije Teacher-partitioned
  rate limitera.

## Namjerno nije implementirano

- PresentationElement/content handleri, asseti, tema i layout iz 7.3;
- teaching-specific, Quick Check i Knowledge runtime iz 7.4–7.6;
- autosave/recovery/conflict UX iz 7.7;
- notes, preview, package/publish/export iz 7.8;
- AI, PPTX conversion i collaboration.

## Promijenjene / dodane kategorije datoteka

- Domain: PresentationDocument i PresentationSlide;
- Application/API: editor contracti, endpointi i route registracija;
- Infrastructure: EF query/write service, konfiguracije, migracija i model snapshot;
- Frontend: API client, route, editor component/CSS i Material edit handoff;
- Tests: domain/model, API/security, stvarni SQL i frontend component testovi;
- Docs/evidence: contract, ROADMAP, persistence/ADR bilješka, summary i visual acceptance.

## Domain / database promjene

- nove tablice `PresentationDocuments` i `PresentationSlides`;
- novi migration ID `20261009200502_AddPresentationSlideDocumentModel`;
- nema seeda ni backfilla;
- stvarni upgrade od prethodne migracije i ponovljeni apply su idempotentni;
- SQL error `51241` štiti document snapshot, `51242` slide snapshot.

## API promjene

Dodani su owner-only GET/initialize/add/delete/duplicate/reorder endpointi pod
`/api/v1/materials/{materialId}/presentation`. Inicijalizacija koristi Material rowversion;
nakon stvaranja dokumenta svaki write koristi `expectedDocumentRowVersion`.

## Frontend promjene

Dodani su presentation API client, route, start/editor states, persisted slide selection i
CRUD/reorder kontrole. Buduće kontrole su disabled s poštenom Phase granicom. Material Draft
PRESENTATION edit ekran vodi u editor.

## Security / authorization

- Teacher-only i server-derived teacher ID;
- missing/foreign/archived/non-Presentation resursi ne otkrivaju ownership;
- svi writeovi imaju CSRF, bounded body i dedicated Teacher-scoped fixed-window rate limit;
- malformed/missing revision daje kontrolirani invalid request;
- stvarni runtime potvrđuje anonymous `401`, missing CSRF `400` i stale revision `409`.

## Validacija

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Targeted domain/API tests | PASS — 6/6 |
| Targeted stvarni SQL tests | PASS — 3/3 |
| Full backend regression | PASS — 226 PASS, 52 očekivana SQL opt-in skipa |
| Architecture tests | PASS — 4/4 |
| Frontend targeted tests | PASS — 4/4 |
| Frontend lint/typecheck/build | PASS |
| EF pending model changes | PASS — nema drifta |
| Idempotent migration script | PASS — migration guard i oba triggera prisutni |
| Docker migration/runtime/health | PASS — live/ready/frontend HTTP 200 |
| Docker non-root | PASS — API `1654(app)`, frontend `101(nginx)` |
| Visual acceptance | PASS — 1536×1024 i 390×844, bez overflowa/errora/capture writeova |
| Dependency vulnerability audit | PASS — NuGet 0 poznatih; npm 0 |

Lokalni Windows Application Control blokirao je novoizgrađeni xUnit DLL. Isti source/testovi
izvršeni su u pinned službenom .NET 10 SDK containeru s izoliranim `/tmp` artefaktima; to nije
project test skip ni zamjena za stvarni SQL, koji je korišten kroz Compose mrežu.

## Self-review

- [x] implementirana je samo Phase 7.2 granica
- [x] ownership, CSRF, concurrency, limiti i SQL integrity ponovno su provjereni
- [x] nema PresentationElement/Quick Check/autosave/publish scope creepa
- [x] canonical desktop/mobile usporedba i odstupanja dokumentirani su
- [x] migracija nema destruktivan backfill ni model drift
- [x] credentiali i storage ključevi nisu spremljeni u Git

## Arhitekturne odluke

Nema novog ADR-a. Implementacija primjenjuje ADR-0026. Jedina lifecycle nijansa je da prvi
document create koristi postojeći Material rowversion, nakon čega document rowversion postaje
authority za sve slide writeove.

## Poznati rizici / otvorena pitanja

Nema otvorenih blockera za Phase 7.2. PresentationDocument restore kopira samo trenutno implementirane
prazne slide retke; 7.3 mora proširiti isti version-copy boundary elementima bez mijenjanja
7.2 redoslijeda ili concurrency contracta.
