# Phase 0.2 — Product/domain glossary

## Status

`DONE`

Commit/push gate: `AWAITING OWNER REVIEW`

## Datum

`2026-08-23`

## Cilj faze

Zaključati canonical nazive i značenja ključnih PLUS 5 pojmova te ukloniti terminološke kontradikcije prije izrade domenskog i podatkovnog modela.

## Implementirano

- dodan canonical `DOMAIN_GLOSSARY.md` za kod, API, bazu i tehničku dokumentaciju
- definirani akteri, organizacija nastave, raspored, curriculum/knowledge, materijali, zadaci, evidence i supporting product pojmovi
- zaključane razlike Program–Group, SchoolGrade–ProficiencyLevel, RegularGroupSchedule–Session, LearningActivity–AssessableTask, Tag–KnowledgeComponent i MasteryEstimate–ReadinessEstimate
- hrvatski/UI termini mapirani su na canonical English nazive
- eksplicitno su označene granice koje glossary ne smije riješiti bez budućih business odluka
- obavezni redoslijed čitanja dokumentacije dopunjen je glossaryjem
- ROADMAP 0.2 označen je `DONE`

## Namjerno nije implementirano

- aplikacijski kod, projekti, baza, migracije, API i frontend
- authentication/permissions contract
- kardinalnosti Student–Program–Group
- model ponavljanja termina i iznimki
- readiness/mastery/confidence algoritmi i weighting
- katalozi evidence metapodataka i file policy

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `docs/DOMAIN_GLOSSARY.md` | added | Canonical product/domain glossary |
| `docs/README.md` | changed | Glossary dodan u obavezni redoslijed čitanja |
| `docs/ROADMAP.md` | changed | Status faze 0.2 postavljen na DONE i deliverable evidentiran |
| `docs/summaries/PHASE_0.2_PRODUCT_DOMAIN_GLOSSARY_SUMMARY.md` | added | Obavezni phase handoff zapis |

## Domain / database promjene

- Novi entiteti/value objects: nema implementiranih tipova; uveden je samo canonical vokabular za buduće modele.
- Promijenjena pravila: terminološke razlike navedene u glossaryju postaju obavezne.
- Migracije: nema.
- Backfill/data migration: nema.

## API promjene

- Nema API-ja ni contract promjena.

## Frontend promjene

- Nema routeova, ekrana ni komponenti.

## Security / authorization

- Glossary izričito ne poistovjećuje Teacher, Student ili Guardian s `UserAccount` i ne pretpostavlja nedokumentirane permissions.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| provjera obaveznih glossary pojmova i jedinstvenih canonical naziva | PASS |
| provjera internih Markdown poveznica/referenci i trailing whitespacea | PASS |
| provjera da su izmjene ograničene na dokumentaciju faze 0.2 | PASS |
| build / unit / integration testovi | N/A — repository bootstrap još ne postoji |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build nije primjenjiv prije faze 0.4
- [x] relevantne dokumentacijske provjere prolaze
- [x] migracije nisu primjenjive
- [x] auth/validation nisu proizvoljno definirani
- [x] dokumentacija je ažurirana

## Arhitekturne odluke

Nema novog ADR-a. Glossary normalizira postojeće zaključane poslovne razlike bez izbora nove tehnologije ili arhitekture.

## Poznati rizici / tehnički dug

- Izvorni Markdown snapshotovi zadržavaju encoding artefakte poput `u#U010denik`; glossary koristi normalizirane hrvatske nazive, ali Phase 0.2 ne prepravlja izvorne snapshotove.
- Budući domain model mora provjeriti kardinalnosti i lifecycle pravila prije persistence locka.

## Otvorena pitanja

- Postojeća pitanja iz `OPEN_QUESTIONS.md` ostaju važeća; nijedno nije proizvoljno zatvoreno u ovoj fazi.

## Točna početna točka za sljedeću fazu

Phase 0.3 Technology architecture decision već je dokumentirana i označena READY sa zadovoljenim preduvjetom. Nakon potvrde phase reviewa treba uskladiti njezin status sa stvarnim acceptance kriterijima, a zatim otvoriti Phase 0.4 Repository/bootstrap i izraditi minimalan buildable/testable React + TypeScript / .NET 10 modularni monolit bez business feature logike.
