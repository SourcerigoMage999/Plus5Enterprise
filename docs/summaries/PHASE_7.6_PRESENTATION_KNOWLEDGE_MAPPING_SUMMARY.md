# Phase 7.6 — Knowledge Component mapping per question

## Status

**FINAL LOCKED — odobreno 2026-10-21.**

## Implementirano

- owner-scoped, paginirani katalog `Published + Active + leaf` Knowledge Component kandidata;
- bounded search po nazivu, području te KnowledgeModel code/version vrijednostima;
- atomski create/update Quick Check mappinga kroz postojeći
  `AssessableTaskVersionKnowledgeComponent` model;
- server-side validacija leaf/status/version pravila i neutralni invalid-target API rezultat;
- očuvanje i eksplicitni prikaz postojećih Retired/Deprecated povijesnih targeta uz zabranu
  njihova novog dodavanja;
- responsive property-panel picker sa selected stateom, remove akcijom te
  loading/error/retry/empty stanjima;
- version-explicit prikaz KnowledgeArea i KnowledgeModel code/version konteksta.

## Sačuvane odluke

- mapping pripada TaskVersionu, ne slajdu, elementu ni cijelom Materialu;
- Draft Quick Check smije privremeno biti bez targeta; Phase 6.5 activation invariant i dalje
  zahtijeva najmanje jedan mapping prije aktivacije;
- nema automatskog mapiranja, weighta, parent expansiona ni spajanja model verzija;
- picker nije security authority; backend ponovno provjerava ownership i target integrity;
- mapping write ne stvara Attempt, EvidenceEvent ni mastery/readiness promjenu.

## Validacija

- backend Release build: PASS — 0 warninga i 0 grešaka;
- targeted API/security testovi: PASS — 5/5;
- targeted stvarni SQL query/atomic mapping test: PASS — 1/1;
- frontend targeted Presentation Editor testovi: PASS — 7/7;
- frontend lint, TypeScript i production build: PASS;
- visual acceptance: PASS — jedan stvarni desktop 1536×1024 i jedan mobile 390×844 feature
  state, stvarni Teacher login/API/SQL, odabrani target i otvoreni candidate picker, bez
  document overflowa, browser/page grešaka ili Presentation business writeova tijekom
  capturea;
- EF migration/model drift: N/A — Phase 7.6 ne mijenja schema model;
- architecture tests: N/A — dependency granice nisu promijenjene;
- full regression: N/A — targeted Phase 7.6 validacija je dovoljna;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni; postojeći transitive
  crypto advisory ostaje zaseban već evidentiran maintenance finding;
- Docker infrastructure validation: N/A — Dockerfile, Compose, startup i runtime identity nisu
  promijenjeni.

## Dokumentacija

- `PRESENTATION_KNOWLEDGE_MAPPING.md`;
- `PRESENTATION_QUICK_CHECK.md`;
- `ASSESSABLE_TASK_METADATA.md`;
- `PERSISTENCE.md`;
- `DECISION_LOG.md` / ADR-0025 i ADR-0026 Phase 7.6 primjena;
- `ROADMAP.md`;
- `visual-acceptance/phase-7.6/README.md`.

## Scope namjerno nije implementiran

- Phase 7.7 autosave i recovery;
- Phase 7.8 preview, activation/publish i package/export;
- AI/automatic mapping, weighting i parent expansion;
- Student response runtime, Attempt, Evidence emission i analytics.
