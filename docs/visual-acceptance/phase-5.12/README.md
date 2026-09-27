# Phase 5.12 — Speaking detail visual acceptance

## Rezultat

**PASS — 2026-09-27.** Stvarni `/students/:studentId/knowledge` prikaz uspoređen je s
canonical `2.5 Speaking.png` (SHA-256
`6E150733FBDB2121A43CF1979461204C6F29B71BA878652CFF8326DE9E70B8A2`). Canonical je
mjerodavan za Speaking tab, skill listu + desni detaljni panel i vizualnu hijerarhiju;
prikazani podatci ostaju ograničeni zaključanim Phase 5.5–5.12 contractom.

Gate je izveden normalnom lokalnom Teacher prijavom i stvarnim
`GET /api/v1/students/{id}/knowledge` odgovorom. Nisu korišteni auth bypass, API
interception, DOM mutation ni hardkodirani browser response. Tijekom capturea nije
napravljen business write; lokalna lozinka i session podatci nisu spremljeni u Git.

## Dokazi

- [Canonical](canonical.png)
- [Desktop viewport 1536×1024](speaking-detail-desktop-1536x1024.png)
- [Mobile viewport 390×844](speaking-detail-mobile-390x844.png)
- [Dopunski desktop full-page pregled](speaking-detail-overview-desktop.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| PLUS 5 shell i aktivni Učenici modul | PASS |
| Speaking navigacija | PASS — stvarni `Govor` Area iz objavljenog modela; bez production hardkodiranja |
| Skill + desni detaljni panel | PASS — odabrani red i panel predstavljaju istu komponentu |
| Model/version boundary | PASS — `ENGLISH-7 v2026.1` ostaje zaseban od povijesne verzije |
| Explainability | PASS — score, readiness, confidence, Evidence count/weight, calculated-at i algorithm |
| Read-only integrity | PASS — nema assessment kontrola ni business writeova bez source workflowa |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile adaptation | PASS — `scrollWidth/clientWidth = 390/390`; tablica i tabovi skrolaju samo lokalno |
| Browser greške / business writeovi | PASS — 0 / 0 |

## Skill hijerarhija i granica stvarnih podataka

Lokalni objavljeni demo model u trenutku capturea sadrži jednu root komponentu `Govor`, pa
browser snimke pošteno prikazuju taj stvarni katalog. Nisu dodavani lažni Fluency, Accuracy,
Pronunciation ili drugi retci radi sličnosti s canonicalom. Skill → subskill drill-down,
`Samostalnost u govoru` i component no-data zasebno su pokriveni frontend regression testom
s izoliranim fixtureom; production kod koristi model-derived `ParentKnowledgeComponentId`.

## Namjerna odstupanja od canonicala

- Nema overall readiness kruga, predviđene ocjene ni probabilityja jer takva zaključana
  Student projekcija ne postoji.
- Nema trendova, activity broja/lista ni last-activity datuma jer projekcija nema time
  series ni provjerljivi display provenance.
- Canonical `Samopouzdanje` nije prihvaćeno kao psihološki postotak; budući katalog smije
  modelirati samo opaživu `Samostalnost u govoru`.
- Nema Odlično/Dobro/Potrebno uvježbati/Potrebna pomoć write kontrola bez zaključanog
  Speaking Activity/Attempt sourcea i Evidence emitera.
- Nema audio snimanja, transkripta ni AI analize bez privacy/storage/AI contracta.
- Nema automatskih objašnjenja, recommendations, PDF-a ni Lesson Builder handoffa.
- Canonical Speaking skills nisu production seed; konkretni Knowledge Model određuje naziv,
  dubinu i poredak komponenti.

## Ponovljiva provjera

Pokrenuti Compose i odobreni lokalni Teacher review račun sa stvarnim projection
podatcima. Postaviti lokalne `PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL`,
`PLUS5_REVIEW_PASSWORD` i `PLUS5_SPEAKING_CANONICAL` varijable te pokrenuti
`frontend/tests/visual/phase512.mjs`. Skripta provjerava stvarni login/API, Speaking deep
link, selection/detail sinkronizaciju, model verziju, explainability, desktop/mobile
overflow, browser greške i odsutnost business writeova.
