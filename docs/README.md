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

Detaljna dokumentacija trenutno postoji za Radni stol, Učenike, Raspored te dio Materijala. Mapa ekrana definira i kasnije module, ali njihov detaljni business/UX opis još nije potpun. Zbog toga kasnije faze imaju dokumentacijske gateove prije implementacije.
