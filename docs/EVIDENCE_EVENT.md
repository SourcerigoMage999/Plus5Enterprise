# Evidence Event contract

## Status

**LOCKED business contract — Phase 5.3 implementiran; završni review čeka odobrenje**

## Svrha i granica

`EvidenceEvent` je auditabilan, Student-specifičan zapis da je konkretni izvorni rezultat
pedagoški relevantan dokaz za jednu ili više `KnowledgeComponent` komponenti. Sam `Attempt`,
completion ili accuracy ne stvara Evidence automatski. Emission mora eksplicitno zatražiti
konkretni server-controlled feature workflow.

Phase 5.3 uvodi domenu, persistence, migraciju i interni emission service contract. Ne uvodi
javni generic `POST Evidence` API, UI, production emitere iz Lesson/Homework/Board workflowa,
Attempt model, katalog source kindova, evidence metadata, scoring, mastery ili readiness.

## Model

`EvidenceEvent` sadrži:

- `Id`
- `StudentId`
- `Kind`: `Observation`, `Correction` ili `Invalidation`
- `SourceKind` i `SourceId`
- `OccurredAtUtc` i `RecordedAtUtc`
- nullable `SupersedesEvidenceEventId`
- nullable `ReasonCode`

`SourceKind` i `ReasonCode` su stabilni uppercase kodovi duljine najviše 64 znaka, ograničeni
na `A-Z`, `0-9`, `_`, `-` i `.`. `ReasonCode` postoji samo na Correction/Invalidation događaju.
`SourceKind` je neutralan provenance discriminator, a nije `EvidenceType` ni `EvidenceContext`.

`OccurredAtUtc` označava vrijeme pedagoškog događaja. `RecordedAtUtc` označava vrijeme zapisa
konkretnog EvidenceEventa. Persistence prihvaća samo UTC offset `+00:00`.

## Provenance i idempotency

Observation je root chaina. Za jednog Studenta kombinacija
`(StudentId, SourceKind, SourceId)` smije imati najviše jedan root. Ponovljeni emission istog
izvornog rezultata ne stvara drugi nezavisni Evidence chain.

Correction i Invalidation nasljeđuju isti `StudentId`, `SourceKind` i `SourceId`. Correction ne
smije pretvoriti rezultat u dokaz drugog učenika ili drugog sourcea. Novi stvarni rezultat
dobiva novi source identity i novi root.

## KnowledgeComponent targeti

Veza je eksplicitni M:N `EvidenceEventKnowledgeComponent` s composite PK-om
`(EvidenceEventId, KnowledgeComponentId)`. Observation i Correction moraju imati najmanje
jedan jedinstveni target; Invalidation nema target.

Direct target može biti samo leaf komponenta iz `Published` ili `Retired` KnowledgeModel
verzije. `KnowledgeArea`, parent komponenta i komponenta Draft modela nisu dopušteni.
Retired target ostaje valjan radi povijesne reproduktivnosti versioned source artifacta.

Mapping nema weight, contribution, score, mastery, readiness ni drugo algoritamsko polje.
Jedan rezultat relevantan za više komponenti ostaje jedan EvidenceEvent s više targeta.

Aggregate factory i jedini interni emission service zahtijevaju najmanje jedan target za
Observation/Correction. SQL trigger ne dopušta Draft, parent ni Invalidation target, a
composite PK ne dopušta duplikat. SQL Server nema deferred cross-table constraint, stoga se
obavezna minimalna kardinalnost zapisuje atomski kroz isti interni service transaction; nema
javnog ni generičkog write surfacea koji može emitirati djelomični aggregate.

## Append-only lifecycle

Valjani lanac je linearan:

```text
Observation → 0..N Correction → optional Invalidation
```

- Observation nema predecessor ni ReasonCode i nosi puni početni target payload.
- Correction mora referencirati neposredni predecessor, imati ReasonCode i nositi cijeli novi
  efektivni Phase 5.3 payload: ispravljeni occurrence timestamp i potpuni skup targeta.
- Invalidation mora referencirati neposredni predecessor i imati ReasonCode. Ne nosi novi
  pedagoški signal ni KnowledgeComponent target.
- Invalidation je terminalna.

Postojeći redak i mapping ne uređuju se niti hard-deleteaju. Unique filtered successor indeks
zabranjuje fork. Composite self-FK zahtijeva isti Student i source identity. CHECK constraint,
unique indeksi i triggeri zabranjuju self-reference, cycle, drugi root, fork, supersession
nakon Invalidationa te UPDATE/DELETE Evidence povijesti.

Privacy/legal erasure nije definirana u Phase 5.3 i ne smije se improvizirati fizičkim
brisanjem Evidence povijesti.

## Ownership i sigurnost

Evidence pripada Student kontekstu. Nema dupliciranog `TeacherAccountId` na EvidenceEventu.
Interni service za svaki write server-side provjerava:

```text
authenticated Teacher → owns Student
```

Nepostojeći i tuđi Student/Evidence imaju privacy-preserving `NotFound` rezultat. Targeti su
globalne kontrolirane Knowledge reference i ne mogu promijeniti ownership chain. Nema
korisničkog free-form Evidence unosa.

## Transakcija i konkurentnost

Emission service radi u `Serializable` transakciji. Event i svi target mapping retci zapisuju
se atomski. Domain/application validacija daje kontrolirani rezultat, a DB ostaje završna
zaštita za root idempotency, linearnost, source/student identitet i Knowledge target integrity.
Konkurentni duplikat ili pokušaj drugog successora završava conflict rezultatom.

## SQL integrity mapa

| Invarijanta | Zaštita |
|---|---|
| Student postoji | restriktivni FK |
| jedan root po Student/source identitetu | filtered unique indeks |
| Observation/Correction/Invalidation shape | CHECK constraint |
| isti Student i source u chainu | composite self-FK |
| najviše jedan successor | filtered unique indeks |
| bez self-reference/cyclea | CHECK + SQL trigger |
| Invalidation je terminalna | SQL trigger |
| nema event UPDATE/DELETE | AFTER UPDATE + INSTEAD OF DELETE triggeri |
| bez mapping UPDATE/DELETE | SQL trigger |
| Invalidation nema target | SQL trigger |
| target nije Draft i jest leaf | SQL trigger |
| bez duplicate targeta | composite PK |
| Observation/Correction imaju target | domain factory + atomski interni emission service |

## Namjerno odgođeno

- difficulty, assistance level, EvidenceType i EvidenceContext — Phase 5.4
- weighting, confidence, decay, thresholds, mastery i readiness — Phase 5.5
- stvarni Lesson/Homework/Board emitteri i Attempt lifecycle — njihove feature faze
- Student progress i drugi UI prikazi — kasnije ROADMAP faze
- manualni postotci ili AI inference — nisu dio Phase 5.3

## Acceptance

- migracija prolazi clean i upgrade apply na stvarnom SQL Serveru
- root emission je idempotentan i owner-scoped
- Correction/Invalidation lanac ostaje linearan, auditabilan i append-only
- SQL odbija Draft, parent i Invalidation target te UPDATE/DELETE, fork, cycle i terminalni
  supersession
- Published i Retired leaf targeti ostaju valjani
- nema produkcijskih Evidence redaka, javnog API-ja ni emitera iz budućih featurea

