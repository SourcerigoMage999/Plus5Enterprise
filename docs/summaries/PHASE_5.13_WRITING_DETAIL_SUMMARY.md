# Phase 5.13 — Writing detail summary

## Status

**FINAL LOCKED — odobreno 2026-09-27.** SA review odobrio je business/UI implementaciju,
architecture/security i visual acceptance bez dodatnih promjena koda ili contracta.

## Implementirano

- postojeći model-derived Knowledge detail potvrđen je za Writing area bez zasebne
  domene ili endpointa;
- Writing ostaje na istom ekranu i čuva Knowledge Model version boundary;
- odabrani skill/component red i desni detaljni panel ostaju sinkronizirani;
- skill → subskill hijerarhija koristi stvarni `ParentKnowledgeComponentId` model;
- component no-data ostaje različit od `0 %`;
- score, readiness, confidence, Evidence-chain count/weight, calculated-at, algorithm
  version i model code/version ostaju backend projections;
- dodan je ciljani Writing regression i stvarni desktop/mobile visual gate;
- dokumentirano je da dostavljeni Writing PNG zapravo prikazuje Speaking i služi samo za
  zajednički 2.5 layout.

## Očuvane odluke

- ownership, anonymous `401` i indistinguishable `404` ostaju na postojećem endpointu;
- model verzije se ne stapaju;
- Writing skills nisu frontend enum ni production seed;
- Teacher ne upisuje postotak, a Phase 5.13 ne uvodi generički assessment/Evidence write;
- jedan pisani rad može hraniti više komponenti samo kroz eksplicitni Evidence mapping;
- otvoreni tekst nije automatski objektivno ocijenjen bez zaključanog workflowa;
- osnovni PLUS 5 ne ovisi o AI analizi pisanog rada;
- trend, activities, recommendations, PDF i Lesson Builder nisu simulirani.

## Validation

- backend Release build: PASS, 0 warninga i 0 grešaka;
- backend/API regression: PASS, 177 passed / 37 opt-in SQL skipped;
- architecture testovi: PASS, 4/4;
- ciljani `StudentKnowledge.test.tsx`: PASS, 8/8;
- puni frontend suite: PASS, 70/70;
- TypeScript typecheck, frontend lint i production build: PASS;
- `dotnet format --verify-no-changes`: PASS;
- NuGet vulnerability audit: PASS, 0 poznatih ranjivosti;
- npm audit: PASS, 0 ranjivosti;
- stvarni SQL-backed Docker health: live 200, ready 200, frontend 200;
- non-root runtime: API UID 1654, frontend `nginx`;
- stvarni login + stvarni Knowledge API: PASS;
- visual acceptance 1536×1024 i 390×844: PASS;
- document overflow: 1536/1536 i 390/390;
- browser `pageerror`: 0;
- business writeovi tijekom capturea: 0.

SQL opt-in suite nije potreban za 5.13 jer nema promjene baze, migracije, queryja ni write
contracta. Stvarni SQL-backed API runtime potvrđen je Docker visual gateom. Production
frontend kod nije dupliciran: Phase 5.13 koristi generički model-derived detail i dodaje
Writing-specific regression/evidence bez hardkodiranja kataloga.

## Self-review

- [x] nema hardkodiranog Writing kataloga, vještina ni production seeda
- [x] nema assessment kontrola bez owner-scoped source workflowa i rubrike
- [x] nema client-side score/trend/recommendation formule
- [x] nema pohrane teksta ni AI analize bez privacy/provenance contracta
- [x] model verzije ostaju odvojene i no-data nije nula
- [x] postojeći server ownership boundary nije oslabljen
- [x] native kontrole, focus i selected state ostaju dostupni
- [x] skill hijerarhija i component no-data pokriveni su regression testom
- [x] stvarni browser evidence nema lažne API podatke ni business write
- [x] desktop/mobile dokument nema horizontalni overflow
- [x] canonical asset mismatch je eksplicitno dokumentiran
- [x] nema backend, schema, migration ni package promjene

## Datoteke

- `docs/WRITING_DETAIL.md`
- `docs/ROADMAP.md`
- `docs/visual-acceptance/README.md`
- `docs/summaries/PHASE_5.13_WRITING_DETAIL_SUMMARY.md`
- `frontend/tests/StudentKnowledge.test.tsx`
- `frontend/tests/visual/phase513.mjs`
- `docs/visual-acceptance/phase-5.13/*`

## Scope koji nije implementiran

Nema nove baze/migracije/API-ja, assessment writea, pohrane ili prikaza učeničkog rada,
AI analize, hardkodiranih Writing skills, overall scorea, školske ocjene, probabilityja,
trenda, activity feeda, recommendations, PDF-a, Lesson Builder handoffa ni ručnog
overridea.

## Blockeri

Nema otvorenog business, tehničkog ni review blockera unutar read-only Phase 5.13 scopea.
Phase 5.13 je FINAL LOCKED. Budući Writing assessment write, display provenance i AI
analiza zahtijevaju zasebne source workflow, storage/privacy i Teacher-confirmation
contracte te nisu blockeri za ovaj prikaz. Dostavljeni Writing PNG ne pruža zasebnu
Writing semantiku, ali je dovoljno upotrebljiv za zajednički 2.5 layout; ta je granica
eksplicitno evidentirana u visual acceptanceu.
