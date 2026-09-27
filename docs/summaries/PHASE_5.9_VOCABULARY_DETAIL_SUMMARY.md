# Phase 5.9 — Vocabulary detail summary

## Status

**FINAL LOCKED — 2026-09-27.** SA review odobrio je business/UI implementaciju,
architecture/security i visual acceptance bez dodatnih promjena koda ili contracta.

## Implementirano

- postojeći model-derived Knowledge detail potvrđen je za Vocabulary area bez zasebne
  domene ili endpointa;
- area tab mijenja sadržaj na istom ekranu i čuva Knowledge Model version boundary;
- odabrani topic/component red i desni detaljni panel ostaju sinkronizirani;
- arbitrary-depth topic → child → leaf putanja koristi stvarni
  `ParentKnowledgeComponentId` model;
- score/no-data, readiness, confidence, Evidence-chain count/weight, calculated-at,
  algorithm version i točan model code/version ostaju backend projections;
- dodan je ciljani Vocabulary regression i stvarni desktop/mobile visual gate;
- viewport evidence uklanja raniji full-page screenshot artefakt skraćenog sticky sidebara.

## Očuvane odluke

- ownership, anonymous `401` i indistinguishable `404` ostaju na postojećem endpointu;
- model verzije se ne stapaju i no-data nije `0 %`;
- Vocabulary teme, riječi i usage levels nisu production katalog ni frontend enum;
- recognize/understand/choose/write/use-in-context postoji samo ako ga modelira konkretni
  Knowledge Model;
- Teacher ne uređuje score, a UI ne računa novu mastery/readiness matematiku;
- trend, activities, recommendations, PDF i Lesson Builder nisu simulirani.

## Validation

- backend Release build: PASS, 0 warninga i 0 grešaka;
- backend/API regression u digest-pinnanom .NET 10 SDK containeru: PASS,
  177 passed / 37 opt-in SQL skipped;
- architecture testovi: PASS, 4/4;
- ciljani `StudentKnowledge.test.tsx`: PASS, 4/4;
- puni frontend suite: PASS, 66/66;
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

SQL opt-in suite nije potreban za 5.9 jer nema promjene baze, migracije, queryja ni write
contracta. Stvarni SQL-backed API runtime potvrđen je Docker visual gateom. Production
frontend kod nije dupliciran: Phase 5.9 namjerno koristi generički, model-derived detail
zaključan u Phase 5.8 i dodaje Vocabulary-specific regression/evidence bez hardkodiranja.

## Self-review

- [x] nema hardkodiranog Vocabulary kataloga, riječi, usage-level enuma ni production seeda
- [x] nema client-side score/trend/recommendation formule
- [x] model verzije ostaju odvojene i eksplicitne
- [x] no-data nije prikazan kao nula
- [x] postojeći server ownership boundary nije oslabljen
- [x] native kontrole, focus i selected state ostaju dostupni
- [x] arbitrary-depth hijerarhija pokrivena je regression testom
- [x] stvarni browser evidence nema lažne API podatke ni business write
- [x] desktop/mobile dokument nema horizontalni overflow
- [x] nema backend, schema, migration ni package promjene

## Datoteke

- `docs/VOCABULARY_DETAIL.md`
- `frontend/tests/StudentKnowledge.test.tsx`
- `frontend/tests/visual/phase59.mjs`
- `docs/visual-acceptance/phase-5.9/*`

## Scope koji nije implementiran

Nema nove baze/migracije/API-ja, hardkodiranih Vocabulary tema, usage-level business polja,
overall scorea, školske ocjene, probabilityja, trenda, activity feeda, recommendations,
PDF-a, Lesson Builder handoffa ni ručnog overridea.

## Blockeri

Nema otvorenog business, tehničkog ni review blockera. Phase 5.9 je FINAL LOCKED.
