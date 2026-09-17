# Documentation manifest

## Status

**MERGED PROJECT MANIFEST — ažurirano 2026-09-16**

Ovaj manifest opisuje authoritative projektni `docs` paket nakon selektivnih teacher i
full-platform source mergeova. Vanjski snapshot nije samostalna zamjena za projektne dokumente.

## Obavezni projektni source-of-truth

- product i workflow: `PRODUCT_SCOPE.md`, `DOMAIN_GLOSSARY.md`, `PROJECT_RULES.md`, `ROADMAP.md`, `SCREEN_SPEC_STATUS.md`
- arhitektura i odluke: `ARCHITECTURE_BASELINE.md`, `DECISION_LOG.md`, `OPEN_QUESTIONS.md`
- security/auth: `SECURITY_ENGINEERING_STANDARD.md`, `AUTHENTICATION_REQUIREMENTS.md`, `AUTHENTICATION_ARCHITECTURE.md`
- engineering standardi: `DATABASE_DESIGN_STANDARD.md`, `BACKEND_ENGINEERING_STANDARD.md`, `FRONTEND_ENGINEERING_STANDARD.md`, `DOCKER_DEPLOYMENT_STANDARD.md`, `TESTING_QUALITY_STANDARD.md`, `ENGINEERING_CHECKLIST.md`
- application foundation: `CONFIGURATION.md`, `PERSISTENCE.md`, `API_CONVENTIONS.md`, `OBSERVABILITY.md`, `FRONTEND_FOUNDATION.md`
- domain/feature contracti: `CORE_TEACHING_FOUNDATION.md`, `CURRICULUM_HIERARCHY.md`, `KNOWLEDGE_COMPONENT_MODEL.md`, `STUDENT_FOUNDATION.md`, `GROUP_FOUNDATION.md`, `SCHEDULING_FOUNDATION.md`, `RECURRENCE_MATERIALIZATION.md`, `STUDENT_LIST.md`, `GROUP_LIST.md`, `GROUP_CREATION.md`, `GROUP_EDITING.md`, `SCHEDULE_CALENDAR.md`, `SESSION_DETAIL.md`, `SESSION_CREATION.md`, `SESSION_EDITING.md`

## Source snapshotovi

- `source_specs/` sadrži ranije 1.x–4.3 snapshotove i 54 nova teacher-source dokumenta dodana 2026-09-01.
- `source_specs/MASTER_SITEMAP_TEACHER.md` daje izvedeni cross-module sitemap.
- `source_specs/DOCUMENTATION_BACKLOG.md` navodi još nezaključane domenske dokumente, završni functional audit i MVP rez.
- `SOURCE_DOCUMENT_INDEX.md` bilježi podrijetlo i redoslijed source refresha.
- `SOURCE_PACKAGE_AUDIT_2026_09_04.md` bilježi selektivni merge studentske aplikacije,
  DS-001, Lesson Builder/KB kataloga, sitemap C i baznih cross-role dokumenata.
- Novi source-derived sažeci su `DESIGN_SYSTEM_DS001.md`,
  `LESSON_BUILDER_KNOWLEDGE_BLOCK_CATALOG.md`, `STUDENT_APPLICATION_SITEMAP.md` i
  `CROSS_ROLE_BASELINE.md`. Izvorni DRAFT/otvoreni statusi ne pretvaraju se u lock.

## Phase evidence

- [DESIGN_SYSTEM_ALIGNMENT.md](DESIGN_SYSTEM_ALIGNMENT.md) sadrži Phase 1.7 audit i
  prihvaćeni ADR-0014 mapping, canonical iznimke i obavezni regression gate.
- [PHASE_3.6_CREATE_GROUP_SUMMARY.md](summaries/PHASE_3.6_CREATE_GROUP_SUMMARY.md) i
  [phase-3.6 visual acceptance](visual-acceptance/phase-3.6/README.md) sadrže završni
  implementacijski i canonical desktop/mobile dokaz za kreiranje grupe.
- [PHASE_3.7_EDIT_GROUP_SUMMARY.md](summaries/PHASE_3.7_EDIT_GROUP_SUMMARY.md) i
  [phase-3.7 visual acceptance](visual-acceptance/phase-3.7/README.md) sadrže završni
  implementacijski i canonical desktop/mobile dokaz za uređivanje grupe.
- [PHASE_4.1_SCHEDULE_CALENDAR_SUMMARY.md](summaries/PHASE_4.1_SCHEDULE_CALENDAR_SUMMARY.md) i
  [phase-4.1 visual acceptance](visual-acceptance/phase-4.1/README.md) sadrže završni
  owner-scoped calendar, SQL-metrics i canonical week/day desktop/mobile dokaz.
- [PHASE_4.2_SESSION_DETAIL_SUMMARY.md](summaries/PHASE_4.2_SESSION_DETAIL_SUMMARY.md) i
  [phase-4.2 visual acceptance](visual-acceptance/phase-4.2/README.md) sadrže owner-scoped
  Session detail, temporalni roster i canonical desktop/mobile dokaz.
- [PHASE_4.3_SESSION_CREATION_SUMMARY.md](summaries/PHASE_4.3_SESSION_CREATION_SUMMARY.md) i
  [phase-4.3 visual acceptance](visual-acceptance/phase-4.3/README.md) sadrže atomski
  Session create, recurrence/conflict SQL gate i canonical desktop/mobile dokaz.
- [PHASE_4.4_SESSION_EDITING_SUMMARY.md](summaries/PHASE_4.4_SESSION_EDITING_SUMMARY.md) i
  [phase-4.4 visual acceptance](visual-acceptance/phase-4.4/README.md) sadrže Session edit/cancel,
  occurrence/future-series SQL gate i canonical desktop/mobile dokaz.
- [PHASE_4.5_RECURRENCE_SERIES_CONSISTENCY_SUMMARY.md](summaries/PHASE_4.5_RECURRENCE_SERIES_CONSISTENCY_SUMMARY.md)
  sadrži završnu recurrence/series preservation, lineage, bounded generation i concurrency
  regression matricu za Phase 4.
- [PHASE_4.6_RECURRENCE_MATERIALIZATION_REPLENISHMENT_SUMMARY.md](summaries/PHASE_4.6_RECURRENCE_MATERIALIZATION_REPLENISHMENT_SUMMARY.md)
  sadrži background replenishment, SQL lease, durable issue, idempotency/concurrency i runtime
  evidence kojim je Phase 4 završno zaključana.
- [PHASE_5.1_CURRICULUM_HIERARCHY_SUMMARY.md](summaries/PHASE_5.1_CURRICULUM_HIERARCHY_SUMMARY.md)
  sadrži domain/persistence contract, stvarni SQL hierarchy gate, migration evidence i puni
  regression audit za model-only curriculum hierarchy.
- [PHASE_5.2_KNOWLEDGE_COMPONENT_MODEL_SUMMARY.md](summaries/PHASE_5.2_KNOWLEDGE_COMPONENT_MODEL_SUMMARY.md)
  sadrži versioned Knowledge Model lifecycle, Area/component tree, Curriculum M:N, stvarni SQL
  integrity gate i puni regression audit.

- `summaries/` sadrži dovršene phase handoff zapise i ne smije se zamijeniti starijim statusima iz vanjskih paketa.
- `visual-acceptance/` sadrži canonical visual-acceptance dokaze za dovršene business UI faze.

## Merge pravilo

Novi source može proširiti budući feature scope i razriješiti dokumentacijski gate, ali ne smije retroaktivno poništiti zaključani tehnički contract, Accepted ADR, dovršeni ROADMAP status ili phase evidence bez eksplicitne odluke vlasnika proizvoda.

DS-001 je obavezan input za budući visual acceptance, ali razlike prema postojećim
tokenima prvo prolaze Phase 1.7 alignment audit. Student source ne mijenja postojeći
Teacher-only authentication contract; implementacija je blokirana do cross-role odluka.
