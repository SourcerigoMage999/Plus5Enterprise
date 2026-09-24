# Phase 5.4 — Evidence metadata

## Status

`IMPLEMENTED — REVIEW READY`

## Datum

`2026-09-24`

## Cilj faze

Proširiti zaključani append-only EvidenceEvent model potpunim immutable metadata snapshotom za
svaki Observation i Correction, bez weightinga, readiness matematike, Task redizajna, javnog
API-ja, UI-ja ili production emitera.

## Implementirano

- `Difficulty` kao ordinalni cijeli broj `1..5`, bez matematičkog učinka
- bounded canonical `EvidenceType`: `Recognition`, `Understanding`, `Application`, `Production`
- bounded canonical `AssistanceLevel`: `Independent`, `MinorAssistance`,
  `SignificantAssistance`, `NotObserved`
- bounded canonical `EvidenceContext`: `Lesson`, `Homework`, `Assessment`,
  `IndependentPractice`
- domain `EvidenceMetadata` value object koji validira range i zaključane enum vrijednosti
- obavezni potpuni metadata snapshot za Observation i Correction factoryje
- Correction kao potpuna zamjena snapshota bez nasljeđivanja polja ili izmjene prethodnika
- Invalidation bez metadata vrijednosti
- interni emission commandi i EF service koji atomski spremaju snapshot s eventom i targetima
- bounded string persistence za canonical codeove
- aditivna `AddEvidenceMetadata` migracija bez seeda, defaulta ili backfilla
- DB CHECK zaštita lifecycle shapea, raspona i dopuštenih codeova
- postojeći append-only SQL trigger štiti i metadata kolone od in-place UPDATE-a
- formalni `EVIDENCE_METADATA.md` contract i ADR-0021

## Namjerno nije implementirano

- weighting, multiplier ili drugi algoritamski učinak Difficulty/EvidenceType/Assistance vrijednosti
- mastery, readiness, confidence, decay, thresholds ili postotci
- Task/Activity model ili promjena budućih source tablica
- javni Evidence API, frontend ekran ili lokalizacijski UI
- Lesson, Homework, Assessment, Board ili drugi production emitter
- AI inference, metadata catalog tablice ili `EvidenceMetadataVersion` kolona
- backfill postojećeg Evidencea ili runtime join na današnje source metadata vrijednosti

## Persistence i migration ponašanje

Četiri nove kolone fizički su nullable jer Invalidation mora imati `NULL`, ali
`CK_EvidenceEvents_MetadataShape` zahtijeva sva četiri polja za Kind Observation/Correction i sva
četiri `NULL` za Invalidation. Zasebni CHECK constrainti štite Difficulty `1..5` i svaki canonical
string katalog. Migracija ne izmišlja neutralne vrijednosti i ne mijenja povijesne business retke.
Production baza prije primjene imala je 0 EvidenceEvent redaka, što je potvrđeno i nakon migracije.

## Testovi i evidence

| Gate | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Phase 5.4 fokusirani domain/EF/SQL testovi | PASS — 13/13, 0 skipped |
| Puni Backend/API/domain/persistence suite sa stvarnim SQL-om | PASS — 191/191, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| EF pending-model check (Release model) | PASS — nema promjena nakon migracije |
| Idempotentni migration script | PASS |
| Frontend testovi | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker API/migrations/frontend rebuild | PASS |
| Compose migration i runtime health | PASS — init/migration exit 0; baza, API i frontend healthy |
| HTTP smoke | PASS — API live/ready i frontend vraćaju 200 |
| Non-root runtime | PASS — API/migrations UID 1654; frontend UID 101 (`nginx`) |
| Runtime migration | PASS — `20260924205126_AddEvidenceMetadata` primijenjena |
| Runtime schema | PASS — 4/4 metadata kolone i 5/5 Phase 5.4 CHECK constrainta postoje |
| Production Evidence podaci | PASS — 0 EvidenceEvent redaka; nema seeda/backfilla |

Puni SQL suite izvršen je u pinned projektnom .NET SDK containeru koji dijeli network namespace
lokalnog SQL Server containera. Time je cijeli migration chain i svih 191 test scenarija izvršen
na stvarnom SQL Serveru, bez LocalDB zamjene ili skipped testova.

## Pokriveni Phase 5.4 scenariji

- Observation sprema puni authoritative snapshot
- Correction sprema potpuno novi snapshot, a Observation ostaje nepromijenjen
- Invalidation sprema sva četiri metadata polja kao `NULL`
- domain odbija Difficulty izvan `1..5` i nepoznate enum vrijednosti
- SQL odbija Difficulty `0` i `6`
- SQL odbija nepoznati EvidenceType, AssistanceLevel i EvidenceContext
- SQL odbija parcijalni Observation/Correction snapshot
- SQL odbija metadata na Invalidationu
- SQL append-only trigger odbija naknadni metadata UPDATE
- upgrade s Phase 5.3 migracije i ponovljeni migration apply ostaju ispravni
- canonical string vrijednosti reproducibilno se čitaju kroz EF model

## Self-review

- [x] `NotObserved` nije izjednačen s `Independent`
- [x] source kind i evidence context ostaju odvojeni pojmovi
- [x] Board nije uveden kao zaseban EvidenceContext
- [x] snapshot je povijesno authoritative i nema runtime source joina
- [x] Correction je full replacement, bez field inheritancea
- [x] Invalidation ne predstavlja novu pedagošku procjenu
- [x] katalozi su stabilni bounded codeovi bez preuranjenih tablica
- [x] nema backfilla, fake defaulta ili production seeda
- [x] nema weightinga/readinessa, Taska, UI-ja, API-ja ili emitera iz budućih faza
- [x] domain, service i SQL sloj provode isti contract
- [x] build/test/format/audit/EF/Docker gateovi prolaze
- [x] dokumentacija i implementacija imaju isti status i scope

## Točna početna točka za sljedeću fazu

Phase 5.4 je implementirana i spremna za završni review/LOCK. Phase 5.5 ne smije krenuti dok
readiness algoritam iz `OPEN_QUESTIONS.md` ne dobije zasebnu eksplicitnu product/architecture
odluku. Phase 5.4 metadata vrijednosti ne impliciraju nikakav weighting.
