# Phase 5.9 — Vocabulary detail

## Status i granica

**IMPLEMENTED / REVIEW READY — 2026-09-27.** Faza primjenjuje postojeći generički
Knowledge detail na canonical Vocabulary scenarij. Ne uvodi novu domenu, tablicu,
migraciju, projekciju ni API endpoint.

Canonical poslovni i vizualni izvor je
[`source_specs/2.5_Vocabulary.md`](source_specs/2.5_Vocabulary.md) i pripadajući
`2.5 Vocabulary.png`. Naziv faze opisuje canonical scenarij; production frontend ne
hardkodira naziv `Vocabulary`/`Vokabular`, engleske teme, riječi ni razine uporabe.

## Query i authorization contract

- koristi postojeći `GET /api/v1/students/{studentId}/knowledge`;
- samo prijavljeni Teacher dobiva vlastitog, nearhiviranog Studenta;
- missing, foreign i archived Student ostaju isti `404`, anonymous ostaje `401`;
- svaka `KnowledgeModel` verzija ostaje zasebna;
- backend projections ostaju jedini izvor scorea, confidencea i readinessa.

## UI contract

Vocabulary je običan model-derived `KnowledgeArea`. Teacher ostaje na istom Knowledge
ekranu i može:

1. odabrati stvarni Vocabulary area tab ako ga konkretni model sadrži;
2. odabrati stvarnu temu iz pripadajućih `KnowledgeComponent` redaka;
3. vidjeti istu odabranu komponentu u desnom detaljnom panelu;
4. prolaziti arbitrary-depth parent/child hijerarhijom;
5. uz rezultat vidjeti readiness, confidence, broj/težinu Evidence lanaca, vrijeme
   izračuna, algorithm version i točan model code/version.

Odabir je lokalno presentation stanje i ne radi business write. Kontrole ostaju native
buttoni s tipkovničkim/focus stanjem i `aria-pressed` oznakom.

## Vocabulary semantika

Teme i uži pojmovi dolaze isključivo iz verzioniranog Knowledge Modela. Source primjeri
poput School, Family & Friends, Hobbies & Free Time, Equipment ili pojedinačnih riječi
nisu globalni katalog ni seed zahtjev.

Razlika recognize → understand → choose → write → use in context može se izraziti samo
ako ju odobreni Knowledge Model stvarno modelira kao komponente/hijerarhiju. Phase 5.9
ne dodaje `UsageLevel`, ne izvodi tu razinu iz scorea i ne uvodi browser-side formulu.

`Score = null` ostaje no-data, a ne `0 %`. Teacher ne može ručno upisivati rezultat.

## Namjerno izvan scopea

Nisu uvedeni activity feed, trend, last-activity datum, preporuke, ukupni score,
predviđena ocjena, probability, PDF ni Lesson Builder handoff. Zaključani backend nema
njihov time-series, display provenance, recommendation, export ni handoff contract pa se
ne simuliraju iz postojećih projection podataka.

## Visual acceptance

Canonical tema + desni detaljni panel očuvani su unutar prihvaćenog PLUS 5 shella.
Desktop i mobile dokazi koriste stvarnu prijavu i stvarni API; nema auth bypassa, API
interceptiona, DOM mutationa ni business writeova. Glavne snimke su viewport snimke kako
sticky sidebar ne bi bio pogrešno prikazan kao skraćen u full-page kompoziciji. Rezultati
su u [`visual-acceptance/phase-5.9`](visual-acceptance/phase-5.9/README.md).
