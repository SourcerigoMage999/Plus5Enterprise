# Phase 5.9 — Vocabulary detail visual acceptance

## Rezultat

**PASS — 2026-09-27.** Stvarni `/students/:studentId/knowledge` prikaz uspoređen je s
canonical `2.5 Vocabulary.png` (SHA-256
`D535452D875128C52724117227A3CC5429A1D0BC29D781AB9F9FA4B6151F262A`). Canonical je
mjerodavan za Vocabulary tab, temu + desni detaljni panel i vizualnu hijerarhiju;
prikazani podatci ostaju ograničeni zaključanim Phase 5.5–5.9 contractom.

Gate je izveden normalnom lokalnom Teacher prijavom i stvarnim
`GET /api/v1/students/{id}/knowledge` odgovorom. Nisu korišteni auth bypass, API
interception, DOM mutation ni hardkodirani browser response. Tijekom capturea nije
napravljen business write; lokalna lozinka i session podatci nisu spremljeni u Git.

## Dokazi

- [Canonical](canonical.png)
- [Desktop viewport 1536×1024](vocabulary-detail-desktop-1536x1024.png)
- [Mobile viewport 390×844](vocabulary-detail-mobile-390x844.png)
- [Dopunski desktop full-page pregled](vocabulary-detail-overview-desktop.png)
- [Mjerenja](measurements.json)

Glavne snimke su stvarni viewport capturei, ne full-page stitch. Time sticky sidebar
ostaje prikazan kao puna visina preglednika i ne nastaje lažni dojam skraćene bočne trake.

## Usporedba

| Područje | Rezultat |
|---|---|
| PLUS 5 shell i aktivni Učenici modul | PASS |
| Vocabulary navigacija | PASS — stvarni `Vokabular` Area iz objavljenog modela; bez production hardkodiranja |
| Tema + desni detaljni panel | PASS — odabrani red i panel predstavljaju istu komponentu |
| Model/version boundary | PASS — `ENGLISH-7 v2026.1` ostaje zaseban od povijesne verzije |
| Explainability | PASS — score, readiness, confidence, Evidence count/weight, calculated-at i algorithm |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile adaptation | PASS — `scrollWidth/clientWidth = 390/390`; tablica i tabovi skrolaju samo lokalno |
| Browser greške / business writeovi | PASS — 0 / 0 |

## Hijerarhija i granica stvarnih podataka

Lokalni objavljeni demo model u trenutku capturea sadrži jednu root komponentu
`Vokabular`, pa browser snimke pošteno prikazuju taj stvarni katalog. Nisu dodavani lažni
topic/word retci samo da bi slika nalikovala canonicalu. Model-derived topic → child → leaf
drill-down proizvoljne dubine zasebno je pokriven frontend regression testom s izoliranim
test fixtureom; produkcijski kod za to koristi samo `ParentKnowledgeComponentId`.

## Namjerna odstupanja od canonicala

- Nema overall readiness kruga, predviđene ocjene ni probabilityja jer takva zaključana
  Student projekcija ne postoji.
- Nema trendova, activity broja/lista ni last-activity datuma jer projekcija nema time
  series ni provjerljivi display provenance.
- Nema recommendations, PDF-a ni Lesson Builder handoffa bez zasebnog contracta.
- Canonical teme i riječi nisu production seed; konkretni Knowledge Model određuje naziv,
  dubinu i poredak komponenti.
- Recognize/understand/choose/write/use-in-context nije izvedeni UI score ni novo polje;
  može se pojaviti samo kao eksplicitna verzionirana Knowledge Model struktura.
- Povijesna i objavljena verzija prikazane su odvojeno kako se različite semantike ne bi
  stopile u isti rezultat.

## Ponovljiva provjera

Pokrenuti Compose i odobreni lokalni Teacher review račun sa stvarnim projection
podatcima. Postaviti lokalne `PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL`,
`PLUS5_REVIEW_PASSWORD` i `PLUS5_VOCABULARY_CANONICAL` varijable te pokrenuti
`frontend/tests/visual/phase59.mjs`. Skripta provjerava stvarni login/API, Vocabulary deep
link, selection/detail sinkronizaciju, model verziju, explainability, desktop/mobile
overflow, browser greške i odsutnost business writeova.
