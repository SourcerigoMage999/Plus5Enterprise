# Phase 6.3 — Screen 4.1 Material library

## Status

**IMPLEMENTED — REVIEW READY (2026-10-01).**

Faza nije FINAL LOCKED. Čeka SA review i izričito odobrenje prije commita/pusha.

## Cilj faze

Implementirati prvi stvarni Materials business ekran nad zaključanim Phase 6.1–6.2
domain/persistence contractom: Teacher-only read biblioteku s owner/share scopeom,
pretragom, filtrima, sortom, responsive prikazom i dokazivom canonical visual fidelity,
bez preuranjenog import/detail/edit/write scopea.

## Implementirano

- application read contract za Mine/SharedWithMe, sort, filtere, facets i pagination;
- EF Core query nad `Material.CurrentVersionId`, Active snapshotom i Clean fileom;
- SQL-level owner i eksplicitni share scope prije projekcije rezultata;
- `GET /api/v1/materials` i `GET /api/v1/materials/overview`, oba Teacher-only;
- `/materials` stvarni route s canonical shellom, searchom, tabovima, grid/list prikazom,
  sortom, filtrima, counts/recent zonom i paginacijom;
- loading, error/retry, owner empty, shared empty i no-results stanja;
- disabled buduće create/item akcije s iskrenim phase objašnjenjem;
- desktop/mobile stvarni login/API visual acceptance i ponovljiva Playwright skripta.

## Promijenjene / dodane datoteke

| Grupa | Datoteke | Razlog |
|---|---|---|
| application | `MaterialLibraryContracts.cs` | stabilan query/read-model contract |
| infrastructure | `EfMaterialLibraryQuery.cs`, DI registration | autorizirani EF read model |
| API | `MaterialLibraryEndpoints.cs`, `Program.cs` | Teacher-only versioned endpointi |
| backend tests | `MaterialLibraryQueryTests.cs`, `MaterialLibrarySqlTests.cs`, auth regression | scope, translation, filter i anonymous gate |
| frontend | `materialsApi.ts`, `MaterialLibraryPage.tsx/.css`, `AppRoutes.tsx` | stvarni Screen 4.1 route i UI |
| frontend tests | `Materials.test.tsx`, `App.test.tsx`, `visual/phase63.mjs` | component/regression/visual gate |
| docs | `MATERIAL_LIBRARY.md`, `ROADMAP.md`, `FRONTEND_FOUNDATION.md`, visual evidence, ovaj summary | source-of-truth i audit trail |

## Domain / database promjene

Nema schema, migracije ili Material lifecycle promjene. Query koristi već zaključane
`Material`, `MaterialVersion`, `MaterialFile`, metadata/tag i `MaterialShare` retke.

## API promjene

- `GET /api/v1/materials`: pagination, ownership, sort, search te Subject/Program/Grade/
  Type/Tag filtri;
- `GET /api/v1/materials/overview`: autorizirani facets, type counts i recently added;
- anonymous zahtjev dobiva `401`;
- Teacher identitet čita se isključivo iz autentificiranog session claima.

Response ne sadrži object-storage key, bucket, checksum, scan detalje ili binary sadržaj.

## Frontend promjene

- `/materials` više nije foundation placeholder;
- stanje pretrage/filtera/sorta/prikaza/stranice ostaje u URL-u;
- responsive desktop grid i mobilni single-column flow nemaju document overflow;
- canonical thumbnails nisu lažirani: koristi se format preview dok ne postoji službeni
  preview/conversion contract;
- create/detail/edit/share/archive/delete akcije nisu aktivirane prije svojih faza.

## Security / authorization

- owner/share ograničenje ulazi u SQL query prije paginationa, counts ili facets;
- shared rezultat traži eksplicitni recipient grant, Shared visibility i drugog ownera;
- samo Clean file i Active/current version ulaze u UI;
- filtri ne mogu proširiti autorizirani skup;
- stvarni browser gate potvrđuje normalan login i stvarne endpoint odgovore bez bypassa.

## Testovi

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Phase 6.3 query testovi | PASS — 3/3 |
| Stvarni SQL Server owner/share/translation test | PASS — 1/1 |
| Puni backend regression | PASS — 199 prošlo, 45 opt-in SQL testova preskočeno, 0 palo |
| Architecture testovi | PASS — 4/4 |
| Phase 6.3 frontend testovi | PASS — 4/4 |
| Puni frontend regression | PASS — 74/74 |
| Frontend lint/typecheck/production build | PASS |
| `.NET format --verify-no-changes` | PASS |
| EF pending-model check | PASS — nema pending model promjene |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker build / runtime health | PASS — API live/ready i frontend HTTP 200 |
| Non-root runtime | PASS — API `uid=1654(app)`, frontend `uid=101(nginx)` |
| Canonical visual acceptance | PASS — desktop 1536×1024, mobile 390×844, no-results/shared evidence |

Visual gate koristi lokalni lifecycle-valid fixture samo u development SQL bazi. Fixture
nije production seed i nije dio Gita. Snimke koriste stvarni login i API, bez interceptiona,
DOM mutationa ili business writea tijekom capturea.

## Self-review

- [x] nema client-supplied Teacher scopea ili post-query security filtera
- [x] private/revoked/foreign/draft/unclean sadržaj ne ulazi u library read model
- [x] storage tajne i object key nisu izloženi
- [x] canonical business elementi nisu zamijenjeni izmišljenim featureom
- [x] thumbnail nedostatak nije prikriven fake sadržajem
- [x] budući write workflowi ostaju disabled
- [x] nema schema ili migration drifta
- [x] dokumentacija i visual evidence odgovaraju stvarnom runtimeu

## Arhitekturne odluke

Nema novog ADR-a. Implementacija primjenjuje već zaključane ADR-0023/ADR-0024 storage,
ownership, versioning i metadata granice.

## Poznati rizici / tehnički dug

- binary preview/thumbnail pipeline nije ugovoren ni implementiran;
- detail i signed-download authorization pripadaju Phase 6.4;
- import/create pripada Phase 6.6, edit/version history Phase 6.7, a permissions mutation
  Phase 6.8;
- production referentni katalog i production sadržaj ostaju zasebni provisioning gate.

## Otvorena pitanja

Nema otvorenog pitanja koje blokira Phase 6.3 REVIEW READY scope.

## Točna početna točka za sljedeću fazu

Nakon SA FINAL LOCK-a, Phase 6.4 može dodati owner/share autorizirani Material detail nad
istim current-version read contractom, uz zaseban signed-download/preview security gate i bez
preskakanja storage ili share permission pravila.
