# Phase 5.7 — Knowledge detail visual acceptance

## Rezultat

**PASS — 2026-09-26.** Stvarni `/students/:studentId/knowledge` Screen 2.5 uspoređen je s
canonical `2.5.Detalji znanja učenika.png` (SHA-256
`16A86766FBBCEAC1541931D6EC8E9B017B81491AAC4A843EF913964660F5C7BC`) iz odobrenog Teacher
source paketa. Usporedba potvrđuje high visual fidelity za zaključani Phase 5.7 projection
contract bez imitacije budućih funkcija koje nemaju backend podatkovni model.

Gate je izveden na rebuilt Docker aplikaciji normalnom lokalnom prijavom. Nisu korišteni auth
bypass, API interception, DOM mutation ni hardkodirani frontend response. Lokalna baza sadrži
stvarne `readiness-v1` projekcije nastale iz Evidence događaja iz Phase 5.6 gatea. Tijekom
capturea nije napravljen nijedan business write; lozinka i session podatci nisu spremljeni u Git.

## Dokazi

- [Canonical](canonical.png)
- [Desktop 1536×1024](knowledge-detail-desktop-1536x1024.png)
- [Mobile 390×844](knowledge-detail-mobile-390x844.png)
- [No-data desktop](knowledge-detail-no-data-desktop.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| Canonical shell i aktivni modul | PASS — stalni PLUS 5 shell; Učenici označeni žutom |
| Breadcrumb i povratak | PASS — dosje i 2.4 readiness route vode na owned Student kontekst |
| Student summary | PASS — ime, razred, škola/program/grupa stanje i explainability zona |
| Model/Area/Component hijerarhija | PASS — verzijske kartice, model-derived tabovi i projection tablica |
| Explainability | PASS — score, status, confidence, broj chainova, weight i calculated-at |
| Versioning | PASS — `ENGLISH-7/2025.1 Retired` i `ENGLISH-7/2026.1 Published` su odvojeni |
| No-data semantika | PASS — stvarni Student bez projekcija nema sintetički `0 %` |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile overflow | PASS — `scrollWidth/clientWidth = 390/390`; široka tablica skrola samo lokalno |
| Browser greške | PASS — 0 `pageerror` događaja |
| Business writeovi tijekom snimanja | PASS — 0; nakon prijave svi screen/API pozivi su read-only |

## Namjerna odstupanja od canonicala

- Nema overall readiness kruga, predviđene ocjene ni probability jer Phase 5.5 nema te
  projekcije.
- Nema trenda jer persistence čuva aktualnu projekciju, a ne time series.
- Nema recent activity feeda s nazivima kviza/zadaće/testa jer Task/Attempt/material provenance
  i production emitters nisu zaključani.
- Nema automatskih preporuka, PDF exporta ni Lesson Builder handoffa.
- Canonicalovih šest engleskih područja nije hardkodirano; stvarni Knowledge Model određuje
  nazive, poredak i dostupne tabove.
- Model verzija i lifecycle status prikazani su eksplicitnije od canonicala kako povijesne i
  aktualne semantike ne bi bile spojene.
- Na mobitelu component tablica ima vlastiti kontrolirani horizontalni scroll kako bi zadržala
  explainability kolone bez document overflowa.

## Ponovljiva provjera

Pokrenuti lokalni Compose i osigurati odobreni Teacher visual račun s postojećim stvarnim
Evidence → projection podatcima za dva owned Studenta. Postaviti samo lokalne
`PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL` i `PLUS5_REVIEW_PASSWORD` varijable te pokrenuti
`frontend/tests/visual/phase57.mjs`. Skripta radi normalnu prijavu, provjerava stvarni API,
handoff 2.4 → 2.5, breadcrumb, readiness povratak, odvojene model verzije, Area tabove,
component projekcije, no-data i overflow te sprema tri screenshota bez business writea.
