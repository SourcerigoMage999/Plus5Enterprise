# Phase 5.11 — Listening detail visual acceptance

## Rezultat

**PASS — 2026-09-27.** Stvarni `/students/:studentId/knowledge` prikaz uspoređen je s
canonical `2.5 Listening.png` (SHA-256
`D7B4124F5EC663EB8AB5E20807062444D78EA66708103A8A22C65A108AB3A792`). Canonical je
mjerodavan za Listening tab, skill listu + desni detaljni panel i vizualnu hijerarhiju;
prikazani podatci ostaju ograničeni zaključanim Phase 5.5–5.11 contractom.

Gate je izveden normalnom lokalnom Teacher prijavom i stvarnim
`GET /api/v1/students/{id}/knowledge` odgovorom. Nisu korišteni auth bypass, API
interception, DOM mutation ni hardkodirani browser response. Tijekom capturea nije
napravljen business write; lokalna lozinka i session podatci nisu spremljeni u Git.

## Dokazi

- [Canonical](canonical.png)
- [Desktop viewport 1536×1024](listening-detail-desktop-1536x1024.png)
- [Mobile viewport 390×844](listening-detail-mobile-390x844.png)
- [Dopunski desktop full-page pregled](listening-detail-overview-desktop.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| PLUS 5 shell i aktivni Učenici modul | PASS |
| Listening navigacija | PASS — stvarni `Slušanje` Area iz objavljenog modela; bez production hardkodiranja |
| Skill + desni detaljni panel | PASS — odabrani red i panel predstavljaju istu komponentu |
| Reading/Listening granica | PASS — Listening je zaseban Area, ne Reading alias |
| Model/version boundary | PASS — `ENGLISH-7 v2026.1` ostaje zaseban od povijesne verzije |
| Explainability | PASS — score, readiness, confidence, Evidence count/weight, calculated-at i algorithm |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile adaptation | PASS — `scrollWidth/clientWidth = 390/390`; tablica i tabovi skrolaju samo lokalno |
| Browser greške / business writeovi | PASS — 0 / 0 |

## Skill hijerarhija i granica stvarnih podataka

Lokalni objavljeni demo model u trenutku capturea sadrži jednu root komponentu `Slušanje`,
pa browser snimke pošteno prikazuju taj stvarni katalog. Nisu dodavani lažni Main Idea,
Inference, Pronunciation Recognition ili drugi retci radi sličnosti s canonicalom. Skill →
subskill drill-down i component no-data zasebno su pokriveni frontend regression testom s
izoliranim fixtureom; production kod koristi model-derived `ParentKnowledgeComponentId`.

## Namjerna odstupanja od canonicala

- Nema overall readiness kruga, predviđene ocjene ni probabilityja jer takva zaključana
  Student projekcija ne postoji.
- Nema trendova, activity broja/lista ni last-activity datuma jer projekcija nema time
  series ni provjerljivi display provenance.
- Nema audio duration/difficulty/speech-rate/speaker/content-type ni replay-count breakdowna
  dok Materials/Task/Attempt provenance contract ne zaključa podatke i njihovu semantiku.
- Nema automatskih objašnjenja problema, preporuka, PDF-a ni Lesson Builder handoffa.
- Canonical Listening skills nisu production seed; konkretni Knowledge Model određuje naziv,
  dubinu i poredak komponenti.
- Povijesna i objavljena verzija prikazane su odvojeno kako se različite semantike ne bi
  stopile u isti rezultat.

## Ponovljiva provjera

Pokrenuti Compose i odobreni lokalni Teacher review račun sa stvarnim projection
podatcima. Postaviti lokalne `PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL`,
`PLUS5_REVIEW_PASSWORD` i `PLUS5_LISTENING_CANONICAL` varijable te pokrenuti
`frontend/tests/visual/phase511.mjs`. Skripta provjerava stvarni login/API, Listening deep
link, selection/detail sinkronizaciju, model verziju, explainability, desktop/mobile
overflow, browser greške i odsutnost business writeova.
