# Phase 7.7 — Presentation autosave/version safety

## Status

**FINAL LOCKED — odobreno 2026-10-23.**

## Implementirano

- 800 ms debounce za valjane promjene odabranog Presentation elementa;
- manualni `Spremi nacrt` i `Spremi sada` flush istog autosave flowa;
- saved/dirty/saving/error/conflict status u headeru i property panelu;
- tab-scoped `sessionStorage` recovery vezan uz exact MaterialVersion, element i bazni potpis;
- retry nakon mrežne/server greške bez gubitka lokalnog unosa;
- `409 concurrency_conflict` UX koji zaustavlja autosave, čuva lokalni unos i ne radi
  last-write-wins;
- `beforeunload` upozorenje dok postoji nespremljena promjena;
- privremeno zaključavanje drugih write akcija dok element nema sigurno `saved` stanje.

## Sačuvane odluke

- `PresentationDocument.RowVersion` ostaje jedini concurrency authority za editor writeove;
- backend owner/Draft/CSRF/rate-limit/payload zaštite nisu premještene na klijent;
- postojeći typed element i Quick Check endpointi ponovno se koriste bez novog generic patcha;
- structural slide/element writeovi ostaju immediate i atomski;
- recovery nije persistent history, offline queue ni collaborative merge.

## Validacija

- targeted frontend `PresentationEditor.test.tsx`: PASS — 9/9;
- autosave debounce/current-rowversion: PASS;
- conflict/no-overwrite i tab recovery: PASS;
- frontend lint: PASS — 0 warninga;
- TypeScript: PASS;
- production build: PASS;
- visual acceptance: PASS — stvarni Teacher login, aktualni API i Docker SQL, jedan desktop
  1536×1024 i jedan mobile 390×844 viewport, spremljeni autosave status, bez document
  overflowa, capture writeova ili browser/page grešaka;
- backend/SQL/EF drift/architecture: N/A — frontend-only faza, postojeći API i schema nisu
  promijenjeni;
- full regression: N/A — targeted phase validation je dovoljna;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni;
- Docker infrastructure validation: N/A — Dockerfile, Compose, startup i runtime identity
  nisu promijenjeni.

## Dokumentacija

- `PRESENTATION_AUTOSAVE_VERSION_SAFETY.md`;
- `PRESENTATION_SLIDE_DOCUMENT_MODEL.md` dependency ostaje nepromijenjen;
- `DECISION_LOG.md` / ADR-0026 Phase 7.7 primjena;
- `visual-acceptance/phase-7.7/README.md`;
- `ROADMAP.md`.

## Scope namjerno nije implementiran

- undo/redo i server-side revision history;
- collaborative merge/presence/locking;
- offline-first queue i cross-device recovery;
- Phase 7.8 preview, package, activation/publish i export;
- Phase 7.9 AI assistance.

## Review handoff

Kod, dokumentacija, targeted provjere i minimalni stvarni visual acceptance su dovršeni.
Solution Architect odobrio je završni lock 2026-10-23. Phase 7.8 nije započeta.
