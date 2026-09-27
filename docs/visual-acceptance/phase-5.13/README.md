# Phase 5.13 — Writing detail visual acceptance

## Rezultat

**PASS — 2026-09-27.** Stvarni `/students/:studentId/knowledge` prikaz uspoređen je s
dostavljenim `Writing/2.5 Writing.png` (SHA-256
`6E150733FBDB2121A43CF1979461204C6F29B71BA878652CFF8326DE9E70B8A2`). Taj PNG binarno
je identičan `Speaking/2.5 Speaking.png` i na slici prikazuje Speaking sadržaj. Zato je
mjerodavan samo za zajednički 2.5 shell, area tabove, skill listu + desni detaljni panel i
vizualnu hijerarhiju; [`2.5_Writing.md`](../../source_specs/2.5_Writing.md) vodi Writing
semantiku. Nisu izmišljeni Writing demo podatci da bi ekran glumio nepostojeći mockup.

Gate je izveden normalnom lokalnom Teacher prijavom i stvarnim
`GET /api/v1/students/{id}/knowledge` odgovorom. Nisu korišteni auth bypass, API
interception, DOM mutation ni hardkodirani browser response. Tijekom capturea nije
napravljen business write; lokalna lozinka i session podatci nisu spremljeni u Git.

## Dokazi

- [Dostavljeni canonical layout](canonical.png)
- [Desktop viewport 1536×1024](writing-detail-desktop-1536x1024.png)
- [Mobile viewport 390×844](writing-detail-mobile-390x844.png)
- [Dopunski desktop full-page pregled](writing-detail-overview-desktop.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| PLUS 5 shell i aktivni Učenici modul | PASS |
| Writing navigacija | PASS — stvarni `Pisanje` Area iz objavljenog modela; bez production hardkodiranja |
| Skill + desni detaljni panel | PASS — odabrani red i panel predstavljaju istu komponentu |
| Model/version boundary | PASS — `ENGLISH-7 v2026.1` ostaje zaseban od povijesne verzije |
| Explainability | PASS — no-data/score, readiness, confidence, Evidence count/weight, calculated-at i algorithm |
| Read-only integrity | PASS — nema assessment kontrola ni business writeova bez source workflowa |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile adaptation | PASS — `scrollWidth/clientWidth = 390/390`; tablica i tabovi skrolaju samo lokalno |
| Browser greške / business writeovi | PASS — 0 / 0 |

## Skill hijerarhija i granica stvarnih podataka

Lokalni objavljeni demo model u trenutku capturea sadrži jednu root komponentu `Pisanje`
bez Evidencea, pa browser snimke pošteno prikazuju stvarni no-data katalog. Nisu dodavani
lažni Coherence, Organisation, Spelling ili drugi retci radi sličnosti s tekstualnim
primjerima. Skill → subskill drill-down, više model-derived Writing komponenti i component
no-data zasebno su pokriveni frontend regression testom s izoliranim fixtureom; production
kod koristi model-derived `ParentKnowledgeComponentId`.

## Namjerna odstupanja od sourcea

- Nema overall readiness kruga, predviđene ocjene ni probabilityja jer takva zaključana
  Student projekcija ne postoji.
- Nema trendova, activity broja/lista ni last-activity datuma jer projekcija nema time
  series ni provjerljivi display provenance.
- Nema Usvojeno/Dobro/Potrebno uvježbati/Potrebna pomoć write kontrola bez zaključanog
  Writing Activity/Attempt sourcea, rubrike i Evidence emitera.
- Nema prikaza učeničkog teksta ni AI analize koherentnosti, gramatike ili kreativnosti bez
  privacy/storage/provenance/Teacher-confirmation contracta.
- Nema automatskih objašnjenja, recommendations, PDF-a ni Lesson Builder handoffa.
- Writing komponente nisu production seed; konkretni Knowledge Model određuje naziv,
  dubinu i poredak komponenti.
- Dostavljeni Writing PNG sadržajno je Speaking mockup; nije korišten kao izvor Writing
  demo vrijednosti ili poslovnog ponašanja.

## Ponovljiva provjera

Pokrenuti Compose i odobreni lokalni Teacher review račun sa stvarnim projection
podatcima. Postaviti lokalne `PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL`,
`PLUS5_REVIEW_PASSWORD` i `PLUS5_WRITING_CANONICAL` varijable te pokrenuti
`frontend/tests/visual/phase513.mjs`. Skripta provjerava stvarni login/API, Writing deep
link, selection/detail sinkronizaciju, model verziju, explainability, desktop/mobile
overflow, browser greške, odsutnost assessment kontrola i business writeova.
