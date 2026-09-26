# Phase 5.5 — Mastery / Readiness v1

## Status

`FINAL LOCKED`

## Datum

`2026-09-25`

## Cilj faze

Uvesti zaključani deterministički `readiness-v1` izračun iz append-only Evidence povijesti:
pedagoški rezultat, weight/decay, confidence/readiness, hijerarhijsku agregaciju, rebuildable
projekcije i daily refresh, bez UI-ja, AI-ja, school-grade mapiranja ili production emitera.

## Implementirano

- obavezni `PerformanceScore decimal 0..1` za Observation/Correction; Invalidation ga nema
- append-only domain/SQL zaštita PerformanceScorea i potpuni Correction replacement
- zaključani Difficulty, EvidenceType, AssistanceLevel i EvidenceContext v1 weight katalog
- 90-dnevni exponential half-life iz `OccurredAtUtc` s floorom `0.10`
- samo najnoviji valjani Observation/Correction po chainu; invalidirani chain ne doprinosi
- weighted-average leaf Mastery, effective evidence weight i distinct-chain count
- canonical confidence: `NoData`, `VeryLow`, `Low`, `Medium`, `High`
- canonical readiness: `InsufficientData`, `NeedsWork`, `Developing`, `Ready`, `Strong`
- minimum weight `2.0` + dva chaina za readiness; najmanje tri chaina za High confidence
- pragovi `0.60 / 0.75 / 0.90`; Ready/Strong zahtijevaju najmanje Medium confidence
- parent agregacija samo neposredne djece jednakom težinom
- KnowledgeArea agregacija samo root komponenti, bez preskakanja hijerarhije
- 70% coverage gate i Medium cap za coverage `70%..<85%`
- CurriculumOutcome parent-to-leaf expansion uz deduplikaciju leafova i 70% coverage
- rebuildable `MasteryEstimates`, `KnowledgeAreaReadinessEstimates` i
  `CurriculumOutcomeReadinessEstimates` projekcije
- obavezni `AlgorithmVersion = readiness-v1`
- affected-path recalculation u istoj transakciji nakon Observation/Correction/Invalidationa
- daily refresh približno u 02:00 `Europe/Zagreb`, bounded batch `100`, kratka serializable
  transakcija po Studentu i SQL-backed 15-minutni lease
- strukturirani worker logovi za start, completion, cancellation, failure i lease loss
- formalni `MASTERY_READINESS.md` contract i ADR-0022

## Persistence i migration ponašanje

`AddMasteryReadinessV1` dodaje nullable `PerformanceScore decimal(9,8)` jer Invalidation mora
imati `NULL`, ali lifecycle CHECK zahtijeva score za Observation/Correction. Tri projection
tablice imaju composite Student/target PK, restriktivne FK-ove, normalizirani score,
confidence/readiness codeove, evidence count/effective weight, UTC timestamp i algorithm
version. CHECK constrainti štite range, canonical codeove, NoData shape i UTC.

Migracija ne seeda podatke i ne backfilla business vrijednosti. Produkcijska Evidence tablica
bila je prazna prije Phase 5.5, a projekcije su derived cache koji se može ponovno izgraditi.

## Testovi i evidence

| Gate | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Readiness/Evidence host suite bez SQL opt-ina | PASS — 19 passed, 11 očekivano skipped |
| Fokusirani stvarni SQL Evidence/Readiness suite | PASS — 11/11, 0 skipped |
| Puni Backend/API/domain/persistence suite sa stvarnim SQL-om | PASS — 208/208, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| EF pending-model check | PASS — nema promjena nakon migracije |
| Idempotentni migration script | PASS — sadrži Phase 5.5 migration/schema guardove |
| Frontend regression | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker API/migrations/frontend rebuild | PASS |
| Runtime migration | PASS — `20260924214206_AddMasteryReadinessV1` primijenjena |
| Runtime schema | PASS — PerformanceScore 1/1, projection tablice 3/3, Phase 5.5 CHECK 19/19 |
| HTTP runtime smoke | PASS — API live/ready i frontend vraćaju 200 |
| Non-root runtime | PASS — API i migrations UID 1654; frontend `nginx` |

SQL testovi koriste zasebne disposable baze na stvarnom SQL Serveru. Upgrade scenarij kreće od
Phase 5.4 migracije, primjenjuje Phase 5.5 i potvrđuje prazan rebuildable projection state.
Docker runtime zatim primjenjuje istu migraciju na lokalnu persistent bazu prije pokretanja nove
API slike.

## Pokriveni ključni scenariji

- svi zaključani weight multiplikatori, 90-dnevni half-life i recency floor
- NoData nije `0%`; score, confidence i readiness ostaju odvojeni
- threshold boundaryji `0.60`, `0.75` i `0.90`
- jedan chain ne može umjetno postati više chainova kada targetira više leafova
- latest Correction zamjenjuje Observation u izračunu
- terminalna Invalidation uklanja cijeli chain iz izračuna
- correction target change recalculira stari i novi component path
- parent, Area i parent-mapped Outcome agregacije bez leaf dupliciranja
- daily decay smanjuje effective weight i confidence/readiness bez promjene Evidence povijesti
- SQL lease sprječava istovremeni drugi refresh run
- SQL odbija PerformanceScore izvan raspona, nedostajući Observation score, nevaljane projection
  score/confidence vrijednosti i naknadni PerformanceScore UPDATE
- EF migration upgrade i ponovljeni apply ostaju idempotentni

## Namjerno nije implementirano

- Phase 5.6+ UI ekrani ili frontend readiness state
- AI/ML inference
- Teacher override, ručni mastery postotak ili personalizirani weightovi
- automatsko mapiranje readinessa u školsku ocjenu
- recommendations ili intervention engine
- novi Task/Attempt model
- Lesson/Homework/Board ili drugi production emitter
- javni generic Evidence API

## Self-review

- [x] Evidence history ostaje jedini authority; projekcije su rebuildable
- [x] Correction/Invalidation semantics ne zbrajaju prethodni event
- [x] distinct-chain hard rule vrijedi i na agregiranim razinama
- [x] parent/Area računaju samo neposrednu razinu i ne rade hierarchy double count
- [x] Outcome parent expansion deduplicira leafove
- [x] UI ne može postati authority jer Phase 5.5 nema write/read endpoint
- [x] daily refresh je multi-instance safe, bounded i observable
- [x] nema secreta, ownership popuštanja ni novog javnog attack surfacea
- [x] nema AI-ja, gradinga, emitera ili drugih budućih modela
- [x] kod, migracija, testovi, ROADMAP i formalni contract imaju isti status i scope

## Točna početna točka za sljedeću fazu

Phase 5.5 je finalno zaključana 2026-09-26. Phase 5.6 smije čitati projection rezultate samo
kroz zasebno definiran owner-scoped query/UI contract. Budući UI uz score mora prikazati
confidence, broj dokaza i vrijeme zadnjeg izračuna; ne smije prikazati readiness kao školsku
ocjenu ili objektivnu činjenicu.
