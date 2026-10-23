# Phase 7.7 visual acceptance

## Status

**PASS — 2026-10-23.**

## Canonical source

- izvor: `4.3 P4.png`;
- lokalna neizmijenjena kopija: `canonical.png`;
- SHA-256: `8365CD11CD94DB8573472E627B510A6576DB4735ABB279E775B5F42766F6D394`.

## Minimalni stvarni capture

- `presentation-autosave-desktop-1536x1024.png`;
- `presentation-autosave-mobile-390x844.png`;
- `measurements.json`.

Capture koristi stvarni Teacher cookie login, aktualni lokalni API i Docker SQL Server.
Valjana promjena postojećeg Quick Checka spremljena je nakon 800 ms kroz stvarni typed
endpoint, a izvorni tekst zatim je vraćen istim autosave flowom. Nisu korišteni auth bypass,
API interception ni DOM mutation. Tijekom samog capturea nema business writeova ni
browser/page grešaka.

## Measurements

| Prikaz | clientWidth | scrollWidth | Rezultat |
|---|---:|---:|---|
| desktop 1536×1024 | 1536 | 1536 | PASS |
| mobile 390×844 | 390 | 390 | PASS |

Oba prikaza pokazuju stvarni spremljeni Quick Check, vidljivo `Spremljeno` stanje i odabrani
property panel bez globalnog horizontalnog overflowa. Desktop dodatno pokazuje header status
`Sve promjene spremljene`; mobile fokusira isti status i editable sadržaj u čitljivom
jednostupčanom rasporedu.

## Canonical usporedba

Očuvani su canonical P4 app shell, koraci izrade, centralni 16:9 canvas, slojevi i desni
property panel. Phase 7.7 dodaje eksplicitni autosave status uz postojeći `Spremi nacrt`, bez
promjene canonical vizualne hijerarhije. Konflikt/retry/recovery ponašanja nisu duplicirana u
browser screenshotovima jer su dokaziva ciljanim component testovima; preview/publish i AI
kontrole ostaju izvan scopea.

## Reprodukcija

`frontend/tests/visual/phase77.mjs` prima lokalne review varijable, koristi stvarnu prijavu i
API te generira točno jedan desktop i jedan mobile viewport capture. Lozinka se ne sprema u
Git ni visual evidence.
