# Mastery / Readiness v1 contract

## Status

**LOCKED business contract — Phase 5.5**

## Svrha i granica

Phase 5.5 uvodi determinističku, rebuildable procjenu znanja iz zaključane Evidence povijesti.
`MasteryEstimate` procjenjuje ovladanost konkretnom KnowledgeComponent komponentom, dok
readiness status daje klasifikaciju dobivene procjene. Algoritam nije AI model, školska ocjena
niti ručno upisan postotak.

U scopeu su `PerformanceScore`, v1 weighting, temporal decay, effective-chain resolution,
leaf/parent/Area/CurriculumOutcome agregacija, confidence, readiness, projekcije, event-driven
recalculation i daily refresh. Nema UI-ja, javnog Evidence API-ja, production emitera,
Teacher overridea, preporuka, intervention enginea, Task modela ni personaliziranih weightova.

## Pedagoški rezultat Evidencea

Svaki `Observation` i `Correction` nosi obavezni `PerformanceScore` u rasponu `0.00..1.00`:

- `0.00` — potpuno neuspješan rezultat
- `0.50` — djelomično uspješan rezultat
- `1.00` — potpuno uspješan rezultat

`Invalidation` nema PerformanceScore. Correction nosi puni novi score; append-only povijest ne
mijenja prethodni event. Vrijednost mora doći iz budućeg konkretnog server-controlled emittera;
Phase 5.5 ne uvodi generic write endpoint ni emitter workflow.

## Weight katalog `readiness-v1`

Weight određuje dokaznu vrijednost u agregaciji, a ne mijenja PerformanceScore:

```text
Weight = DifficultyWeight × EvidenceTypeWeight × AssistanceWeight × ContextWeight × RecencyWeight
```

| Difficulty | Weight |
|---:|---:|
| 1 | 0.80 |
| 2 | 0.90 |
| 3 | 1.00 |
| 4 | 1.10 |
| 5 | 1.20 |

| EvidenceType | Weight |
|---|---:|
| Recognition | 0.75 |
| Understanding | 0.90 |
| Application | 1.00 |
| Production | 1.15 |

| AssistanceLevel | Weight |
|---|---:|
| Independent | 1.00 |
| MinorAssistance | 0.85 |
| SignificantAssistance | 0.60 |
| NotObserved | 0.75 |

| EvidenceContext | Weight |
|---|---:|
| Lesson | 0.90 |
| Homework | 0.85 |
| Assessment | 1.00 |
| IndependentPractice | 0.90 |

## Temporal decay i effective chain

Recency se računa iz `OccurredAtUtc`, uz half-life od 90 dana i floor `0.10`:

```text
RecencyWeight = max(0.10, 0.5 ^ (AgeDays / 90))
```

Za jedan linearni Evidence chain računa se samo najnoviji efektivni Observation/Correction.
Ako chain završava Invalidationom, cijeli se chain isključuje. Prethodnik i Correction nikada
se ne zbrajaju. Jedan chain koji targetira više leafova ostaje jedan različiti chain za
confidence count na agregiranim razinama.

## Leaf Mastery i confidence

Za leaf KnowledgeComponent:

```text
Mastery = Σ(PerformanceScore × Weight) / Σ(Weight)
EffectiveEvidenceWeight = Σ(Weight)
```

Bez Evidencea `Score = null`, `Confidence = NoData` i
`Readiness = InsufficientData`; nedostatak podataka nije `0%`.

| Effective evidence weight | Confidence |
|---:|---|
| `< 1.0` | VeryLow |
| `1.0 .. < 2.0` | Low |
| `2.0 .. < 4.0` | Medium |
| `>= 4.0` | High |

`High` dodatno zahtijeva najmanje tri različita Evidence chaina. Formalna readiness
klasifikacija zahtijeva effective weight najmanje `2.0` i najmanje dva aktivna chaina;
inače je `InsufficientData`.

| Mastery | Readiness |
|---:|---|
| `< 0.60` | NeedsWork |
| `0.60 .. < 0.75` | Developing |
| `0.75 .. < 0.90` | Ready |
| `>= 0.90` | Strong |

`Ready` i `Strong` dodatno zahtijevaju najmanje `Medium` confidence.

## Hijerarhijska agregacija

Direct Evidence ostaje dopušten samo leaf komponentama. Parent agregira samo neposrednu djecu,
svako dijete jednakom težinom. KnowledgeArea na isti način agregira samo svoje root komponente;
ne preskače tree i ne zbraja leafove drugi put.

Readiness je dopušten samo kada najmanje 70% neposredne djece ima dovoljno podataka. Coverage
`70%..<85%` ograničava confidence na najviše `Medium`; coverage `>=85%` može dati `High`.
Mastery score može postojati i kada coverage nije dovoljan, ali readiness tada ostaje
`InsufficientData`.

CurriculumOutcome se računa iz mapiranih KnowledgeComponents. Direct leaf mapping ulazi izravno;
parent mapping širi se na njegove leaf potomke. Svaki leaf ulazi jednom, a isti 70% coverage
guard vrijedi i za Outcome.

## Derived persistence

Autoritativni izvor ostaje append-only Evidence povijest. Sljedeće tablice su rebuildable
projekcije s composite ključem Student + procijenjeni element:

- `MasteryEstimates`
- `KnowledgeAreaReadinessEstimates`
- `CurriculumOutcomeReadinessEstimates`

Svaka projekcija sprema nullable `Score`, `Confidence`, `Readiness`, distinct `EvidenceCount`,
`EffectiveEvidenceWeight`, UTC `CalculatedAtUtc` i obavezni
`AlgorithmVersion = readiness-v1`. CHECK constrainti štite raspone, canonical codeove,
nenegativne brojače/weight i konzistentni NoData oblik. Projekcije nisu business authority i
mogu se potpuno ponovno izgraditi.

## Recalculation i operativno izvršavanje

Observation, Correction i Invalidation u istoj transakciji recalculiraju pogođeni Student path:

```text
target leaf → ancestors → KnowledgeArea → mapped CurriculumOutcomes
```

Correction recalculira uniju starih i novih targeta; Invalidation prethodne targete. Ne radi se
full-database scan nakon pojedinog eventa.

Zbog recency decaya `Plus5.Api` jednom dnevno, približno u 02:00 `Europe/Zagreb`, u bounded
batchovima ponovno računa Studente koji imaju Evidence. Multi-instance sigurnost koristi
SQL-backed expiring lease; recalculation po Studentu koristi kratku serializable transakciju.
Worker bilježi početak, rezultat, broj recalculiranih Studenata, trajanje, lease loss i failure.

## Eksplicitno odbijeno

- automatsko mapiranje readinessa u školsku ocjenu
- AI inference ili neprozirni score
- Teacher override i ručni `postavi mastery`
- preporuke i intervention engine
- UI iz Phase 5.6+
- novi Task/Attempt model ili production emitter
- weightovi po Teacheru

## Acceptance

- domain testovi dokazuju katalog weightova, half-life/floor, confidence, thresholds, coverage i
  distinct-chain ponašanje
- stvarni SQL Server dokazuje upgrade migraciju, PerformanceScore shape/range/append-only,
  projection constraints, correction/invalidation resolution, hierarchy/Area/Outcome agregaciju,
  decay refresh i SQL lease
- migracija i idempotentni script prolaze, a EF model nema pending promjene
- puni backend/frontend regression, dependency auditi i Docker runtime gate prolaze
- nema javnog API-ja, UI-ja, production emitera, AI-ja ni school-grade mapiranja
