# Phase 5.13 — Writing detail

## Status i granica

**IMPLEMENTED / REVIEW READY — 2026-09-27.** Faza primjenjuje postojeći generički
Knowledge detail na canonical Writing scenarij. Ne uvodi novu domenu, tablicu,
migraciju, projekciju, assessment write workflow ni API endpoint.

Canonical poslovni izvor je
[`source_specs/2.5_Writing.md`](source_specs/2.5_Writing.md). Dostavljeni
`Writing/2.5 Writing.png` binarno je identičan `Speaking/2.5 Speaking.png` i prikazuje
Speaking sadržaj; zato služi samo kao canonical izvor zajedničkog 2.5 layouta, dok
tekstualni Writing source vodi semantiku. Production frontend ne hardkodira naziv
`Writing`/`Pisanje` ni popis vještina pisanja.

## Query i authorization contract

- koristi postojeći `GET /api/v1/students/{studentId}/knowledge`;
- samo prijavljeni Teacher dobiva vlastitog, nearhiviranog Studenta;
- missing, foreign i archived Student ostaju isti `404`, anonymous ostaje `401`;
- svaka `KnowledgeModel` verzija ostaje zasebna;
- backend projections ostaju jedini izvor scorea, confidencea i readinessa.

## Writing semantika

Writing je zaseban model-derived `KnowledgeArea`. Primjeri poput gramatičke točnosti,
rječnika, organizacije, koherentnosti, pravopisa, raznolikosti rečenica i ispunjavanja
zadatka nisu globalni katalog ni production seed; postoje samo ako ih konkretna Knowledge
Model verzija definira kao `KnowledgeComponent` retke.

Jedan pisani rad može proizvesti Evidence za više Writing i drugih jezičnih komponenti
samo kroz eksplicitne Knowledge targete. UI ne izvodi cross-area mapping iz teksta, naziva,
vrste zadatka ili scorea. Teacher ne upisuje postotak, a `Score = null` ostaje no-data,
ne `0 %`.

## UI contract

Teacher ostaje na istom Knowledge ekranu i može:

1. odabrati stvarni Writing area tab ako ga konkretni model sadrži;
2. odabrati stvarnu skill komponentu iz pripadajućih redaka;
3. vidjeti istu odabranu komponentu u desnom detaljnom panelu;
4. prolaziti skill → subskill hijerarhijom proizvoljne dubine;
5. uz rezultat vidjeti readiness, confidence, broj/težinu Evidence lanaca, vrijeme
   izračuna, algorithm version i točan model code/version.

Odabir je lokalno presentation stanje i ne radi business write. Kontrole ostaju native
buttoni s tipkovničkim/focus stanjem i `aria-pressed` oznakom.

## Otvoreni tekst i Teacher procjena

Source predviđa buduću kratku strukturiranu Teacher procjenu otvorenog pisanog rada.
Phase 5.13 nema zaključan Writing Activity/Attempt emitter, source identity, rubriku ni
write route, stoga ne uvodi generički Evidence unos niti inertne
Usvojeno/Dobro/Potrebno uvježbati/Potrebna pomoć kontrole.

Budući owner-scoped workflow mora vezati append-only Evidence uz stvarni source i
eksplicitne Knowledge targete. Automatska analiza otvorenog teksta, gramatike,
koherentnosti, kreativnosti ili organizacije ostaje blokirana do zasebnog AI/privacy,
provenance i Teacher-confirmation contracta. Osnovna verzija ne smije glumiti da bez takvog
workflowa može objektivno ocijeniti otvoreni sastavak.

## Namjerno izvan scopea

Nisu uvedeni assessment write, pohrana ili prikaz učeničkog rada, AI analiza, activity
feed, trend, last-activity datum, task/context breakdown, automatska objašnjenja,
preporuke, overall score, predviđena ocjena, probability, PDF ni Lesson Builder handoff.

## Visual acceptance

Canonical zajednički 2.5 skill-list + desni detaljni panel layout očuvan je unutar
prihvaćenog PLUS 5 shella. Desktop i mobile dokazi koriste stvarnu prijavu i stvarni API;
nema auth bypassa, API interceptiona, DOM mutationa ni business writeova. Semantički
nedostatak dostavljenog PNG-a eksplicitno je dokumentiran u
[`visual-acceptance/phase-5.13`](visual-acceptance/phase-5.13/README.md).
