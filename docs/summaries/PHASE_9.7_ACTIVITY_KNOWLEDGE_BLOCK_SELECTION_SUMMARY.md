# Phase 9.7 — Activity / Knowledge Block selection summary

## Status

**FINAL LOCKED — odobreno 2026-11-06.**

## Implementirano

- owner-scoped current Published/Active Knowledge Block candidate query;
- participant/homework hard eligibility, bounded search i duration filter;
- stvarni `/lesson-plans/:lessonPlanId/activity-selection` Screen 5.4;
- participant/goal/KC/time kontekst, kandidat lista i detail preview;
- existing-count upozorenje bez pogrešnog duplicate hard rejecta;
- CSRF + rowversion + serializable atomic add na kraj Draft timelinea;
- server-authoritative naziv, trajanje, purpose, SortOrder i KC mapping;
- canonical desktop/mobile visual acceptance.

## Sačuvane odluke

- KB-001–KB-005 Published v1 su jedini dostupni katalog; Draft KB-006–KB-025 ostaju skriveni;
- homework-required blok nije legalan bez zaključanog LessonPlan homework contexta;
- `Aktivnosti` tab ne izmišlja nepostojeći ActivityTemplate katalog;
- ponovljeni exact blok je dopušten uz vidljivo upozorenje i postojeći balance model;
- write ne stvara Attempt/Evidence i ne koristi AI/provider niti uvodi naplatu.

## Validation

| Gate | Rezultat |
|---|---|
| Release build | PASS — 0 compile warninga, 0 grešaka |
| Targeted backend | PASS — 4/4 |
| SQL/runtime | PASS — stvarni SQL candidate query i atomic add; schema/migration unchanged |
| Architecture | PASS — 4/4 |
| Frontend targeted | PASS — 4/4 |
| Frontend lint/typecheck/build | PASS |
| Visual acceptance | PASS — 1536×1024 i 390×844; errors 0; overflow 0 |
| Full regression | N/A — targeted phase validation is sufficient |
| Dependency audit | N/A — dependency graph and lockfiles unchanged |
| Docker infrastructure validation | N/A — infrastructure unchanged |

Stvarni SQL visual setup otkrio je EF translation defect nad enum `ToString()` projekcijom;
enum vrijednosti sada se mapiraju nakon SQL materializacije, nakon čega su ponovno pokrenuti
samo pogođeni backend i visual gateovi. Neuspjeli fixture pokušaj bio je ograničen na pogrešan
default database target, a očekivani pre-login `401` izdvojen je od capture error mjerenja.

## Namjerno nije implementirano

- ActivityTemplate katalog, persistence ili lažni demo rezultati;
- activity detail/edit, delete/reorder ili regeneration;
- Quick Check authoring i Draft → Ready;
- Board runtime, Attempt, Evidence, Mastery/Readiness mutation;
- AI provider poziv ili bilo kakva dodatna/in-app naplata.

## Sljedeća faza

Phase 9.8 smije dodati activity detail/edit i zaključano regeneration ponašanje nad ovim Draft
graphom, bez preuranjene Ready potvrde ili Board/Evidence runtimea.
