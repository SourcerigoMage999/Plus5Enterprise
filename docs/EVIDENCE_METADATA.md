# Evidence Metadata contract

## Status

**LOCKED business contract — Phase 5.4**

## Svrha i granica

Phase 5.4 proširuje append-only `EvidenceEvent` iz Phase 5.3 potpunim povijesnim metadata
snapshotom. U granicama Phase 5.4 metadata nema numerički učinak; naknadno zaključani
`readiness-v1` weight katalog i odvojeni `PerformanceScore` definirani su isključivo u
`MASTERY_READINESS.md`.

U scopeu su četiri canonical metadata vrijednosti, EvidenceEvent snapshot, correction semantics,
domain/DB validacija, migracija, interni emission service, testovi i dokumentacija. Izvan scopea
ostaju Task redesign, UI, javni API, production emitters, AI, weighting, decay, confidence,
thresholds, mastery i readiness.

## Canonical katalozi v1

Katalozi su bounded domain codeovi. U persistenceu se spremaju stabilni canonical kodovi, ne
lokalizirani tekst. Ne uvode se katalog tablice.

### Difficulty

`Difficulty` je cijeli broj `1..5`:

- `1` — najmanja zahtjevnost
- `5` — najveća zahtjevnost

Skala je samo ordinalna. Phase 5.4 ne definira multiplier, contribution ili drugi matematički
učinak pojedine vrijednosti.

### EvidenceType

| Canonical code | Hrvatska UI labela | Semantika |
|---|---|---|
| `Recognition` | Prepoznavanje | učenik prepoznaje ili identificira točan koncept/odgovor |
| `Understanding` | Razumijevanje | učenik pokazuje razumijevanje značenja ili pravila |
| `Application` | Primjena | učenik primjenjuje znanje u strukturiranom problemu/zadatku |
| `Production` | Produkcija | učenik samostalno proizvodi odgovor ili izvedbu |

Test, Homework i Quiz nisu EvidenceType. Oni opisuju kontekst ili izvor.

### AssistanceLevel

| Canonical code | Hrvatska UI labela |
|---|---|
| `Independent` | Samostalno |
| `MinorAssistance` | Uz manju pomoć |
| `SignificantAssistance` | Uz značajnu pomoć |
| `NotObserved` | Pomoć nije opažena |

`NotObserved` nije isto što i `Independent`. Odsutnost pouzdanog podatka ne smije se tumačiti kao
samostalan rad.

### EvidenceContext

| Canonical code | Hrvatska UI labela |
|---|---|
| `Lesson` | Sat |
| `Homework` | Domaća zadaća |
| `Assessment` | Provjera / test |
| `IndependentPractice` | Samostalna vježba |

`Assessment` obuhvaća test, kviz ili drugu formalniju provjeru. Tehnička površina poput PLUS 5
Ploče nije zaseban context: stvarna nastavna situacija određuje jedan od četiri codea.
`SourceKind` i `EvidenceContext` ostaju odvojeni pojmovi.

## EvidenceEvent snapshot

Svaki `Observation` i `Correction` mora imati sva četiri polja:

```text
Difficulty
EvidenceType
AssistanceLevel
EvidenceContext
```

Nijedno nije nullable na efektivnom Observation/Correction snapshotu. `Invalidation` ne nosi
KnowledgeComponent target ni jedno metadata polje jer ne predstavlja novu pedagošku procjenu.

Snapshot na EvidenceEventu je authoritative povijesni zapis. Buduća promjena Taska, activity
templatea, Homeworka, LessonPlana ili source klasifikacije ne mijenja stari EvidenceEvent i ne
pokreće backfill.

## Source authoring granica

`Difficulty` može nastati kao authoring metadata budućeg procjenjivog Taska/Activityja. Budući
feature contract može na sourceu definirati i intended `EvidenceType`. Phase 5.4 zato ne uvodi
Task model niti mijenja buduće Task tablice.

`AssistanceLevel` i `EvidenceContext` svojstva su konkretnog evidence trenutka, a nisu intrinsic
svojstva Taska. Isti Task može biti riješen samostalno, uz pomoć na satu ili kao provjera.

## Correction i Invalidation

Correction uvijek nosi potpuni novi efektivni snapshot: sva četiri metadata polja, ispravljeni
occurrence timestamp i puni KnowledgeComponent target skup. Nema field-by-field patcha ni
read-time nasljeđivanja pojedinih metadata vrijednosti iz starijih događaja.

Prethodni Observation/Correction ostaje nepromijenjen. Efektivni evidence je najnoviji valjani
Observation/Correction u linearnom chainu. Invalidation je terminalna i sva četiri metadata polja
na njoj moraju biti `NULL`.

## Stabilnost i versioning

- persisted code ne mijenja značenje i ne reciklira se
- budući katalog smije se proširiti samo aditivno
- promjena semantike postojećeg codea zahtijeva novi formalni metadata contract version
- Phase 5.4 ne uvodi zasebnu `EvidenceMetadataVersion` kolonu; stabilni v1 codeovi dovoljno čuvaju
  trenutačnu povijesnu interpretaciju
- nema backfilla Evidence podataka ili runtime joina na današnje source metadata vrijednosti

## Domain i persistence invarijante

- `Difficulty` mora biti `1..5`
- enum vrijednosti moraju pripadati zaključanim v1 katalozima
- Observation/Correction moraju imati sva četiri metadata polja
- Invalidation ne smije imati nijedno metadata polje
- metadata se ne mijenja in-place; postojeći append-only UPDATE/DELETE trigger ostaje authority
- novi canonical codeovi spremaju se kao bounded string vrijednosti
- DB CHECK constrainti štite range, dopuštene codeove i conditional lifecycle shape

## Acceptance

- Observation i Correction domain factoryji zahtijevaju valjani potpuni snapshot
- interni emission service prima i sprema potpuni snapshot atomski s eventom i KC targetima
- Correction dokazuje potpunu zamjenu bez izmjene Observationa
- Invalidation nema metadata
- stvarni SQL odbija difficulty izvan `1..5`, nepoznate codeove, djelomične snapshote i metadata
  na Invalidationu
- stvarni SQL i dalje odbija metadata UPDATE postojećeg eventa
- migracija, EF model, idempotentni script, regression, auditi i Docker runtime gate prolaze
- nema Task modela, scoringa/readinessa, UI-ja, javnog API-ja ili production emitera
