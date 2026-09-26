# Phase 5.8 — Grammar detail visual acceptance

## Rezultat

**PASS — 2026-09-26.** Stvarni `/students/:studentId/knowledge` prikaz uspoređen je s
canonical `2.5 Grammar.png` (SHA-256
`DDE53574F25C20CB504E4C14127D1FCDD3CF25B9DEFA37A504BA184E761D390B`). Canonical je
mjerodavan za temu + desni detaljni panel, vizualnu hijerarhiju i proportions; podatkovni
prikaz ostaje ograničen zaključanim Phase 5.5–5.8 contractom.

Gate je izveden na rebuilt Docker aplikaciji normalnom lokalnom prijavom i stvarnim
`GET /api/v1/students/{id}/knowledge` odgovorom. Nisu korišteni auth bypass, API interception,
DOM mutation ni hardkodirani frontend response. Tijekom capturea nije napravljen business
write; lokalna lozinka i session podatci nisu spremljeni u Git.

## Dokazi

- [Canonical](canonical.png)
- [Desktop 1536×1024](grammar-detail-desktop-1536x1024.png)
- [Mobile 390×844](grammar-detail-mobile-390x844.png)
- [Component no-data desktop](grammar-detail-no-data-desktop.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| PLUS 5 shell i aktivni Učenici modul | PASS |
| Area/topic navigacija | PASS — model-derived tabovi; bez hardkodiranog engleskog kataloga |
| Tema + desni detaljni panel | PASS — odabrani red i panel prikazuju istu komponentu |
| Hijerarhijska putanja | PASS — Area i stvarni parent chain; child drill-down pokriven frontend testom |
| Explainability | PASS — score, status, confidence, Evidence chain count/weight, calculated-at i algorithm |
| Versioning | PASS — detalj nosi točan model code/version; verzije su zasebne |
| No-data semantika | PASS — stvarni nullable score prikazan bez sintetičkog postotka |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile adaptation | PASS — `scrollWidth/clientWidth = 390/390`; tablica skrola samo lokalno, panel se slaže ispod |
| Browser greške / business writeovi | PASS — 0 / 0 |

## Namjerna odstupanja od canonicala

- Nema overall readiness kruga, predviđene ocjene ni probability jer ne postoji takva
  zaključana Student projekcija.
- Nema trenda ni “zadnji put” activity metrike jer se čuva aktualna projekcija, ne time series
  ni activity display provenance.
- Nema recent activities, recommendations, PDF-a ni Lesson Builder handoffa.
- Canonical Grammar teme nisu hardkodirane; stvarni Knowledge Model određuje područja,
  komponente, hijerarhiju i poredak.
- Povijesna i objavljena Knowledge Model verzija prikazane su odvojeno i eksplicitnije nego u
  canonicalu kako se različita semantika ne bi stopila.

## Ponovljiva provjera

Pokrenuti Compose i odobreni lokalni Teacher review račun sa stvarnim projection podatcima.
Postaviti lokalne `PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL` i
`PLUS5_REVIEW_PASSWORD` varijable te pokrenuti `frontend/tests/visual/phase58.mjs`. Skripta
provjerava stvarni login/API, selection/detail sinkronizaciju, točnu model verziju,
explainability, component no-data, desktop/mobile overflow i odsutnost business writeova.
