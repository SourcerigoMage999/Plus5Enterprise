# Phase 5.7 — Detalj znanja učenika

## Status

`FINAL LOCKED — odobreno 2026-09-26`

## Datum

`2026-09-26`

## Cilj faze

Otvoriti Screen 2.5 kao owner-scoped drill-down iz procjene spremnosti u postojeće
`MasteryEstimate` projekcije, bez nove formule ili prikazivanja podataka koje sustav još nema.

## Implementirano

- autorizirani `GET /api/v1/students/{studentId}/knowledge`
- ista owner/non-archived granica i nerazlučivi `404` kao u Student/readiness contractu
- Student kontekst: razred, škola, program i aktivna grupa
- zasebne sekcije po točnoj Knowledge Model verziji i lifecycle statusu
- model-derived area tabovi bez hardkodiranih šest engleskih područja
- single-parent component hijerarhija s trenutnim scoreom, readinessom, confidenceom,
  Evidence-chain countom, effective weightom, vremenom i algorithm versionom
- no-data prikaz bez sintetičkog `0 %`
- poveznice 2.4 → 2.5, povratak na procjenu i breadcrumb prema dosjeu
- responsive prikaz s lokalnim table overflowom

## Sigurnost i konzistentnost

- Student ownership provjerava se prije čitanja projekcija
- frontend ne čita sirove odgovore ni Evidence payload
- endpoint nema write operaciju
- različite Knowledge Model verzije ne stapaju se
- lifecycle status `Published`/`Retired` ostaje vidljiv
- browser ne izvodi readiness račun

## Namjerne granice

Nisu uvedeni overall score, predviđena ocjena, probability, trend, recent activity feed,
recommendations, PDF, Lesson Builder handoff ni ručni override. Zaključani model nema potrebne
projekcije/provenance za te funkcije. Detaljan contract: `KNOWLEDGE_DETAIL.md`.

## Testovi i evidence

| Gate | Rezultat |
|---|---|
| Release backend/architecture suite bez SQL opt-ina | PASS — 177 passed, 37 očekivano skipped; architecture 4/4 |
| Backend owner/version/hierarchy testovi | PASS — 3/3 |
| API auth regression | PASS — anonymous `401`, owned Student `200` |
| Frontend screen testovi | PASS — projection, area switch, no-data i safe `404` |
| TypeScript i lint | PASS |
| Frontend regression | PASS — 65/65 |
| Frontend production build i lint | PASS |
| `.NET format --verify-no-changes` | PASS |
| Docker rebuild, migration apply i HTTP health | PASS |
| Non-root runtime | PASS — API UID `1654`, frontend `nginx` |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Stvarni visual acceptance | PASS — desktop, mobile, no-data, dvije model verzije, Area tabovi, bez document overflowa |

## Točna početna točka za sljedeću fazu

Phase 5.8–5.13 ne smiju hardkodirati jezične teme niti izmišljati area-specific semantiku.
Mogu koristiti isti verzionirani component tree tek kada odgovarajući production katalog i
detaljni area contract budu zaključani. Trend, activity provenance i recommendations ostaju
zasebni gateovi.
