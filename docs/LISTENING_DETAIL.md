# Phase 5.11 — Listening detail

## Status i granica

**IMPLEMENTED / REVIEW READY — 2026-09-27.** Faza primjenjuje postojeći generički
Knowledge detail na canonical Listening scenarij. Ne uvodi novu domenu, tablicu,
migraciju, projekciju ni API endpoint.

Canonical poslovni i vizualni izvor je
[`source_specs/2.5_Listening.md`](source_specs/2.5_Listening.md) i pripadajući
`2.5 Listening.png`. Naziv faze opisuje canonical scenarij; production frontend ne
hardkodira naziv `Listening`/`Slušanje` ni popis vještina.

## Query i authorization contract

- koristi postojeći `GET /api/v1/students/{studentId}/knowledge`;
- samo prijavljeni Teacher dobiva vlastitog, nearhiviranog Studenta;
- missing, foreign i archived Student ostaju isti `404`, anonymous ostaje `401`;
- svaka `KnowledgeModel` verzija ostaje zasebna;
- backend projections ostaju jedini izvor scorea, confidencea i readinessa.

## Listening semantika

Listening je zaseban model-derived `KnowledgeArea` za razumijevanje govornog jezika i ne
spaja se s Readingom. Primjeri poput Main Idea, Specific Information, Understanding
Details, Inference, Vocabulary in Context, Pronunciation Recognition i Listening for Gist
nisu globalni katalog ni production seed; postoje samo ako ih konkretna Knowledge Model
verzija definira kao `KnowledgeComponent` retke.

Jedan audiozapis može proizvesti Evidence za više Listening komponenti. To mapiranje ostaje
u zaključanom Task/Evidence lancu i ne izvodi se iz naziva ili rezultata u browseru. Teacher
ne upisuje postotak, a `Score = null` ostaje no-data, ne `0 %`.

## UI contract

Teacher ostaje na istom Knowledge ekranu i može:

1. odabrati stvarni Listening area tab ako ga konkretni model sadrži;
2. odabrati stvarnu skill komponentu iz pripadajućih redaka;
3. vidjeti istu odabranu komponentu u desnom detaljnom panelu;
4. prolaziti skill → subskill hijerarhijom proizvoljne dubine;
5. uz rezultat vidjeti readiness, confidence, broj/težinu Evidence lanaca, vrijeme
   izračuna, algorithm version i točan model code/version.

Odabir je lokalno presentation stanje i ne radi business write. Kontrole ostaju native
buttoni s tipkovničkim/focus stanjem i `aria-pressed` oznakom.

## Audio kontekst i ponašanje učenika

Trajanje, težina, brzina govora, broj govornika, vrsta sadržaja, broj slušanja i response
time važni su budući Task/Material/Attempt/Evidence provenance podatci. Trenutačni
zaključani query ih ne nosi, stoga ih Phase 5.11 ne dodaje kao ad-hoc polja, ne procjenjuje
iz scorea i ne koristi za novu confidence ili mastery formulu.

Posebno, točan odgovor nakon više slušanja ne smije biti proizvoljno kažnjen bez zasebno
zaključanog pedagoškog algoritma i verzioniranja. Phase 5.11 ništa ne mijenja u
`readiness-v1`.

## Namjerno izvan scopea

Nisu uvedeni activity feed, trend, last-activity datum, audio-context/replay breakdown,
automatska objašnjenja problema, preporuke, ukupni score, predviđena ocjena, probability,
PDF ni Lesson Builder handoff. Zaključani backend nema njihov time-series, display
provenance, recommendation, export ni handoff contract.

## Visual acceptance

Canonical skill lista + desni detaljni panel očuvani su unutar prihvaćenog PLUS 5 shella.
Desktop i mobile dokazi koriste stvarnu prijavu i stvarni API; nema auth bypassa, API
interceptiona, DOM mutationa ni business writeova. Rezultati su u
[`visual-acceptance/phase-5.11`](visual-acceptance/phase-5.11/README.md).
