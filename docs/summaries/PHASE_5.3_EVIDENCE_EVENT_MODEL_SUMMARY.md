# Phase 5.3 — Evidence Event model

## Status

`FINAL LOCK — odobreno prije Phase 5.4 odluke 2026-09-24`

## Datum

`2026-09-24`

## Cilj faze

Uvesti Student-specific Evidence temelj s obaveznim provenanceom, idempotentnim root emissionom,
leaf KnowledgeComponent targetima i auditabilnim append-only correction/invalidation lifecycleom,
bez automatskog pretvaranja Attempta u Evidence i bez preuranjenog metadata/scoring/UI scopea.

## Implementirano

- `EvidenceEvent` aggregate s `Observation`, `Correction` i `Invalidation` vrstama
- odvojeni `OccurredAtUtc` i `RecordedAtUtc`
- bounded/stabilni `SourceKind` i `ReasonCode`
- obavezni source provenance i unique root po `(StudentId, SourceKind, SourceId)`
- čisti M:N `EvidenceEventKnowledgeComponent` bez weight/score polja
- Observation/Correction target samo na leafu Published/Retired KnowledgeModela
- Correction kao novi puni efektivni Phase 5.3 payload bez izmjene prethodnika
- terminalna Invalidation bez Knowledge targeta
- linearni same-Student/same-source chain bez self-referencea, cyclea ili forka
- append-only event i mapping povijest bez feature hard-deletea
- interni `IEvidenceEmissionService` s Teacher→Student ownership provjerom
- `Serializable` atomski zapis eventa i svih target mappinga
- EF konfiguracija, migracija, indeksi, CHECK constrainti, composite FK-ovi i SQL triggeri
- domain/model i stvarni SQL regression testovi
- formalni `EVIDENCE_EVENT.md` contract i ADR-0020

## Namjerno nije implementirano

- javni generic Evidence endpoint ili frontend ekran
- stvarni Lesson, Homework ili Board emitters
- `Attempt` model i automatski emission iz completiona/accuracyja
- difficulty, assistance level, EvidenceType ili EvidenceContext iz Phase 5.4
- mastery, readiness, confidence, weighting, decay, thresholds ili postotci iz Phase 5.5
- production source-kind katalog, Evidence seed ili backfill
- AI inference, Student progress prikaz ili manualni “znanje 80%” unos
- privacy/legal fizičko brisanje bez zasebnog budućeg contracta

## Promijenjene / dodane cjeline

| Područje | Promjena |
|---|---|
| Domain | EvidenceEvent aggregate, lifecycle enum, Knowledge target i junction model |
| Application | interni observation/correction/invalidation commandi, rezultati i service port |
| Infrastructure | owner-scoped EF emission service i DI registracija |
| Persistence | dva DbSeta, EF konfiguracije, indeksi, constrainti i trigger metadata |
| Migracija | `20260924130925_AddEvidenceEventModel` i model snapshot |
| Testovi | 5 domain/EF testova i 6 stvarnih SQL scenario testova |
| Dokumentacija | Evidence contract, ADR-0020, ROADMAP, pitanja, persistence, manifest i backlog |

## Relational integrity

- restriktivni Student i KnowledgeComponent FK-ovi
- composite alternate key + self-FK fizički vežu successor uz isti Student/source identity
- filtered unique root indeks osigurava jedan početni emission po source rezultatu i Studentu
- filtered unique successor indeks sprječava correction fork
- CHECK constrainti štite kind/lifecycle shape, self-reference, bounded kodove, source ID i UTC
- event triggeri odbijaju UPDATE/DELETE, cycle i supersession terminalne Invalidation
- mapping trigger odbija UPDATE/DELETE te target na Invalidation, Draft model ili parent komponentu
- composite mapping PK odbija duplicate target
- aggregate + jedini interni service zahtijevaju najmanje jedan target za
  Observation/Correction i zapisuju aggregate atomski; nema javnog generic write surfacea
- migracija ne mijenja postojeće business retke i ne stvara Evidence podatke

## API / frontend promjene

Nema novog endpointa, routea, request/response HTTP contracta ili UI-ja. Emission port je interni
application contract koji budući feature workflow smije pozvati tek kada njegova faza eksplicitno
zaključa uvjete emissiona.

## Security / authorization

- Evidence nema duplicirani `TeacherAccountId`; ownership se izvodi preko `Student`
- svaki interni write server-side provjerava da Teacher owns Student
- tuđi/nepostojeći Student ili Evidence vraća privacy-preserving `NotFound` rezultat
- nema free-form korisničkog write surfacea ili production seeda
- ReasonCode je bounded code, ne audit note, stack trace ili proizvoljni osjetljivi tekst
- DB integritet vrijedi i za izravne SQL inserte leaf/Draft/chain scenarija

## Testovi i evidence

| Gate | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Phase 5.3 domain/EF testovi | PASS — 5/5 |
| Phase 5.3 stvarni SQL testovi | PASS — 6/6, 0 skipped |
| Puni Backend/API/domain/persistence suite sa stvarnim SQL-om | PASS — 189/189, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| EF pending-model check (Release model) | PASS — nema promjena nakon migracije |
| Idempotentni migration script | PASS — obje tablice i sva tri triggera prisutni |
| Frontend testovi | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker API/migrations/frontend build | PASS |
| Compose migration i runtime health | PASS — init/migration exit 0; baza, API i frontend healthy |
| HTTP smoke | PASS — API live/ready i frontend vraćaju 200 |
| Non-root runtime | PASS — API UID 1654; frontend UID 101 `nginx` |
| Runtime schema | PASS — migration 1/1, 2/2 tablice i 3/3 triggera postoje |
| Production Evidence podaci | PASS — 0 EvidenceEvent i 0 mapping redaka |

Puni SQL suite izvršen je u projektnom pinned .NET SDK containeru koji dijeli network namespace
lokalnog SQL Server containera. Time postojeći testovi dobivaju očekivani `localhost:1433`, a
svi disposable database scenariji stvarno izvršavaju cijeli migration chain.

## Pokriveni Phase 5.3 scenariji

- upgrade migracija čuva postojeće podatke i počinje s praznim Evidence tablicama
- owned observation → correction → invalidation chain i potpuni target replacement
- tuđi Student/Evidence, duplicate root, correction fork i post-terminal write se odbijaju
- SQL odbija event/mapping UPDATE i DELETE
- SQL odbija cross-Student/source successor, self/cycle i duplicate successor
- SQL odbija Draft, parent i Invalidation target
- Published i Retired leaf targeti prolaze
- event/mapping zapis ostaje atomski, idempotentan i auditabilan

## Self-review

- [x] Attempt, completion i accuracy nisu automatski Evidence
- [x] emission je eksplicitan, server-controlled i nema generic javni API
- [x] Evidence je Student-specific i ownership nema paralelnu istinu
- [x] source provenance i root idempotency fizički su zaštićeni
- [x] M:N targeti su leaf-only i dopuštaju Published/Retired, ne Draft
- [x] Correction/Invalidation su append-only i chain nema fork/cycle
- [x] Invalidation je terminalna i bez targeta
- [x] nema feature hard-deletea ni in-place rewritea
- [x] nema Phase 5.4/5.5 scope creepa
- [x] nema produkcijskih emitera, API-ja, UI-ja, seeda ili backfilla
- [x] build/test/format/audit/EF/Docker gateovi prolaze
- [x] dokumentacija i implementacija imaju isti contract

## Poznate granice

- SQL Server nema deferred cross-table assertion za minimalni broj junction redaka. Obavezni
  target za Observation/Correction zato je aggregate/service invarijanta unutar iste
  `Serializable` transakcije; leaf/Draft/Invalidation/duplicate target pravila ostaju SQL-level.
- Budući source kind ne smije se aktivirati dok njegova feature faza ne definira kada i kako
  rezultat postaje pedagoški Evidence.
- Evidence metadata i readiness matematika ostaju zasebni ROADMAP gateovi.

## Točna početna točka za sljedeću fazu

Phase 5.3 je završno zaključana. Phase 5.4 počinje zasebnim zaključanim metadata contractom bez
ponovnog otvaranja provenance, target ili append-only lifecycle odluka.
