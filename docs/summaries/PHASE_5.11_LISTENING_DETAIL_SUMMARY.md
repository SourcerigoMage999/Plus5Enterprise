# Phase 5.11 — Listening detail summary

## Status

**FINAL LOCKED — 2026-09-27.** SA review odobrio je business/UI implementaciju,
architecture/security i visual acceptance bez dodatnih promjena koda ili contracta.

## Implementirano

- postojeći model-derived Knowledge detail potvrđen je za Listening area bez zasebne
  domene ili endpointa;
- Listening ostaje na istom ekranu i čuva Knowledge Model version boundary;
- odabrani skill/component red i desni detaljni panel ostaju sinkronizirani;
- skill → subskill hijerarhija koristi stvarni `ParentKnowledgeComponentId` model;
- component no-data ostaje različit od `0 %`;
- score, readiness, confidence, Evidence-chain count/weight, calculated-at, algorithm
  version i model code/version ostaju backend projections;
- dodan je ciljani Listening regression i stvarni desktop/mobile visual gate.

## Očuvane odluke

- ownership, anonymous `401` i indistinguishable `404` ostaju na postojećem endpointu;
- model verzije se ne stapaju;
- Listening i Reading ostaju zasebne vještine i Knowledge Areas;
- Main Idea, Inference i drugi canonical primjeri nisu frontend enum ni production seed;
- jedna aktivnost može hraniti više komponenti samo kroz postojeći Evidence mapping;
- Teacher ne uređuje score, a UI ne računa novu mastery/readiness matematiku;
- audio/replay context, trend, activities, recommendations, PDF i Lesson Builder nisu
  simulirani.

## Validation

- backend Release build: PASS, 0 warninga i 0 grešaka;
- backend/API regression u digest-pinnanom .NET 10 SDK containeru: PASS,
  177 passed / 37 opt-in SQL skipped;
- architecture testovi: PASS, 4/4;
- ciljani `StudentKnowledge.test.tsx`: PASS, 6/6;
- puni frontend suite: PASS, 68/68;
- TypeScript typecheck, frontend lint i production build: PASS;
- `dotnet format --verify-no-changes`: PASS;
- NuGet vulnerability audit: PASS, 0 poznatih ranjivosti;
- npm audit: PASS, 0 ranjivosti;
- Docker health i SQL-backed runtime: PASS;
- non-root runtime: API UID 1654, frontend `nginx`;
- stvarni login + stvarni Knowledge API: PASS;
- visual acceptance 1536×1024 i 390×844: PASS;
- document overflow: 1536/1536 i 390/390;
- browser `pageerror`: 0;
- business writeovi tijekom capturea: 0.

SQL opt-in suite nije potreban za 5.11 jer nema promjene baze, migracije, queryja ni write
contracta. Stvarni SQL-backed API runtime potvrđen je Docker visual gateom. Production
frontend kod nije dupliciran: Phase 5.11 koristi generički model-derived detail i dodaje
Listening-specific regression/evidence bez hardkodiranja kataloga.

## Self-review

- [x] nema hardkodiranog Listening kataloga, vještina ni production seeda
- [x] Listening nije spojen s Readingom
- [x] nema client-side score/trend/recommendation formule
- [x] audio context/replay count nije lažno izveden iz postojećih podataka
- [x] model verzije ostaju odvojene i no-data nije nula
- [x] postojeći server ownership boundary nije oslabljen
- [x] native kontrole, focus i selected state ostaju dostupni
- [x] skill hijerarhija i component no-data pokriveni su regression testom
- [x] stvarni browser evidence nema lažne API podatke ni business write
- [x] desktop/mobile dokument nema horizontalni overflow
- [x] nema backend, schema, migration ni package promjene

## Datoteke

- `docs/LISTENING_DETAIL.md`
- `frontend/tests/StudentKnowledge.test.tsx`
- `frontend/tests/visual/phase511.mjs`
- `docs/visual-acceptance/phase-5.11/*`

## Scope koji nije implementiran

Nema nove baze/migracije/API-ja, hardkodiranih Listening skills, audio-context/replay
metadata modela ili breakdowna, overall scorea, školske ocjene, probabilityja, trenda,
activity feeda, recommendations, PDF-a, Lesson Builder handoffa ni ručnog overridea.

## Blockeri

Nema otvorenog business, tehničkog ni review blockera. Phase 5.11 je FINAL LOCKED.
