# Phase 9.4 visual acceptance

## Status

**PASS — 2026-10-31.**

## Canonical source

- izvor: `5.1 Početak pripreme - cilj sata.png`;
- lokalna neizmijenjena kopija: `canonical.png`;
- SHA-256: `96EC5F640C754B7FC536547DA013C16505B3C00794B27670B59A0F0C55679394`.

## Minimalni stvarni capture

- `lesson-goal-desktop-1536x1024.png`;
- `lesson-goal-mobile-390x844.png`;
- `measurements.json`.

Capture koristi stvarni Teacher cookie login, lokalni API, stvarni SQL Server i postojeće
owner-scoped Student/KC podatke. Nisu korišteni auth bypass, API interception ni DOM mutation.
Tijekom capturea nema business writeova.

## Measurements

| Prikaz | clientWidth | scrollWidth | Rezultat |
|---|---:|---:|---|
| desktop 1536×1024 | 1536 | 1536 | PASS |
| mobile 390×844 | 390 | 390 | PASS |

Participant, Session i Knowledge Component API pozivi vratili su HTTP 200 u oba viewporta.
Browser/page errors: 0.

## Canonical usporedba

Očuvani su PLUS 5 shell, jasna početna faza pripreme, Student/Group odabir, optional Session,
dominantni goal editor, Knowledge Component izbor i desni kontekstni sažetak. Desktop zadržava
glavni sadržaj i summary stupac; mobile slaže blokove bez document overflowa.

Namjerna odstupanja od canonicala:

- AI prijedlog nije lažno simuliran; prikazan je zaključani default-disabled/no-cost status;
- homework/exam/readiness kontekst nije izmišljen bez implementiranog source contracta;
- akcija nastavlja samo spremanjem Draft cilja; timeline i balance dolaze u Phase 9.5.

## Reprodukcija

`frontend/tests/visual/phase94.mjs` koristi lokalne `PLUS5_REVIEW_*` varijable i
`PLUS5_LESSON_GOAL_CANONICAL`, radi točno jedan desktop i jedan mobile capture te odbija
unexpected business write, obavezni API status različit od 200, browser error ili document
overflow. Lokalna lozinka nije spremljena u Git ni visual evidence.
