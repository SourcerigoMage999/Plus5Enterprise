# Phase 9.1 — Lesson Plan Activity and Knowledge Block formal domain contract

## Status

**FINAL LOCKED — odobreno 2026-10-30.**

## Cilj faze

Zatvoriti formalni Lesson Builder domain gate prije persistence, API i UI implementacije,
bez preuranjenog AI, Knowledge Block kataloga ili Lesson Session runtime scopea.

## Implementirano

- definiran je stabilni `LessonPlan` root i immutable versioned Draft/Ready/Superseded model;
- zaključani su Student XOR Group, optional same-context Session i Teacher ownership;
- edit/restore stvaraju novi Draft, a root/version imaju odvojeni optimistic concurrency;
- definiran je versioned System/Teacher `ActivityTemplate` i snapshot instanciranje u
  `LessonActivity`;
- odvojeni su KnowledgeBlock, KnowledgeComponent, ActivityTemplate i LessonActivity;
- zaključani su activity order, duration, duplicate/remove i archive/delete boundaryji;
- definirane su exact MaterialVersion i AssessableTaskVersion reference;
- planirani Quick Check odvojen je od StudentAttempta i EvidenceEventa;
- ADR-0028 bilježi trajnu arhitekturnu odluku.

## Namjerno nije implementirano

- domain klase, EF konfiguracija, migracija ili SQL tablice;
- API, application use caseovi, frontend route ili visual acceptance;
- AI provider/input/privacy/explainability/audit iz Phase 9.2;
- versioned KB-001–KB-025 katalog i applicability pravila iz Phase 9.3;
- LessonSessionActivity, Attempt, Evidence, attendance ili Board runtime;
- automatski Material/Task upgrade ili Knowledge mapping.

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `docs/LESSON_BUILDER_DOMAIN.md` | added | canonical Phase 9.1 contract |
| `docs/DECISION_LOG.md` | changed | ADR-0028 |
| `docs/DOMAIN_GLOSSARY.md` | changed | canonical Lesson Builder pojmovi |
| `docs/OPEN_QUESTIONS.md` | changed | zatvoren Phase 9.1 gate, očuvani 9.2/9.3 gateovi |
| `docs/ROADMAP.md` | changed | Phase 9.1 status i acceptance |
| `docs/summaries/PHASE_9.1_LESSON_BUILDER_DOMAIN_SUMMARY.md` | added | phase evidence i handoff |

## Domain / database promjene

- Novi runtime entiteti/value objecti: nema; model je dokumentiran.
- Promijenjena runtime pravila: nema.
- Migracije i backfill: nema.
- Budući persistence invarianti izričito su navedeni u contractu za 9.4–9.9.

## API promjene

Nema. Budući endpointi moraju biti Teacher-only, owner-scoped, bounded i concurrency-safe.

## Frontend promjene

Nema. Phase 9.1 nema ekran ni visual gate.

## Security / authorization

Contract zaključava server-side Teacher ownership, same-owner participant/Session reference,
exact immutable content reference i zabranu preuranjenog AI pristupa Student/Group podacima.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `git diff --check` | PASS |
| Build/test/SQL/frontend/visual | N/A — DOCS_ONLY faza |
| Full regression | N/A — nema runtime ni shared-contract diffa |
| Dependency audit | N/A — dependency graph nije promijenjen |
| Docker infrastructure validation | N/A — infrastructure unchanged |

## Self-review

- [x] scope nije proširen izvan Phase 9.1
- [x] SA odluka prenesena je bez novih business pretpostavki
- [x] runtime kod i FINAL LOCKED faze nisu mijenjani
- [x] Phase 9.2 i 9.3 gateovi ostaju eksplicitni
- [x] dokumentacija je međusobno usklađena

## Arhitekturne odluke

- ADR-0028 — versioned Lesson Plan and snapshot-based activity planning.

## Poznati rizici / tehnički dug

- Persistence invarianti i SQL zaštita moraju se tek implementirati i stvarno validirati u
  odgovarajućim Phase 9.4–9.9 write podfazama.
- KnowledgeBlockVersion FK ne uvodi se prije formalnog Phase 9.3 kataloga.

## Otvorena pitanja

Phase 9.2 ostaje BLOCKED dok Solution Architect ne zaključa Lesson Builder AI input privacy,
explainability, retention, audit, group fairness, provider boundary i model-improvement
ponašanje. Phase 9.3 čeka 9.2.

## Točna početna točka za sljedeću fazu

Otvoriti Phase 9.2 kao zasebni DOCS_ONLY contract. Ne prenositi automatski Phase 7.9
Presentation AI odluke na Lesson Builder i ne implementirati provider pozive prije zatvaranja
Student/Group/privacy gatea.
