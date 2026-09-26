# Phase 5.8 — Grammar detail summary

## Status

**IMPLEMENTED / REVIEW READY — 2026-09-26.** Faza nije FINAL LOCKED; čeka SA review.

## Implementirano

- model-derived Area tabovi ostaju verzijski odvojeni;
- svaka projicirana `KnowledgeComponent` postala je dostupna selekcijska kontrola;
- odabrani red ima jasan selected state i sinkronizirani desni detaljni panel;
- panel prikazuje hijerarhijsku putanju i izravne child komponente kada postoje;
- score/no-data, readiness, confidence, Evidence-chain count/weight, calculated-at,
  algorithm version i točan Knowledge Model code/version prikazani su bez nove formule;
- desktop dvostupčani i mobile stacked raspored slijede canonical Grammar vizual;
- selection je local UI state i ne radi business write.

## Očuvane odluke

- ownership i indistinguishable `404` ostaju na postojećem server endpointu;
- verzije modela se ne stapaju;
- no-data nije `0 %`;
- nazivi Grammar tema nisu production seed ni hardkodirani frontend katalog;
- Teacher ne uređuje score;
- trend, activities, recommendations, PDF i Lesson Builder nisu simulirani bez contracta.

## Validation

- backend Release build: PASS, 0 warninga i 0 grešaka;
- backend/API regression u pinned .NET 10 SDK containeru: PASS, 177 passed / 37 opt-in SQL skipped;
- architecture testovi: PASS, 4/4;
- ciljani `StudentKnowledge.test.tsx`: PASS, 3/3;
- puni frontend suite: PASS, 65/65;
- TypeScript typecheck, frontend lint i production build: PASS;
- `dotnet format --verify-no-changes`: PASS;
- NuGet vulnerability audit: PASS, 0 poznatih ranjivosti;
- npm audit: PASS, 0 ranjivosti;
- Docker image build/start, migracije i health gate: PASS;
- non-root runtime: API UID 1654, frontend `nginx`;
- stvarni login + stvarni Knowledge API: PASS;
- visual acceptance 1536×1024, 390×844 i component no-data: PASS;
- document overflow: 1536/1536 i 390/390;
- browser `pageerror`: 0;
- business writeovi tijekom capturea: 0.

Windows Application Control na hostu blokirao je učitavanje svježe generiranog
`Plus5.Infrastructure.dll` u VSTest procesu s poznatim `0x800711C7`. Kao i u ranijim
dokumentiranim fazama, mjerodavni puni regression izvršen je u digest-pinnanom .NET 10 SDK
Linux containeru nad read-only mountanim sourceom. Host Release build i format provjera
normalno prolaze. SQL opt-in suite nije potreban za 5.8 jer nema promjene baze, migracije,
queryja ni write contracta; stvarni SQL-backed API runtime potvrđen je Docker visual gateom.

## Self-review

- [x] nema hardkodiranog Grammar kataloga ni production seed podataka
- [x] nema client-side score/trend/recommendation formule
- [x] model verzije ostaju odvojene i eksplicitne
- [x] no-data nije prikazan kao nula
- [x] postojeći server ownership boundary nije oslabljen
- [x] native controls, keyboard focus i selected state su dostupni
- [x] desktop/mobile layout nema document overflow
- [x] nema backend, schema, migration ni package promjene
- [x] documentation i ROADMAP opisuju isti scope i status

## Datoteke

- `docs/GRAMMAR_DETAIL.md`
- `frontend/src/students/StudentKnowledgePage.tsx`
- `frontend/src/students/StudentKnowledgePage.css`
- `frontend/tests/StudentKnowledge.test.tsx`
- `frontend/tests/visual/phase58.mjs`
- `docs/visual-acceptance/phase-5.8/*`

## Scope koji nije implementiran

Nema nove baze/migracije/API-ja, overall scorea, školske ocjene, probabilityja, trenda,
activity feeda, recommendations, PDF-a, Lesson Builder handoffa ni ručnog overridea.

## Blockeri

Nema otvorenog business ili tehničkog blockera unutar Phase 5.8. Final LOCK je zaseban SA
review korak.
