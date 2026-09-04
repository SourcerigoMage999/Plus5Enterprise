# Documentation manifest

## Status

**MERGED PROJECT MANIFEST — 2026-09-04**

Ovaj manifest opisuje authoritative projektni `docs` paket nakon selektivnih teacher i
full-platform source mergeova. Vanjski snapshot nije samostalna zamjena za projektne dokumente.

## Obavezni projektni source-of-truth

- product i workflow: `PRODUCT_SCOPE.md`, `DOMAIN_GLOSSARY.md`, `PROJECT_RULES.md`, `ROADMAP.md`, `SCREEN_SPEC_STATUS.md`
- arhitektura i odluke: `ARCHITECTURE_BASELINE.md`, `DECISION_LOG.md`, `OPEN_QUESTIONS.md`
- security/auth: `SECURITY_ENGINEERING_STANDARD.md`, `AUTHENTICATION_REQUIREMENTS.md`, `AUTHENTICATION_ARCHITECTURE.md`
- engineering standardi: `DATABASE_DESIGN_STANDARD.md`, `BACKEND_ENGINEERING_STANDARD.md`, `FRONTEND_ENGINEERING_STANDARD.md`, `DOCKER_DEPLOYMENT_STANDARD.md`, `TESTING_QUALITY_STANDARD.md`, `ENGINEERING_CHECKLIST.md`
- application foundation: `CONFIGURATION.md`, `PERSISTENCE.md`, `API_CONVENTIONS.md`, `OBSERVABILITY.md`, `FRONTEND_FOUNDATION.md`
- domain/feature contracti: `CORE_TEACHING_FOUNDATION.md`, `STUDENT_FOUNDATION.md`, `GROUP_FOUNDATION.md`, `SCHEDULING_FOUNDATION.md`, `STUDENT_LIST.md`

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

- `summaries/` sadrži dovršene phase handoff zapise i ne smije se zamijeniti starijim statusima iz vanjskih paketa.
- `visual-acceptance/` sadrži canonical visual-acceptance dokaze za dovršene business UI faze.

## Merge pravilo

Novi source može proširiti budući feature scope i razriješiti dokumentacijski gate, ali ne smije retroaktivno poništiti zaključani tehnički contract, Accepted ADR, dovršeni ROADMAP status ili phase evidence bez eksplicitne odluke vlasnika proizvoda.

DS-001 je obavezan input za budući visual acceptance, ali razlike prema postojećim
tokenima prvo prolaze Phase 1.7 alignment audit. Student source ne mijenja postojeći
Teacher-only authentication contract; implementacija je blokirana do cross-role odluka.
