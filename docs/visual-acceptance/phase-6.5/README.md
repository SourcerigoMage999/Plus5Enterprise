# Phase 6.5 visual acceptance — Assessable task metadata

## Status

**PASS — stvarni login, stvarni API i desktop/mobile prikaz procjenjivih zadataka.**

## Evidence

| Datoteka | Svrha |
|---|---|
| `canonical.png` | neizmijenjeni canonical `4.2 Pregled materijala.png` |
| `material-tasks-desktop-1536x1024.png` | stvarni task metadata, desktop viewport |
| `material-tasks-mobile-390x844.png` | stvarni task metadata, mobile viewport |
| `measurements.json` | viewport/overflow i runtime evidence |

Canonical SHA-256:
`FE87227511E863D6A62B0349066CF082E3DA89365666E5BB697F534C4679C456`.

## Izvršeni gate

- stvarna prijava kroz `/auth/login`, bez auth bypassa;
- stvarni `GET /api/v1/materials` i `GET /api/v1/materials/{materialId}`;
- response sadrži dvije stvarne version-bound Task projekcije;
- Recognition i Production EvidenceType, difficulty, max score, answer/criterion i leaf
  Knowledge Component mapping potvrđeni su kroz stvarni API i UI;
- desktop `scrollWidth/clientWidth = 1536/1536`;
- mobile `scrollWidth/clientWidth = 390/390`;
- browser errors: `0`;
- business writeovi tijekom capturea: `0`.

## Canonical odnos i odstupanja

Phase 6.5 proširuje prihvaćeni Screen 4.2 layout sekcijom `Zadaci i procjena` ispod
version-bound detalja i kurikuluma. Canonical ne daje zasebni task-card raspored, pa kartice
slijede postojeći PLUS 5 card/badge/spacing sustav i responsive stacking bez mijenjanja
zaključane detail hijerarhije.

Ne prikazuju se izmišljeni Attempt, accuracy, EvidenceEvent, mastery/readiness ni activity
podaci. Task metadata opisuje procjenjivost; sam pregled materijala ne stvara dokaz znanja.

Ponovljivi gate je `frontend/tests/visual/phase65.mjs`. Skripta traži lokalne review
credentiale, put do bundled Playwrighta i canonical PNG kroz environment varijable; lozinka
se ne sprema u Git. Idempotentni lokalni fixture je `frontend/tests/visual/phase65-fixture.sql`.
