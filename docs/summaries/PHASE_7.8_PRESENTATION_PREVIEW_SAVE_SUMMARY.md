# Phase 7.8 — Presentation preview and save flow

## Status

**FINAL LOCKED — odobreno 2026-10-24.**

## Implementirano

- owner-scoped review API i responsive `/materials/{materialId}/presentation/review` ekran;
- stvarni version-bound metadata, pedagoške reference, slajdovi, elementi i content counts;
- read-only slide canvas, thumbnails i full-presentation preview;
- server-derived publish blocker lista i disabled publish dok graph nije valjan;
- Teacher-only publish endpoint s CSRF, bounded body, rate limitom i dvostrukim optimistic
  concurrency contractom;
- deterministički server-generated private ZIP manifest i SHA-256;
- serializable, atomska Draft → Active aktivacija uz supersession prethodne Active verzije;
- storage compensation na concurrency/failure putu i best-effort cleanup zamijenjenog Draft
  objecta;
- editor `Nastavi` akcija dostupna samo kada je autosave stanje sigurno `saved`.

## Sačuvane odluke

- relacijski Presentation graph ostaje canonical; ZIP je derived package;
- Active i Superseded snapshoti ostaju nepromjenjivi;
- publish ponovno provjerava Quick Check mapping i same-version Clean media assete;
- browser ne dobiva storage key, checksum, bucket ni public/external URL;
- preview/publish ne stvara StudentAttempt, EvidenceEvent ni mastery/readiness promjenu;
- nema client-side exporta, javnog playera ni AI assistance scopea.

## Validacija

- backend Release build: PASS — 0 warninga i 0 grešaka u targeted build/test gateu;
- targeted backend/API/storage: PASS — 21/21;
- targeted frontend review/publish: PASS — 2/2;
- targeted .NET format: PASS;
- frontend lint, TypeScript i production build: PASS;
- stvarni runtime publish: PASS — real Teacher login, API, SQL, S3-compatible private
  storage i Clean package; review `200` → publish `200` → detail `200` → bivši Draft review
  `404`;
- visual acceptance: PASS — jedan desktop 1536×1024 i jedan mobile 390×844 viewport,
  canonical usporedba, stvarni spremljeni slide sadržaj, `scrollWidth == clientWidth`, bez
  capture writeova ili browser/page grešaka;
- SQL/migration/EF drift: N/A — nema schema promjene;
- architecture: N/A — dependency granice nisu promijenjene;
- full regression: N/A — targeted Phase 7.8 validacija je dovoljna;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni; postojeći transitive
  crypto advisory ostaje zaseban maintenance finding;
- Docker infrastructure validation: N/A — Dockerfile, Compose, startup i runtime identity
  nisu promijenjeni. Postojeći SQL/S3/ClamAV servisi korišteni su samo za stvarni feature
  runtime evidence.

## Dokumentacija

- `PRESENTATION_PREVIEW_SAVE.md`;
- `PRESENTATION_SLIDE_DOCUMENT_MODEL.md`;
- `DECISION_LOG.md` / ADR-0026 Phase 7.8 primjena;
- `ROADMAP.md`;
- `visual-acceptance/phase-7.8/README.md`.

## Scope namjerno nije implementiran

- public share/presentation URL i anonymous playback;
- PPTX/PDF/video export i client-side package generation;
- publish approval, collaborative publish i in-place Active editing;
- Student delivery, response runtime, Attempt, Evidence i grading;
- Phase 7.9 AI assistance.

## Review handoff

Kod, targeted provjere, stvarni runtime publish i minimalni canonical visual acceptance su
dovršeni. Solution Architect odobrio je završni lock 2026-10-24. Phase 7.9 ostaje BLOCKED
do zasebne AI provider/privacy/prompt/data-boundary odluke.
