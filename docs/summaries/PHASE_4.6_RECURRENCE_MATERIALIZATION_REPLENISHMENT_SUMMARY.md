# Phase 4.6 — Recurrence materialization replenishment

## Status

`DONE — FINAL LOCK`

## Datum

`2026-09-16`

## Cilj faze

Održavati zaključani rolling 12-week horizont konkretnih `Session` zapisa za aktivne,
nesupersedane open-ended `RecurringSessionSeries`, bez regeneriranja postojećih occurrencea,
potajnog mijenjanja rasporeda ili divergentnog rezultata pri više API instanci.

## Implementirano

- `Plus5.Api` `BackgroundService` radi startup catch-up nakon spremnosti baze i zatim svakih
  šest sati
- SQL-backed globalni lease s istekom dopušta samo jednom workeru obradu; gubitnik uredno
  preskače run
- batch je ograničen na 50 serija, a svaka serija obrađuje se u kratkoj Serializable
  transakciji uz obnovu leasea
- horizont se računa iz lokalnog dana u vremenskoj zoni serije i održava sljedećih 12 tjedana
- prije inserta ponovno se provjeravaju aktivni context, lineage/supersession, postojeći
  Session, preserved occurrence, DST, Teacher i Location conflict
- postojeći exception, `Cancelled`, `Held`, `InProgress` i svi drugi postojeći occurrence
  datumi ostaju authoritative i nikada se ne regeneriraju
- conflict ili DST problem preskače samo jedan occurrence, ne mijenja canonical seriju i ne
  zaustavlja druge occurrencee
- `ScheduleMaterializationIssue` trajno bilježi dopuštenu kategoriju, occurrence datum,
  first/last seen, broj pokušaja i resolution vrijeme bez stack tracea ili osjetljivog payloada
- isti `(SeriesId, OccurrenceLocalDate, IssueType)` upserta se bez duplikata; uspješna kasnija
  materializacija označava issue riješenim
- tranzijentni SQL kvar ima najviše tri pokušaja s bounded exponential backoffom; business/DST
  problem čeka sljedeći scheduled run
- strukturirani logovi pokrivaju početak, lease acquired/skipped/lost, agregirane rezultate,
  trajanje, retry i failure
- migracija dodaje issue/lease tablice, jedinstveni issue ključ i filtrirani indeks queuea za
  aktivne open-ended serije

## Namjerno nije implementirano

- Teacher/Admin UI ili javni API za materialization issuee
- conflict override, automatsko pomicanje termina, zamjena lokacije ili izmjena recurrence
  serije iz background procesa
- arbitrary/mjesečna recurrence, overnight termini, reminder ili notification delivery
- vanjski cron, SQL Agent ili frontend ownership replenishmenta

## Promijenjene / dodane datoteke

| Područje | Datoteke / promjena |
|---|---|
| Domain | `ScheduleMaterializationIssue`, dopušteni issue tipovi |
| Application | `ScheduleMaterializationContracts.cs` worker/service rezultat i status contract |
| Infrastructure | EF replenishment servis, conflict detector, issue tracker, retry policy i SQL lease manager |
| Persistence | lease/issue konfiguracije, DbSetovi, queue indeks i `AddScheduleMaterializationReplenishment` migracija |
| API | hosted `ScheduleMaterializationWorker` i DI registracija |
| Testovi | issue lifecycle, worker cadence/startup te stvarni SQL idempotency, lease, DST, migration i concurrency gateovi |
| Dokumentacija | ADR-0017, recurrence contract, scheduling/persistence/observability, pitanja, ROADMAP i manifest |

## Domain / database promjene

- novi audit entitet `ScheduleMaterializationIssue`
- interna koordinacijska tablica `ScheduleMaterializationLeases`
- jedinstveni ključ nad `(RecurringSessionSeriesId, OccurrenceLocalDate, IssueType)`
- restriktivni FK prema `RecurringSessionSeries`; brisanje serije ne smije ukloniti audit trag
- filtrirani queue indeks nad aktivnim open-ended serijama
- migracija `20260916153606_AddScheduleMaterializationReplenishment`; nema backfilla ni promjene
  postojećih `Session`/series podataka
- EF provjera potvrđuje da nema pending model promjena

## API / frontend promjene

Nema novog javnog endpointa, request/response contracta ni frontend promjene. Faza je namjerno
operativni backend capability.

## Security / authorization

- worker čita i piše kroz postojeći backend persistence trust boundary
- lease i issue tablice nisu javno izložene
- issue zapis ne sadrži exception stack trace, PII ni slobodni osjetljivi payload
- connection stringovi i lozinke ostaju u lokalnom environment/secrets sloju i nisu spremljeni
  u Git

## Testovi i evidence

| Naredba / suite | Rezultat |
|---|---|
| Debug solution build | PASS — 0 warninga, 0 grešaka |
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Phase 4.6 domain/worker/stvarni SQL | PASS — 9/9 |
| Puni Backend/API/domain/persistence suite sa stvarnim SQL-om | PASS — 155/155, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| `dotnet format --verify-no-changes --no-restore` | PASS — 0 promijenjenih datoteka |
| EF pending-model check | PASS — nema promjena nakon migracije |
| Frontend component testovi | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker API/migrations/frontend build | PASS |
| Compose migration i runtime health | PASS — migration/init exit 0; API, baza i frontend healthy |
| HTTP smoke | PASS — `/health/live`, `/health/ready` i frontend vraćaju 200 |
| Non-root runtime | PASS — API UID 1654; frontend UID 101 `nginx` |
| Startup replenishment evidence | PASS — lease acquired; 1 serija scanned, 0 created, 12 existing occurrencea skipped, 0 issuea |

Windows Application Control na hostu blokira izvršavanje svježe generiranih test DLL-ova s
`0x800711C7`. Zbog toga su završni Phase 4.6 i puni regression suite izvršeni u lokalnom pinned
.NET SDK Docker imageu, sa sourceom montiranim read-only, protiv stvarnog lokalnog SQL Servera.
Host Debug i Release buildovi te format provjera normalno prolaze.

## Self-review

- [x] scope ostaje unutar zaključanog 4.6 contracta
- [x] nema nedokumentiranih business pretpostavki ni UI/API proširenja
- [x] per-occurrence problem ne ruši cijeli batch
- [x] SQL lease, expiry takeover i parallel-worker ponašanje testirani su na stvarnom SQL-u
- [x] idempotentnost, preserved occurrence i issue resolution testirani su na stvarnom SQL-u
- [x] migracija prolazi upgrade/idempotency gate i nema pending model promjena
- [x] puni backend/frontend/architecture regression ostaje zelen
- [x] security auditi, Docker health i non-root runtime prolaze
- [x] dokumentacija i audit trail su usklađeni

## Arhitekturne odluke

Implementacija izvršava ADR-0017 i zaključani `RECURRENCE_MATERIALIZATION.md`. SQL lease je
infrastrukturna koordinacija, `ScheduleMaterializationIssue` je durable operational evidence, a
canonical `RecurringSessionSeries` ostaje jedini izvor rasporeda.

## Poznati rizici / tehnički dug

- Windows Application Control ograničenje hosta ostaje okolišni problem; testni container daje
  reproducibilan završni evidence bez promjene produkcijskog koda.
- Teacher conflict override i eventualni UI za issuee ostaju izvan Phase 4.6, kako je izričito
  zaključano.

## Otvorena pitanja

Nema otvorenog business ponašanja unutar Phase 4.6. Odgođeni Schedule gateovi ostaju zapisani
u `OPEN_QUESTIONS.md` i ne blokiraju ovaj contract.

## Točna početna točka za sljedeću fazu

Završni SA acceptance Phase 4.6 i ukupne Phase 4 dan je 2026-09-16. Gate je zadovoljen i
Phase 5.1 smije početi prema zasebno zaključanom contractu.
