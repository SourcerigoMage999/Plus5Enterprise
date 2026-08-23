# PLUS 5 — AI Development Documentation Pack

Ovaj direktorij je pripremljen kao **izvršna dokumentacija za senior AI arhitekta/programera** koji PLUS 5 razvija fazu po fazu.

## Obavezni redoslijed čitanja prije svake implementacije

1. `PROJECT_RULES.md`
2. `AI_DEVELOPER_SYSTEM_PROMPT.md`
3. `PRODUCT_SCOPE.md`
4. `ARCHITECTURE_BASELINE.md`
5. relevantni tehnički standardi:
   - `DATABASE_DESIGN_STANDARD.md`
   - `BACKEND_ENGINEERING_STANDARD.md`
   - `FRONTEND_ENGINEERING_STANDARD.md`
   - `SECURITY_ENGINEERING_STANDARD.md`
   - `DOCKER_DEPLOYMENT_STANDARD.md`
   - `TESTING_QUALITY_STANDARD.md`
6. `SCREEN_SPEC_STATUS.md`
7. `ROADMAP.md`
8. relevantne datoteke iz `source_specs/`
9. `DECISION_LOG.md`
10. zadnji dovršeni phase summary iz `summaries/`
11. prije completiona `ENGINEERING_CHECKLIST.md`

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
