# Phase 5.12 — Speaking detail summary

## Status

**FINAL LOCKED — 2026-09-27.** SA review odobrio je business/UI implementaciju,
architecture/security i visual acceptance bez dodatnih promjena koda ili contracta.

## Implementirano

- postojeći model-derived Knowledge detail potvrđen je za Speaking area bez zasebne
  domene ili endpointa;
- Speaking ostaje na istom ekranu i čuva Knowledge Model version boundary;
- odabrani skill/component red i desni detaljni panel ostaju sinkronizirani;
- skill → subskill hijerarhija koristi stvarni `ParentKnowledgeComponentId` model;
- component no-data ostaje različit od `0 %`;
- score, readiness, confidence, Evidence-chain count/weight, calculated-at, algorithm
  version i model code/version ostaju backend projections;
- dodan je ciljani Speaking regression i stvarni desktop/mobile visual gate.

## Očuvane odluke

- ownership, anonymous `401` i indistinguishable `404` ostaju na postojećem endpointu;
- model verzije se ne stapaju;
- Speaking skills nisu frontend enum ni production seed;
- `Samopouzdanje` nije psihološki score; mjerljiva semantika je opaživa
  `Samostalnost u govoru`;
- Teacher ne upisuje postotak, a Phase 5.12 ne uvodi generički assessment/Evidence write;
- jedna aktivnost može hraniti više komponenti samo kroz eksplicitni Evidence mapping;
- osnovni PLUS 5 ne ovisi o AI analizi govora;
- trend, activities, recommendations, PDF i Lesson Builder nisu simulirani.

## Validation

- backend Release build: PASS, 0 warninga i 0 grešaka;
- backend/API regression u digest-pinnanom .NET 10 SDK containeru: PASS,
  177 passed / 37 opt-in SQL skipped;
- architecture testovi: PASS, 4/4;
- ciljani `StudentKnowledge.test.tsx`: PASS, 7/7;
- puni frontend suite: PASS, 69/69;
- TypeScript typecheck, frontend lint i production build: PASS;
- `dotnet format --verify-no-changes`: PASS;
- NuGet vulnerability audit: PASS, 0 poznatih ranjivosti;
- npm audit: PASS, 0 ranjivosti;
- Docker start/migracije i health gate: PASS;
- non-root runtime: API UID 1654, frontend `nginx`;
- stvarni login + stvarni Knowledge API: PASS;
- visual acceptance 1536×1024 i 390×844: PASS;
- document overflow: 1536/1536 i 390/390;
- browser `pageerror`: 0;
- business writeovi tijekom capturea: 0.

SQL opt-in suite nije potreban za 5.12 jer nema promjene baze, migracije, queryja ni write
contracta. Stvarni SQL-backed API runtime potvrđen je Docker visual gateom. Production
frontend kod nije dupliciran: Phase 5.12 koristi generički model-derived detail i dodaje
Speaking-specific regression/evidence bez hardkodiranja kataloga.

## Self-review

- [x] nema hardkodiranog Speaking kataloga, vještina ni production seeda
- [x] nema nedokazivog psihološkog `Samopouzdanje` scorea
- [x] nema assessment kontrola bez owner-scoped source workflowa
- [x] nema client-side score/trend/recommendation formule
- [x] nema audio recording/transcript/AI featurea bez contracta
- [x] model verzije ostaju odvojene i no-data nije nula
- [x] postojeći server ownership boundary nije oslabljen
- [x] native kontrole, focus i selected state ostaju dostupni
- [x] skill hijerarhija i component no-data pokriveni su regression testom
- [x] stvarni browser evidence nema lažne API podatke ni business write
- [x] desktop/mobile dokument nema horizontalni overflow
- [x] nema backend, schema, migration ni package promjene

## Datoteke

- `docs/SPEAKING_DETAIL.md`
- `frontend/tests/StudentKnowledge.test.tsx`
- `frontend/tests/visual/phase512.mjs`
- `docs/visual-acceptance/phase-5.12/*`

## Scope koji nije implementiran

Nema nove baze/migracije/API-ja, assessment writea, audio storagea, transkripta, AI analize,
hardkodiranih Speaking skills, overall scorea, školske ocjene, probabilityja, trenda,
activity feeda, recommendations, PDF-a, Lesson Builder handoffa ni ručnog overridea.

## Blockeri

Nema otvorenog business, tehničkog ni review blockera unutar read-only Phase 5.12 scopea.
Phase 5.12 je FINAL LOCKED. Budući Speaking assessment write zahtijeva zaseban source
workflow contract i nije blocker za ovaj prikaz.
