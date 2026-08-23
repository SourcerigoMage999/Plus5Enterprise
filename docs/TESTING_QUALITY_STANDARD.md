# TESTING_QUALITY_STANDARD

## Status

**MANDATORY v1.0 — 2026-08-23**

## 1. Quality gate

Nijedna ROADMAP podfaza nije `DONE` samo zato što “radi na računalu developera”.

Mora proći primjenjive:

- build
- static analysis/lint
- automated tests
- migration provjeru
- security review
- self-review
- dokumentacijski update

## 2. Test pyramid / portfolio

### Unit

Koristiti za:

- domain invariants
- calculations
- state transitions
- validation/business rules bez infrastrukture

### Integration

Koristiti za:

- EF Core + stvarni SQL Server behavior gdje provider-specifično ponašanje nosi rizik
- API authorization/authentication boundary
- transaction/concurrency behavior
- serialization/error contract
- external adapter contract gdje je izvedivo

In-memory provider nije zamjena za SQL Server integration test kritične relational logike.

### Architecture tests

Automatski čuvati ključna pravila kada repo dobije strukturu:

- dependency direction
- zabranjene reference između layera/modula
- naming/placement pravila koja imaju arhitekturnu vrijednost

### Frontend tests

Prioritet:

- kritične komponente i forme
- API/error/loading state contract
- accessibility ponašanje gdje je rizik

### E2E

Prije releasea pokriti kritične korisničke journeyje, ne svaki detalj UI-a.

## 3. Test quality

- test mora provjeravati ponašanje, ne samo implementacijski detalj
- determinističan
- izoliran od vanjskog interneta
- razumljiv failure output
- ne koristiti proizvoljne `sleep` kao sinkronizaciju kada postoji determinističan signal

## 4. Database tests

Svaka rizična migration/schema promjena mora provjeriti:

- clean create path
- upgrade path kada postoje prethodni podaci
- constraint behavior
- concurrency behavior ako se uvodi
- query/index behavior kada je performance cilj faze

## 5. Security tests

Za zaštićene endpointove minimalno provjeriti relevantne kombinacije:

- unauthenticated → 401
- authenticated but forbidden → 403
- authorized → success
- object ownership/scope ne može biti zaobiđen promjenom ID-a
- invalid input ne prolazi trust boundary

## 6. Regression

Bug fix mora, gdje je izvedivo, prvo dobiti test koji reproducira bug ili test dodan uz fix kako se bug ne bi vratio.

## 7. Performance/load testing

Uvodi se ciljano i obavezno prije production performance locka.

Scenariji moraju koristiti realističan mix:

- login/authenticated requests
- list/detail read
- writes
- schedule/knowledge queries prema stvarnom use caseu

Metrike:

- throughput
- p50/p95/p99 latency
- error rate
- CPU/RAM
- DB connections/waits
- container restarts/timeouts

## 8. CI minimum

Kada se CI uvede, quality gate mora najmanje:

1. restore/install
2. backend build
3. backend tests
4. frontend typecheck/build
5. frontend lint/tests
6. dependency vulnerability check
7. migration consistency check kada je moguće

Failure gate se ne zaobilazi bez dokumentiranog razloga.
