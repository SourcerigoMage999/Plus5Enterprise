# Phase 7.5 — Interactive question / Quick Check

## Status

**FINAL LOCKED — odobreno 2026-10-20.**

## Implementirano

- atomski version-bound `QUICK_CHECK`, stabilni `AssessableTask` i exact same-version
  `AssessableTaskVersion`;
- single-choice v1 contract s promptom, 2–8 server-keyed opcija `A`–`H`, jednim točnim
  odgovorom, Difficulty 1–5, EvidenceTypeom i fiksnim `MaxPoints = 1`;
- canonical assessment metadata samo na TaskVersionu; Presentation payload ostaje zatvoreni
  `{"schemaVersion":1}`;
- Teacher-only create/update endpointi s CSRF-om, bounded bodyjem, owner scopeom, postojećim
  rate limitom i optimistic concurrencyjem;
- atomski duplicate, delete i restore lifecycle za povezani Task/TaskVersion snapshot;
- SQL same-MaterialVersion, unique-link, authoring i element/task integrity zaštita s
  greškama `51246` i `51247`;
- responsive editor toolbar, Quick Check canvas card te property panel za prompt, odgovore,
  točan odgovor, Difficulty i EvidenceType.

## Sačuvane odluke

- Draft Quick Check smije privremeno biti bez Knowledge targeta, ali se MaterialVersion ne
  može aktivirati dok Phase 6.5 leaf mapping invariant nije zadovoljen;
- generic element endpoint ne može kreirati ili mijenjati `QUICK_CHECK`;
- browser ne određuje canonical option keys niti računa assessment semantiku;
- instructional `QUESTION` iz Phase 7.4 ostaje odvojen od procjenjivog Quick Checka;
- prikaz ili autoriranje ne stvara `StudentAttempt`, `EvidenceEvent` ni mastery/readiness
  promjenu.

## Validacija

- backend Release build: PASS — 0 warninga i 0 grešaka;
- targeted Phase 7.5 domain/API/security testovi: PASS — 16/16;
- targeted stvarni SQL migration/integrity test: PASS — 1/1;
- migration apply, idempotent migration script i EF model-drift provjera: PASS;
- frontend targeted Presentation Editor: PASS — 7/7;
- frontend lint, TypeScript i production build: PASS; postojeći informativni Vite chunk-size
  warning nije feature defect;
- visual acceptance: PASS — jedan stvarni desktop 1536×1024 i jedan mobile 390×844 feature
  state, bez globalnog overflowa, browser grešaka, auth bypassa, API interceptiona, DOM
  mutationa ili business writea tijekom capturea;
- full regression: N/A — targeted Phase 7.5 validacija je dovoljna;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni;
- Docker infrastructure validation: N/A — Dockerfile, Compose, startup i runtime identity nisu
  promijenjeni;
- architecture tests: N/A — dependency granice nisu promijenjene.

## Maintenance/security finding izvan Phase 7.5 scopea

Restore je prijavio postojeće advisoryje za transitive `System.Security.Cryptography.Pkcs`
i `System.Security.Cryptography.Xml`. Phase 7.5 ne mijenja dependency graph i finding nije
uveden ovim featureom, pa nije napravljen opportunistic dependency upgrade. Finding je
zabilježen u `OPEN_QUESTIONS.md`; zbog postojećeg warnings-as-errors pravila trenutačno
blokira fresh Docker image restore/build, ali ne mijenja rezultat targeted Phase 7.5
feature, SQL ni lokalnog runtime gatea.

## Dokumentacija

- `PRESENTATION_QUICK_CHECK.md`;
- `PRESENTATION_SLIDE_DOCUMENT_MODEL.md`;
- `ASSESSABLE_TASK_METADATA.md`;
- `PERSISTENCE.md`;
- `DECISION_LOG.md` / ADR-0025 i ADR-0026 Phase 7.5 primjena;
- `ROADMAP.md`;
- `visual-acceptance/phase-7.5/README.md`.

## Scope namjerno nije implementiran

- Phase 7.6 Knowledge Component mapping;
- Phase 7.7 autosave i recovery;
- Phase 7.8 preview, activation/publish i package/export;
- Student response runtime, Attempt i eksplicitni Evidence emission;
- analytics, AI generiranje i AI evaluacija.
