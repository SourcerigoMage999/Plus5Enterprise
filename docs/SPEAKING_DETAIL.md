# Phase 5.12 — Speaking detail

## Status i granica

**IMPLEMENTED / REVIEW READY — 2026-09-27.** Faza primjenjuje postojeći generički
Knowledge detail na canonical Speaking scenarij. Ne uvodi novu domenu, tablicu,
migraciju, projekciju, assessment write workflow ni API endpoint.

Canonical poslovni i vizualni izvor je
[`source_specs/2.5_Speaking.md`](source_specs/2.5_Speaking.md) i pripadajući
`2.5 Speaking.png`. Naziv faze opisuje canonical scenarij; production frontend ne
hardkodira naziv `Speaking`/`Govor` ni popis govorno-komunikacijskih vještina.

## Query i authorization contract

- koristi postojeći `GET /api/v1/students/{studentId}/knowledge`;
- samo prijavljeni Teacher dobiva vlastitog, nearhiviranog Studenta;
- missing, foreign i archived Student ostaju isti `404`, anonymous ostaje `401`;
- svaka `KnowledgeModel` verzija ostaje zasebna;
- backend projections ostaju jedini izvor scorea, confidencea i readinessa.

## Speaking semantika

Speaking je zaseban model-derived `KnowledgeArea`. Primjeri poput Fluency, Accuracy,
Vocabulary, Pronunciation, Intelligibility, Interaction, Extended Response i Independence
nisu globalni katalog ni production seed; postoje samo ako ih konkretna Knowledge Model
verzija definira kao `KnowledgeComponent` retke.

Canonical `Samopouzdanje` ne smije se tretirati kao psihološki postotak. Mjerljiva poslovna
semantika je `Samostalnost u govoru`: opažena potreba za pomoći, mogućnost započinjanja i
nastavljanja odgovora/razgovora. Phase 5.12 ne dodaje niti preimenuje production katalog;
zaključava samo da budući katalog i Evidence ne smiju tvrditi nedokazivu psihološku procjenu.

Jedna speaking aktivnost može proizvesti Evidence za više Speaking i drugih jezičnih
komponenti samo kroz eksplicitne Knowledge targete. UI ne izvodi cross-area mapping iz
teksta, naziva ili scorea.

## UI contract

Teacher ostaje na istom Knowledge ekranu i može:

1. odabrati stvarni Speaking area tab ako ga konkretni model sadrži;
2. odabrati stvarnu skill komponentu iz pripadajućih redaka;
3. vidjeti istu odabranu komponentu u desnom detaljnom panelu;
4. prolaziti skill → subskill hijerarhijom proizvoljne dubine;
5. uz rezultat vidjeti readiness, confidence, broj/težinu Evidence lanaca, vrijeme
   izračuna, algorithm version i točan model code/version.

Odabir je lokalno presentation stanje i ne radi business write. `Score = null` ostaje
no-data, ne `0 %`. Kontrole ostaju native buttoni s tipkovničkim/focus stanjem i
`aria-pressed` oznakom.

## Teacher assessment i automatizacija

Source predviđa buduću kratku strukturiranu Teacher procjenu nakon konkretne Speaking
aktivnosti. Phase 5.12 nema zaključan Speaking Activity/Attempt emitter, source identity ni
write route, stoga ne uvodi generički Evidence unos niti inertne Odlično/Dobro kontrole.

Budući workflow smije koristiti postojeće canonical `Production` i `AssistanceLevel`
metapodatke tek kroz owner-scoped, atomski, append-only Evidence emission vezan uz stvarni
source. U osnovnoj verziji ne smije ovisiti o AI-ju. Analiza transkripta, gramatike,
vokabulara, duljine odgovora ili izgovora ostaje blokirana do zasebnog AI/privacy/storage
contracta i Teacher potvrde.

## Namjerno izvan scopea

Nisu uvedeni assessment write, audio upload/recording, transcript, AI analiza, activity feed,
trend, last-activity datum, communication-context breakdown, automatska objašnjenja,
preporuke, overall score, predviđena ocjena, probability, PDF ni Lesson Builder handoff.

## Visual acceptance

Canonical skill lista + desni detaljni panel očuvani su unutar prihvaćenog PLUS 5 shella.
Desktop i mobile dokazi koriste stvarnu prijavu i stvarni API; nema auth bypassa, API
interceptiona, DOM mutationa ni business writeova. Rezultati su u
[`visual-acceptance/phase-5.12`](visual-acceptance/phase-5.12/README.md).
