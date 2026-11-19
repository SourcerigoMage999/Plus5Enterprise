# Phase 10.1 visual acceptance

## Status

**PASS — 2026-11-19.**

Canonical source: `6.1 Ploča učitelja/6.1 Ploča učitelja.png`.

- canonical SHA-256: `343A434E33D9ADFF8227A656FDDF834467543D80CB42127BA8FBD3677CF66D98`;
- canonical je neizmijenjen kopiran kao `canonical.png`;
- desktop evidence: `teacher-board-desktop-1536x1024.png`;
- mobile evidence: `teacher-board-mobile-390x844.png`.

## Runtime flow

Evidence je nastao kroz stvarni lokalni browser/runtime:

1. stvarna Teacher cookie prijava bez auth bypassa;
2. postojeći stvarni Ready LessonPlan otvoren je kroz confirmation ekran;
3. stvarni start endpoint atomarno je stvorio SQL-backed LessonSession i Pending timeline;
4. stvarni owner-scoped GET oporavio je authoritative Board stanje;
5. desktop i mobile capture koriste isti stvarni LessonSession.

Nisu korišteni API interception, mock DOM, DOM mutation ni hardkodirani frontend odgovori.
Jedini business write bio je potreban stvarni start LessonSessiona; tijekom konačnog capturea
nije bilo dodatnih writeova.

## Vizualna usporedba

PASS:

- trajni PLUS 5 shell i Board kontekst;
- participant, exact plan/version, revision i committed-save podaci;
- ordered Pending timeline i jasno odvojena runtime radna zona;
- recovery-safe akcije bez preklapanja s globalnim profilom;
- desktop hijerarhija i mobile single-column stacking;
- bez document horizontalnog overflowa i bez browser/page grešaka.

Namjerna odstupanja od canonicala:

- activity execution, live diagnostic i završni summary ostaju disabled jer pripadaju Phase
  10.2–10.5;
- exact Ready version, revision i server-save status prikazani su eksplicitnije radi
  auditabilnosti i recovery semantike;
- mobile prikaz slaže sadržaj vertikalno umjesto smanjivanja canonical desktop rasporeda.

## Mjerenja

- desktop viewport: `1536×1024`, `scrollWidth 1536 <= clientWidth 1536`;
- mobile viewport: `390×844`, `scrollWidth 390 <= clientWidth 390`;
- browser/page errors tijekom konačnog capturea: `0`;
- auth bypass / API interception / DOM mutation: `false`;
- business writeovi tijekom konačnog capturea: `0`.

Strojno čitljivi detalji su u `measurements.json`.
