# Phase 5.8 — Grammar detail

## Status i granica

**IMPLEMENTED / REVIEW READY — 2026-09-26.** Ovaj contract proširuje zaključani
[Knowledge detail](KNOWLEDGE_DETAIL.md) prikaz interaktivnim detaljem odabrane
`KnowledgeComponent`. Ne uvodi novu domenu, tablicu, migraciju, projekciju ni API endpoint.

Canonical poslovni i vizualni izvor je
[`source_specs/2.5_Grammar.md`](source_specs/2.5_Grammar.md) i pripadajući `2.5 Grammar.png`.
Naziv faze opisuje canonical Grammar scenarij, ali frontend ne smije hardkodirati engleski
katalog ni pretpostaviti da se područje zove `Grammar`/`Gramatika`.

## Query i authorization contract

- koristi postojeći `GET /api/v1/students/{studentId}/knowledge`;
- samo prijavljeni Teacher dobiva vlastitog, nearhiviranog Studenta;
- missing, foreign i archived Student ostaju isti `404`, anonymous ostaje `401`;
- različite `KnowledgeModel` verzije ostaju odvojene i ne spajaju se;
- prikazuju se samo postojeće `MasteryEstimate` i `KnowledgeAreaReadinessEstimate`
  projekcije, bez client-side business izračuna.

## UI contract

Unutar svake zasebne Knowledge Model verzije Teacher može:

1. odabrati model-derived `KnowledgeArea` tab;
2. odabrati stvarnu komponentu iz tablice;
3. vidjeti označeni red i desni panel iste komponente;
4. vidjeti putanju `Area → parenti → odabrana komponenta`;
5. prijeći na izravno podređenu komponentu kada takav čvor postoji.

Detaljni panel prikazuje isključivo postojeća polja projekcije:

- naziv i lifecycle status komponente;
- score ili eksplicitni no-data prikaz;
- readiness status i confidence;
- broj efektivnih Evidence lanaca i effective weight;
- vrijeme izračuna i algorithm version;
- točan `KnowledgeModel.Code + Version`.

Odabir je lokalno presentation stanje. Ne sprema se i ne emitira business write. Server ostaje
autoritet za ownership i sve izračune. Kontrole su native buttoni, dostupne tipkovnicom, s
vidljivim focus stanjem i `aria-pressed` oznakom odabrane komponente.

## No-data i hijerarhija

`Score = null` znači da nema dovoljno valjanih dokaza; UI prikazuje crtu i tekstualno
objašnjenje, nikada sintetički `0 %`. Parent/child odnos dolazi iz
`ParentKnowledgeComponentId`; frontend ne pretpostavlja dubinu ni vrstu čvora i štiti prikaz
putanje od ciklusa ili nedostajućeg parenta.

## Namjerno izvan scopea

Canonical sadrži trend, broj/nazive aktivnosti, last-activity datum, strength/focus
interpretaciju, recommendations, PDF i Lesson Builder handoff. To nije implementirano jer
zaključani Phase 5.5–5.7 contract nema time series, Task/Attempt/material display provenance,
recommendation model, ukupni score ni export/handoff contract. Teacher ne može ručno uređivati
score. Ta se polja ne simuliraju i ne izvode u browseru.

## Visual acceptance

Canonical dvostupčani obrazac tema + detaljni panel očuvan je na desktopu. Na mobilnom se
panel slaže ispod lokalno scrollabilne tablice; dokument nema horizontalni overflow. Stvarni
login/API, selected state, model version, explainability i no-data dokazi nalaze se u
[`visual-acceptance/phase-5.8`](visual-acceptance/phase-5.8/README.md).
