# Phase 9.5 visual acceptance

## Status

**PASS — 2026-11-03.**

## Canonical source

- izvor: `5.2 Prijedlog strukture i sadržaj sata.png`;
- lokalna neizmijenjena kopija: `canonical.png`;
- SHA-256: `70F31D1689D237B3593A2574C1AD8D20A6EDDB77F7C182533B194A32EE8B46CD`.

## Minimalni stvarni capture

- `lesson-structure-desktop-1536x1024.png`;
- `lesson-structure-mobile-390x844.png`;
- `measurements.json`.

Capture koristi stvarni Teacher cookie login, lokalni API i stvarni SQL Server. Setup je kroz
stvarni API stvorio jedan prazan Draft plan s goalom i exact KC mappingom. Nakon početka oba
capturea nije bilo business writeova. Nisu korišteni auth bypass, API interception ni DOM
mutation.

## Measurements

| Prikaz | clientWidth | scrollWidth | Rezultat |
|---|---:|---:|---|
| desktop 1536×1024 | 1536 | 1536 | PASS |
| mobile 390×844 | 390 | 390 | PASS |

Structure API vratio je HTTP 200 u oba viewporta. Browser/page errors: 0.

## Canonical usporedba

Očuvani su PLUS 5 shell, jasan 5.2 naslov/kontekst, tabovi, dominantna timeline zona, desni
balance/material panel i vidljiv nastavak workflowa. Mobile slaže sadržaj u jednu kolonu bez
document overflowa, dok tabovi koriste lokalni horizontalni prostor.

Desktop capture potvrđuje da su akcije `Natrag` i `Primijeni prijedlog` smještene ispod
globalnog notification/profile bloka (`DT` / `Demo Teacher`), uz jasno vertikalno odvajanje
i bez preklapanja. Mobile ih prikazuje u zasebnom punom retku ispod zaglavlja.

Namjerna odstupanja od canonicala slijede zaključani contract:

- nema lažnog predicted outcome/readiness postotka ili future gradea;
- nema plaćene/fake AI pomoći; prikazan je `SYSTEM_BASELINE_V1` i no-extra-cost status;
- prikazuje se Material candidate count, ali nema attachmenta ni relevance tvrdnje prije 9.6;
- timeline koristi točno zaključanih pet baseline aktivnosti i KB-006 ostaje Draft;
- `Apply` je eksplicitna potvrda; preview sam ne piše canonical aktivnosti.

## Reprodukcija

`frontend/tests/visual/phase95.mjs` koristi lokalne `PLUS5_REVIEW_*` varijable i
`PLUS5_LESSON_STRUCTURE_CANONICAL`, stvara jedan legalni Draft setup kroz stvarni API, zatim
radi točno jedan desktop i jedan mobile preview capture te odbija capture business write,
API status različit od 200, browser error ili document overflow. Lokalna lozinka nije
spremljena u Git ni evidence.
