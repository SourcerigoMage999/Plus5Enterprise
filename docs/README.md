# PLUS 5 — AI Development Documentation Pack

Ovaj direktorij je pripremljen kao **izvršna dokumentacija za senior AI arhitekta/programera** koji PLUS 5 razvija fazu po fazu.

## Obavezni redoslijed čitanja prije svake implementacije

1. `PROJECT_RULES.md`
2. `AI_DEVELOPER_SYSTEM_PROMPT.md`
3. `PRODUCT_SCOPE.md`
4. `DOMAIN_GLOSSARY.md`
5. `ARCHITECTURE_BASELINE.md`
6. relevantni tehnički standardi:
   - `DATABASE_DESIGN_STANDARD.md`
   - `BACKEND_ENGINEERING_STANDARD.md`
   - `API_CONVENTIONS.md` nakon Phase 1.3 za svaki API endpoint
   - `OBSERVABILITY.md` nakon Phase 1.4 za svaki backend log, trace i metric enrichment
   - `FRONTEND_ENGINEERING_STANDARD.md`
   - `FRONTEND_FOUNDATION.md` nakon Phase 1.5 za svaki frontend route, app-shell ili design-token zahvat
   - `SECURITY_ENGINEERING_STANDARD.md`
   - `AUTHENTICATION_REQUIREMENTS.md` + `AUTHENTICATION_ARCHITECTURE.md` za Phase 1.6 i svaki kasniji auth/authorization zahvat
   - `CORE_TEACHING_FOUNDATION.md` nakon Phase 2.1 za Program, SchoolGrade, ProficiencyLevel i Curriculum granice
   - `STUDENT_FOUNDATION.md` nakon Phase 2.2 za Student, Guardian, status, organizaciju i arhiviranje
   - `GROUP_FOUNDATION.md` nakon Phase 2.3 za Group, capacity, membership, ownership i concurrency granice
   - `SCHEDULING_FOUNDATION.md` nakon Phase 2.4 za Session, recurrence, location, conflict i series-change granice
   - `DOCKER_DEPLOYMENT_STANDARD.md`
   - `TESTING_QUALITY_STANDARD.md`
7. `SCREEN_SPEC_STATUS.md`
8. `ROADMAP.md`
9. relevantne datoteke iz `source_specs/`
10. `DECISION_LOG.md`
11. zadnji dovršeni phase summary iz `summaries/`
12. prije completiona `ENGINEERING_CHECKLIST.md`

## Najvažnije pravilo

**Dokumentacija je source of truth. Kod nije source of truth.** Ako se kod i dokumentacija razlikuju, implementacija se zaustavlja na granici kontradikcije i dokumentacija se prvo mora razjasniti/izmijeniti.

## Kako se radi projekt

- implementira se samo jedna ROADMAP faza/podfaza odjednom
- ne preskaču se faze
- ne implementira se budući scope “usput”
- ne izmišljaju se nedokumentirana poslovna pravila
- nakon svake faze obavezni su build, testovi, self-review i phase summary
- novi arhitekturni izbor mora se zapisati u `DECISION_LOG.md`
- nejasnoća ili rupa u specifikaciji ide u `OPEN_QUESTIONS.md`

## Status izvora

Teacher source je 2026-09-01 proširen s 54 nova `source_specs` dokumenta. Detaljni screen/lifecycle snapshotovi sada postoje za Materijale 4.1 i 4.4–4.5, Lesson Builder, PLUS 5 Ploču, Povijest sati, Domaće zadaće, Poruke, Izvještaje, Financije, Postavke, Centar obavijesti i Profil/account.

`source_specs/MASTER_SITEMAP_TEACHER.md` daje cross-module pregled, a `source_specs/DOCUMENTATION_BACKLOG.md` navodi domenske contracte i završne audite koji još nisu zaključani. Detaljni screen source ne uklanja ROADMAP gateove za Knowledge/Evidence, storage, permissions, metrics/privacy/export, finance, notifications ili MVP rez.

Full-platform refresh od 2026-09-04 dodaje studentsku aplikaciju, DS-001/UI kit, detaljni
DRAFT katalog KB-001–KB-025, teacher master sitemap C i bazne cross-role specifikacije.
Prije rada s tim scopeom obavezno pročitati `SOURCE_PACKAGE_AUDIT_2026_09_04.md` i četiri
nova source-derived dokumenta navedena u `DOCUMENTATION_MANIFEST.md`. Novi source ne
mijenja postojeći Teacher-only auth niti automatski zatvara otvorene domain/security gateove.
