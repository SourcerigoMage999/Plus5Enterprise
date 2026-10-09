# Phase 7.1 — Technical design for slide document model

## Status

**IMPLEMENTED / REVIEW READY — 2026-10-09.**

## Cilj faze

Zaključati tehnički model nativne PLUS 5 prezentacije prije persistence, API i UI
implementacije Phase 7.2–7.8, uz očuvanje postojećih MaterialVersion, MaterialFile,
AssessableTaskVersion i Evidence granica.

## Implementirano

- definiran je version-bound `PresentationDocument → PresentationSlide → PresentationElement`
  aggregate;
- relacijski su zaključani identitet, redoslijed, geometry, asset i Task reference;
- type-specific sadržaj koristi bounded, validirani i schema-versioned JSON payload;
- zaključani su 1600×900 logical canvas, document rowversion i optimistic concurrency;
- odvojeni su instructional slide mapping i leaf-only AssessableTaskVersion mapping;
- definiran je private Clean-only PresentationAsset contract;
- definiran je deterministic server-generated presentation ZIP manifest koji čuva postojeći
  MaterialFile activation invariant;
- uvedeni su bounded slide/element/payload limiti i sigurni schema-evolution/failure uvjeti;
- ADR-0026 bilježi trajnu arhitekturnu odluku.

## Namjerno nije implementirano

- domain klase, EF konfiguracija, migracija ili SQL tablice;
- API, frontend route, editor ili visual acceptance;
- slide CRUD/reorder/duplicate iz 7.2;
- core/teaching/Quick Check handleri iz 7.3–7.6;
- autosave/recovery iz 7.7 i preview/publish orchestration iz 7.8;
- PPTX conversion, export, Board/Attempt/Evidence runtime i AI iz 7.9.

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `docs/PRESENTATION_SLIDE_DOCUMENT_MODEL.md` | added | canonical Phase 7.1 technical contract |
| `docs/DECISION_LOG.md` | changed | ADR-0026 |
| `docs/ROADMAP.md` | changed | Phase 7.1 REVIEW READY status i acceptance |
| `docs/summaries/PHASE_7.1_PRESENTATION_SLIDE_DOCUMENT_MODEL_SUMMARY.md` | added | phase evidence i handoff |

## Domain / database promjene

- Novi entiteti/value objects: nema implementiranih; planirani model je dokumentiran.
- Promijenjena pravila: nema runtime promjene.
- Migracije: nema.
- Backfill/data migration: nema.

## API promjene

Nema. Budući writeovi moraju biti owner-only, CSRF-zaštićeni, bounded i koristiti
`expectedDocumentRowVersion`; konkretne rute pripadaju 7.2+.

## Frontend promjene

Nema. Phase 7.1 nema route, screen, component state ili visual gate.

## Security / authorization

Contract zaključava owner-only authoring, indistinguishable 404, bez Edit ovlasti za View/Use
grant, bez raw HTML/JS/remote URL-a, private Clean-only assete, bounded payload i sigurno
strukturirano logiranje bez sadržaja slajda.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `git diff --check` | PASS |
| Build/test/SQL/frontend/visual | N/A — DOCS_ONLY faza, nema runtime diffa |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build nije potreban za DOCS_ONLY promjenu
- [x] runtime testovi nisu potrebni za DOCS_ONLY promjenu
- [x] nema migracije
- [x] authorization/security granica je dokumentirana
- [x] dokumentacija je usklađena

## Arhitekturne odluke

- ADR-0026 — version-bound relational PresentationDocument with typed element payloads.

## Poznati rizici / tehnički dug

- schema i SQL constrainti moraju se tek implementirati i stvarno validirati u Phase 7.2+;
- browser rendering i deterministic package serializer zahtijevaju zasebne testove;
- importani PPTX ostaje binary Material dok zasebni conversion contract ne bude odobren.

## Otvorena pitanja

Nema blockera za Phase 7.2. AI provider/privacy/prompt/retention/model-improvement odluke ostaju
eksplicitni blocker samo za Phase 7.9.

## Točna početna točka za sljedeću fazu

Phase 7.2 smije implementirati PresentationDocument/Slide persistence i owner-only slide
add/delete/reorder/duplicate nad praznim slajdovima, uz Draft-only mutation, document rowversion,
same-version DB zaštitu i targeted stvarni SQL concurrency/integrity testove. Ne uvoditi content
element handlere prije 7.3 niti assessable/Knowledge/AI scope prije njihovih podfaza.
