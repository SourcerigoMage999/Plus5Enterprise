# Phase 9.3 — Versioned Knowledge Block catalog and applicability rules

## Status

**FINAL LOCKED — odobreno 2026-10-30.**

## Cilj faze

Zaključati canonical KB-001–KB-025 roster, source-maturity publish granicu, version lifecycle,
exact references i deterministic typed applicability prije Lesson Builder runtimea.

## Implementirano

- svih 25 codeova definirano je kao immutable System-owned stable roots;
- KB-001–KB-005 početno su Published v1, KB-006–KB-025 Draft v1;
- definiran je Draft/Published/Retired version i Active/Retired root lifecycle;
- Published/Retired snapshot, historical readability i controlled catalog deployment su
  zaključani;
- LessonActivity/ActivityTemplateVersion koriste optional exact KB version, najviše jedan
  primary KB po aktivnosti;
- duration envelope odvojen je od canonical Teacher-set activity durationa;
- hard eligibility odvojen je od overrideable recommendation/warning pravila;
- strukturirani participant/grade/proficiency/goal/knowledge/homework/assessment/sequence/
  composition rule skupovi koriste tri-state evaluation i reason codeove;
- KB-024/025 composition, max depth 2, acyclic graph i Ready duration invariant su definirani;
- Phase 9.2 privacy precedence prenesena je na legacy KB draftove;
- ADR-0030 bilježi trajnu architecture/domain odluku.

## Namjerno nije implementirano

- domain klase, EF konfiguracija, migration, SQL catalog seed ili admin tooling;
- Goal Selection, activity picker ili drugi Lesson Builder UI;
- AI/provider runtime i automatic plan generation;
- balance engine, automatic duration correction ili reorder;
- Attempt, Evidence, Session runtime ili auto-KC mapping.

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `docs/KNOWLEDGE_BLOCK_CATALOG.md` | added | canonical Phase 9.3 contract |
| `docs/DECISION_LOG.md` | changed | ADR-0030 |
| `docs/OPEN_QUESTIONS.md` | changed | zatvoren Knowledge Block catalog/applicability gate |
| `docs/ROADMAP.md` | changed | Phase 9.3 status i Phase 9.4 handoff |
| `docs/SCREEN_SPEC_STATUS.md` | changed | KB source maturity i contract status |
| `docs/summaries/PHASE_9.3_KNOWLEDGE_BLOCK_CATALOG_SUMMARY.md` | added | phase evidence i handoff |

## Domain / database promjene

- Novi runtime entiteti/value objecti: nema; model je dokumentiran.
- Promijenjena runtime pravila: nema.
- Migracije/seed/backfill: nema.
- Budući DB invarianti i explicit deployment granica navedeni su u contractu.

## API promjene

Nema. Obični Teacher/Lesson Builder API neće mutirati System katalog.

## Frontend promjene

Nema. Phase 9.3 nema ekran ni visual gate.

## Security / authorization

System catalog nije Teacher-editable. Applicability ne koristi sensitive/demographic/Guardian
podatke, raw Evidence ili free-form notes; FINAL LOCKED Phase 9.2 granica ima prednost nad
legacy draft sourceom.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `git diff --check` | PASS |
| Build/test/SQL/frontend/visual | N/A — DOCS_ONLY faza |
| Full regression | N/A — nema runtime ni shared-contract diffa |
| Dependency audit | N/A — dependency graph nije promijenjen |
| Docker infrastructure validation | N/A — infrastructure unchanged |

## Self-review

- [x] scope nije proširen izvan Phase 9.3
- [x] draft source nije promoviran u production bez eksplicitne maturity odluke
- [x] Phase 9.1/9.2 FINAL LOCKED contracti nisu promijenjeni
- [x] recommendation nije pretvoren u rigidni validator
- [x] runtime/seed/UI/AI nisu implementirani preuranjeno

## Arhitekturne odluke

- ADR-0030 — versioned System Knowledge Block catalog with typed applicability.

## Poznati rizici / tehnički dug

- KB-006–KB-025 ostaju Draft i zahtijevaju zaseban pedagogical approval prije publisha.
- Persistence implementation mora stvarno dokazati filtered uniqueness, snapshot
  immutability, exact-version reference, acyclic graph i max-depth invariante.

## Otvorena pitanja

Nema blockera za Phase 9.4. Concrete KB version rule vrijednosti izvan zaključanog initial
publish rostera uvode se samo kroz odobreni catalog release, ne heuristikom.

## Točna početna točka za sljedeću fazu

Phase 9.4 smije implementirati Goal Selection workflow uz Teacher-owned Student XOR Group i
optional Session context iz 9.1. UI ne smije nuditi Draft KB-ove, direct-writeati AI output ili
preuranjeno implementirati 9.5 balance engine.
