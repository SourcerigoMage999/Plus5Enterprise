# Phase 4.5 — Recurrence/series consistency tests

## Status

`DONE — čeka završni SA review`

## Datum

`2026-09-16`

## Cilj faze

Zaključati regresijskim i stvarnim SQL testovima recurrence/series invarijante iz
`SCHEDULING_FOUNDATION.md`, ADR-0013, Group create/edit contracta i `SESSION_EDITING.md`, bez
uvođenja novog UI-ja, endpointa, schema promjene ili nedokumentiranog replenishment procesa.

## Implementirano

- determinističan 12-tjedni generator testira isti jedinstveni `(slot, occurrence date)` skup
- one-occurrence promjena eksplicitno dokazuje da mijenja samo Session, postavlja exception i
  ne dira canonical Series ni susjednu instancu
- future-series promjena dokazuje supersession, `PreviousSeriesId`, skraćivanje stare serije i
  bounded successor materializaciju
- ranija ručna iznimka te `InProgress`, `Held` i `Cancelled` occurrencei ostaju netaknuti
- successor preskače njihove datume pa ne poništava eksplicitnu occurrence odluku
- obični budući `Scheduled` occurrencei stare serije otkazuju se bez hard deletea
- ponovni write nad supersedanom instancom ne stvara drugi successor
- dvije konkurentne future-series promjene daju točno jednog successora i kontrolirani
  concurrency/unavailable rezultat za gubitnički write
- postojeći DST, inclusive `EndsOn`, 84-dnevni horizont, ownership, conflict i unique-index
  testovi ostaju dio punog regression portfolija

## Namjerno nije implementirano

- background replenishment, cadence ili worker
- arbitrary/mjesečna recurrence, overnight termini ili conflict override
- promjena dana/strukture Group rasporeda kroz Session edit
- novi API, frontend ekran, notification/reminder ili delivery/Evidence ponašanje

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `backend/src/Plus5.Infrastructure/Scheduling/EfScheduleEditingService.cs` | changed | čuva authoritative exception/non-scheduled occurrence datume pri future-series zamjeni |
| `backend/tests/Plus5.Api.Tests/Scheduling/RecurrenceConsistencySqlTests.cs` | added | stvarni SQL preservation, lineage, idempotency i concurrent-write gate |
| `backend/tests/Plus5.Api.Tests/Groups/GroupScheduleGeneratorTests.cs` | changed | determinističnost, uniqueness i bounded-horizon regresija |
| `docs/SESSION_EDITING.md` | changed | precizira očuvanje eksplicitnih occurrence odluka |
| `docs/ROADMAP.md` | changed | Phase 4.4 approval, Phase 4.5 rezultat i obavezna Phase 4.6 |
| `docs/SCHEDULING_FOUNDATION.md` | changed | zaključava minimalnu replenishment granicu za Phase 4.6 |
| `docs/OPEN_QUESTIONS.md` | changed | odvaja riješen phase placement od preostalog 4.6 contract gatea |
| `docs/DECISION_LOG.md` | changed | ADR-0017 evidentira replenishment prije Phase 5 |
| `docs/DOCUMENTATION_MANIFEST.md` | changed | registrira završni Phase 4.5 audit |
| `docs/summaries/PHASE_4.5_RECURRENCE_SERIES_CONSISTENCY_SUMMARY.md` | added | završni audit trail faze |

## Domain / database promjene

- Novi entiteti/value objects: nema.
- Promijenjena pravila: nema novog business pravila; implementacija je usklađena s već
  zaključanim pravilom da postojeći occurrence, uključujući exception/cancelled, ostaje
  autoritet nad recurrence pravilom.
- Migracije: nema.
- Backfill/data migration: nema.

## API promjene

Nema novih endpointa ni promjene javnog request/response contracta. Postojeći future-series
update sada u `sessionCount` vraća stvarni broj novo materializiranih Sessiona nakon preskakanja
sačuvanih occurrence datuma, što je postojeća semantika tog polja.

## Frontend promjene

Nema. Postojećih 59 frontend testova, lint, TypeScript i production build služe kao regression
gate.

## Security / authorization

Nema nove trust granice. Owner scope, CSRF i backend-authority pravila ostaju nepromijenjeni.
SQL testovi koriste lokalne disposable baze i ne spremaju connection string ili lozinku u Git.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Backend/API/domain/persistence sa stvarnim lokalnim SQL-om | PASS — 146/146, 0 skipped |
| Phase 4.5 SQL preservation + concurrency | PASS — 2/2 |
| Generator consistency/DST/horizon | PASS — 4/4 |
| Architecture dependency granice | PASS — 4/4 u Debugu; Release test DLL izvršavanje blokira host Windows Application Control nakon uspješnog builda |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| Frontend component testovi | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker build, health i non-root runtime | PASS |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] Release build prolazi
- [x] relevantni i puni regression testovi prolaze
- [x] nema migracije
- [x] postojeći auth/validation contract nije promijenjen
- [x] dokumentacija je usklađena

## Arhitekturne odluke

Phase 4.5 testovi izvršavaju postojeći ADR-0013 i ADR-0015. Naknadni SA review donio je
ADR-0017: replenishment je zasebna obavezna Phase 4.6 prije Phase 5.

## Poznati rizici / tehnički dug

- Windows Application Control na ovom hostu povremeno blokira novogenerirane Release test
  DLL-ove (`0x800711C7`). Release solution build prolazi; isti puni suiteovi izvršeni su iz
  Debug artefakta.
- Phase 4.6 cadence/trigger, operational ownership i conflict retry/visibility contract ostaju
  otvoreni te nisu prešutno riješeni.

## Otvorena pitanja

- Točna cadence/trigger, operational ownership i ponašanje conflict occurrencea moraju se
  riješiti u Phase 4.6 contractu prije implementacije.

## Točna početna točka za sljedeću fazu

Phase 4.5 je funkcionalno i regresijski pokrivena, ali time nije završena cijela Schedule faza.
Nakon završnog SA reviewa i zasebnog commit/push odobrenja sljedeća ROADMAP stavka je Phase
4.6 Recurrence materialization replenishment. Phase 5.1 ne počinje prije završnog 4.6
acceptancea.
