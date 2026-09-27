# Phase 5.10 — Reading detail

## Status i granica

**IMPLEMENTED / REVIEW READY — 2026-09-27.** Faza primjenjuje postojeći generički
Knowledge detail na canonical Reading scenarij. Ne uvodi novu domenu, tablicu,
migraciju, projekciju ni API endpoint.

Canonical poslovni i vizualni izvor je
[`source_specs/2.5_Reading.md`](source_specs/2.5_Reading.md) i pripadajući
`2.5 Reading.png`. Naziv faze opisuje canonical scenarij; production frontend ne
hardkodira naziv `Reading`/`Čitanje` ni popis vještina.

## Query i authorization contract

- koristi postojeći `GET /api/v1/students/{studentId}/knowledge`;
- samo prijavljeni Teacher dobiva vlastitog, nearhiviranog Studenta;
- missing, foreign i archived Student ostaju isti `404`, anonymous ostaje `401`;
- svaka `KnowledgeModel` verzija ostaje zasebna;
- backend projections ostaju jedini izvor scorea, confidencea i readinessa.

## Reading semantika

Reading je običan model-derived `KnowledgeArea`, ali njegova primarna struktura predstavlja
vještine čitanja, ne teme teksta ili Vocabulary kategorije. Primjeri poput Main Idea,
Specific Information, Vocabulary in Context, Inference, Sequence/Order i Author's Purpose
nisu globalni katalog ni production seed; postoje samo ako ih konkretna verzija Knowledge
Modela definira kao `KnowledgeComponent` retke.

Jedna aktivnost ili tekst može proizvesti Evidence za više Reading komponenti. To mapiranje
ostaje u zaključanom Task/Evidence lancu i ne izvodi se iz naziva ili rezultata u browseru.
Teacher ne upisuje postotak, a `Score = null` ostaje no-data, ne `0 %`.

## UI contract

Teacher ostaje na istom Knowledge ekranu i može:

1. odabrati stvarni Reading area tab ako ga konkretni model sadrži;
2. odabrati stvarnu skill komponentu iz pripadajućih redaka;
3. vidjeti istu odabranu komponentu u desnom detaljnom panelu;
4. prolaziti skill → subskill hijerarhijom proizvoljne dubine;
5. uz rezultat vidjeti readiness, confidence, broj/težinu Evidence lanaca, vrijeme
   izračuna, algorithm version i točan model code/version.

Odabir je lokalno presentation stanje i ne radi business write. Kontrole ostaju native
buttoni s tipkovničkim/focus stanjem i `aria-pressed` oznakom.

## Kontekst teksta

Razred/razina, duljina, vrsta i težina teksta važni su budući Task/Material/Evidence
provenance podatci. Trenutačni zaključani query ih ne nosi, stoga ih Phase 5.10 ne dodaje
kao ad-hoc polja, ne procjenjuje iz scorea i ne prikazuje kao lažne breakdownove. Njihov
podatkovni model mora biti zaključan u pripadajućoj Materials/Task fazi prije prikaza.

## Namjerno izvan scopea

Nisu uvedeni activity feed, trend, last-activity datum, text-type/difficulty breakdown,
automatska objašnjenja problema, preporuke, ukupni score, predviđena ocjena, probability,
PDF ni Lesson Builder handoff. Zaključani backend nema njihov time-series, display
provenance, recommendation, export ni handoff contract.

## Visual acceptance

Canonical skill lista + desni detaljni panel očuvani su unutar prihvaćenog PLUS 5 shella.
Desktop i mobile dokazi koriste stvarnu prijavu i stvarni API; nema auth bypassa, API
interceptiona, DOM mutationa ni business writeova. Rezultati su u
[`visual-acceptance/phase-5.10`](visual-acceptance/phase-5.10/README.md).
