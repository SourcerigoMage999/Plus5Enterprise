# Phase 5.10 — Reading detail visual acceptance

## Rezultat

**PASS — 2026-09-27.** Stvarni `/students/:studentId/knowledge` prikaz uspoređen je s
canonical `2.5 Reading.png` (SHA-256
`4F54C2922875E748B0DAE65D076A814AF4266A73037400B02E248A2612EDA689`). Canonical je
mjerodavan za Reading tab, skill listu + desni detaljni panel i vizualnu hijerarhiju;
prikazani podatci ostaju ograničeni zaključanim Phase 5.5–5.10 contractom.

Gate je izveden normalnom lokalnom Teacher prijavom i stvarnim
`GET /api/v1/students/{id}/knowledge` odgovorom. Nisu korišteni auth bypass, API
interception, DOM mutation ni hardkodirani browser response. Tijekom capturea nije
napravljen business write; lokalna lozinka i session podatci nisu spremljeni u Git.

## Dokazi

- [Canonical](canonical.png)
- [Desktop viewport 1536×1024](reading-detail-desktop-1536x1024.png)
- [Mobile viewport 390×844](reading-detail-mobile-390x844.png)
- [Dopunski desktop full-page pregled](reading-detail-overview-desktop.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| PLUS 5 shell i aktivni Učenici modul | PASS |
| Reading navigacija | PASS — stvarni `Čitanje` Area iz objavljenog modela; bez production hardkodiranja |
| Skill + desni detaljni panel | PASS — odabrani red i panel predstavljaju istu komponentu |
| Model/version boundary | PASS — `ENGLISH-7 v2026.1` ostaje zaseban od povijesne verzije |
| Explainability | PASS — score, readiness, confidence, Evidence count/weight, calculated-at i algorithm |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile adaptation | PASS — `scrollWidth/clientWidth = 390/390`; tablica i tabovi skrolaju samo lokalno |
| Browser greške / business writeovi | PASS — 0 / 0 |

## Skill hijerarhija i granica stvarnih podataka

Lokalni objavljeni demo model u trenutku capturea sadrži jednu root komponentu `Čitanje`,
pa browser snimke pošteno prikazuju taj stvarni katalog. Nisu dodavani lažni Main Idea,
Inference ili drugi retci radi sličnosti s canonicalom. Skill → subskill drill-down i
component no-data zasebno su pokriveni frontend regression testom s izoliranim fixtureom;
production kod koristi samo model-derived `ParentKnowledgeComponentId`.

## Namjerna odstupanja od canonicala

- Nema overall readiness kruga, predviđene ocjene ni probabilityja jer takva zaključana
  Student projekcija ne postoji.
- Nema trendova, activity broja/lista ni last-activity datuma jer projekcija nema time
  series ni provjerljivi display provenance.
- Nema objašnjenja problema i preporuka izvedenih iz samog scorea.
- Nema text-type/length/difficulty breakdowna dok Materials/Task provenance contract ne
  zaključa ta polja i njihovu semantiku.
- Nema PDF-a ni Lesson Builder handoffa bez zasebnog contracta.
- Canonical Reading skills nisu production seed; konkretni Knowledge Model određuje naziv,
  dubinu i poredak komponenti.
- Povijesna i objavljena verzija prikazane su odvojeno kako se različite semantike ne bi
  stopile u isti rezultat.

## Ponovljiva provjera

Pokrenuti Compose i odobreni lokalni Teacher review račun sa stvarnim projection
podatcima. Postaviti lokalne `PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL`,
`PLUS5_REVIEW_PASSWORD` i `PLUS5_READING_CANONICAL` varijable te pokrenuti
`frontend/tests/visual/phase510.mjs`. Skripta provjerava stvarni login/API, Reading deep
link, selection/detail sinkronizaciju, model verziju, explainability, desktop/mobile
overflow, browser greške i odsutnost business writeova.
