# Student knowledge detail

## Status

**LOCKED presentation contract v1.0 — Phase 5.7 — 2026-09-26**

Ovaj dokument zaključava read-only Screen 2.5 prikaz nad postojećim `readiness-v1`
`MasteryEstimate` projekcijama. Ne uvodi novu business formulu, povijesni trend, školsku ocjenu,
recommendation engine, production Knowledge katalog ili generički Evidence history API.

## Route i ownership

```text
GET /api/v1/students/{studentId}/knowledge
/students/{studentId}/knowledge
```

- pristup ima samo prijavljeni Teacher
- Student mora pripadati Teacheru i ne smije biti arhiviran
- missing, foreign i archived Student daju isti `404`
- endpoint je read-only i ne mijenja Evidence ili projekcije
- Screen 2.4 vodi na 2.5, a 2.5 ima eksplicitni povratak na procjenu i breadcrumb prema dosjeu

## Projection source of truth

Ekran prikazuje samo postojeće, persisted `MasteryEstimate` i
`KnowledgeAreaReadinessEstimate` projekcije iz zaključanog `readiness-v1` algoritma. Browser ne
računa novi score, ne spaja chainove i ne određuje readiness/confidence status.

Za svaku komponentu prikazuju se:

- Knowledge Model code, verzija i lifecycle status
- Knowledge Area i mjesto u single-parent component hijerarhiji
- score samo kada postoji
- canonical readiness i confidence
- distinct Evidence-chain count i effective weight
- vrijeme izračuna i algorithm version
- Active/Deprecated status komponente

`Score = null` znači **nema dovoljno podataka**, a ne `0 %`.

## Knowledge Model verzije

Student trenutačno nema zasebnu persisted "active Knowledge Model version" referencu. Phase 5.7
zato ne bira jednu verziju prešutno i ne spaja rezultate različitih verzija. Svaka konkretna
model verzija koja ima Student mastery projekciju prikazuje se kao zasebna označena sekcija,
uključujući `Retired` verziju kada postoji povijesna projekcija.

Područja i komponente dolaze iz Knowledge Modela; Grammar/Vocabulary/Reading/Listening/Speaking/
Writing nisu hardkodirani u endpointu ili frontendu.

## Student kontekst

Sažetak prikazuje postojeće administrativne podatke: ime, razred, školu, program i aktivnu grupu.
Nedodijeljeni program ili grupa prikazuju se neutralno. Ti podatci nisu dio mastery izračuna.

## Namjerne granice

Canonical 2.5 mockup sadrži koncepte za koje zaključani backend još nema vjerodostojan model.
Zato Phase 5.7 ne prikazuje:

- overall Student/test readiness i predviđenu školsku ocjenu
- probability of success
- povijesni trend (projekcija je trenutačni snapshot, ne time series)
- naslove i rezultate "nedavnih aktivnosti" bez zaključanog Task/Attempt/material provenancea
- automatske strengths/focus interpretacije i preporuke
- PDF export ili Lesson Builder handoff
- ručni Teacher score/override

Odsutnost tih elemenata je scope i integrity odluka, a ne frontend placeholder. Buduće faze ih
smiju dodati tek nakon zasebnog zaključanog podatkovnog i pedagoškog contracta.

## UI i responsive ponašanje

- stalni PLUS 5 shell i aktivni Učenici modul
- canonical breadcrumb, naslov, Student summary i "Kako čitati rezultate?" zona
- zasebna kartica po model verziji
- model-derived area tabs
- component tablica s vidljivom tree dubinom i explainability kolonama
- tablica smije imati vlastiti horizontalni scroll na uskom zaslonu; document ne smije imati
  horizontalni overflow
- loading, retry, safe `404` i stvarni no-data state

## Izvan Phase 5.7

- production Knowledge/Curriculum katalog i import
- area-specific business interpretacija Phase 5.8–5.13
- Evidence activity feed i source-specific detalji
- trend/time-series persistence
- recommendation/grade/overall readiness algoritmi
- write, override, export i report workflow
