# Phase 2.1 — Program, grade/level and curriculum foundation

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-25`

## Cilj faze

Uvesti najmanji stabilni domain i persistence temelj za Program, školski razred, razinu znanja i kurikulum koji mogu sigurno koristiti kasniji Student, Group, Material i Knowledge Model scope.

## Implementirano

- `Program` kao Teacher-owned pedagoška ponuda s trimanim nazivom i invariant normalizacijom
- `SchoolGrade` kao odvojeni globalni code/name/sort referentni korijen
- `ProficiencyLevel` kao odvojeni framework/code/name/sort referentni korijen
- `Curriculum` kao globalni code/name/version korijen
- domenske provjere za obavezne GUID-eve, bounded tekst, UTC Program creation timestamp i nenegativni sort order
- EF konfiguracije, DbSetovi, restriktivni Teacher ownership FK, unique indeksi i CHECK constrainti
- `CORE_TEACHING_FOUNDATION.md` i ADR-0010
- izolirani ephemeral Data Protection provider u API test hostu kako testovi ne bi ovisili o korisničkom DPAPI key direktoriju

## Namjerno nije implementirano

- Program CRUD endpointi, frontend ekran ili management workflow
- Program rename/status/archive/delete lifecycle
- SchoolGrade, CEFR/other proficiency ili Curriculum seed/import podaci
- Student–Program, Group–Program ili bilo koja grade/level veza
- CurriculumOutcome, podishodi ili curriculum hijerarhija
- KnowledgeArea, KnowledgeComponent, KnowledgeModel, evidence ili readiness
- Material metadata i LearningGoal veze

## Promijenjene / dodane datoteke

| Grupa | Datoteke | Razlog |
|---|---|---|
| domain | `backend/src/Plus5.Domain/Teaching/*.cs` | četiri canonical entiteta i njihove invariante |
| persistence | `TeachingFoundationPersistenceConfigurations.cs`, `Plus5DbContext.cs` | EF schema contract |
| migration | `20260824220040_AddCoreTeachingFoundation*`, model snapshot | reproducibilna schema evolucija |
| tests | `CoreTeachingFoundationTests.cs`, `AuthenticationApiTests.cs` | domain/model contract i test-host DP izolacija |
| docs | `CORE_TEACHING_FOUNDATION.md`, README, ROADMAP, PERSISTENCE, DECISION_LOG, OPEN_QUESTIONS i ovaj summary | source-of-truth, ADR i phase handoff |

## Domain / database promjene

Novi entiteti:

- `Program(Id, TeacherAccountId, Name, NormalizedName, CreatedAtUtc)`
- `SchoolGrade(Id, Code, Name, SortOrder)`
- `ProficiencyLevel(Id, FrameworkCode, Code, Name, SortOrder)`
- `Curriculum(Id, Code, Name, Version)`

Migracija `20260824220040_AddCoreTeachingFoundation` dodaje tablice `Programs`, `SchoolGrades`, `ProficiencyLevels` i `Curricula`.

Integritet:

- `FK_Programs_UserAccounts_TeacherAccountId` koristi `Restrict`
- Program naziv je jedinstven po Teacheru preko `TeacherAccountId + NormalizedName`
- SchoolGrade code, ProficiencyLevel framework/code i Curriculum code/version imaju unique indekse
- SchoolGrade i ProficiencyLevel `SortOrder` imaju `>= 0` CHECK constraint
- nema seed/backfill podataka ni izmjene postojećih redaka

## API promjene

Nema novih endpointa, request/response contracta ni status kodova. Phase 2.1 ne otvara CRUD trust boundary.

## Frontend promjene

Nema routea, ekrana, data fetchinga ni UI stanja. Postojeći placeholderi ostaju nepromijenjeni.

## Security / authorization

- Program ownership čuva obavezni FK na Teacher `UserAccount`.
- Budući API mora owner ID uzeti iz autentificirane sesije, nikada vjerovati client `TeacherAccountId` vrijednosti.
- Zajednički referentni katalozi nemaju write endpoint u ovoj fazi.
- Brisanje UserAccounta ne može cascade obrisati Program.
- Nema novih PII podataka, secreta ni javnog attack surfacea.

## Ovisnosti

Nema novih NuGet ili npm ovisnosti.

## Testovi i provjere

| Provjera | Rezultat |
|---|---|
| locked NuGet restore | PASS |
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/domain/persistence testovi | PASS — 88/88 |
| architecture testovi | PASS — 4/4 |
| `dotnet format --verify-no-changes` | PASS |
| EF pending-model provjera | PASS — nema pending promjena |
| idempotent migration SQL generation | PASS |
| NuGet vulnerability audit | PASS — bez poznatih ranjivosti |
| `npm ci` i audit | PASS — 0 ranjivosti |
| frontend lint/typecheck/build | PASS |
| frontend testovi | PASS — 3 files, 13/13 |
| clean Docker build/runtime | PASS |
| health endpointi | PASS — live 200, ready 200 |
| clean SQL migration | PASS — 4 ukupne migracije, 4 nove tablice, 0 seed redaka |
| stvarni SQL constraint behavior | PASS — duplicate Program, negativni sort i orphan Teacher FK odbijeni |
| ponovljena migracija | PASS — no-op; postojeći Program redak očuvan |
| non-root runtime | PASS — API UID 1654, frontend `nginx` |
| cleanup | PASS — testni containeri, mreža i volume uklonjeni; imageovi ostavljeni |

## Self-review

- [x] scope je ograničen na Phase 2.1 foundation
- [x] Program, SchoolGrade i ProficiencyLevel nisu spojeni u `GradeLevel`
- [x] Program nema preuranjene grade/level/curriculum veze
- [x] nema CurriculumOutcome/Knowledge/Student/Group/Material modela
- [x] model je u 3NF s bounded tipovima i eksplicitnim constraintima
- [x] Teacher ownership ima DB FK i nema cascade deletea
- [x] nema hardkodiranog kataloga, seed podataka ili nedokumentiranog lifecyclea
- [x] migracija, testovi, auditi i Docker runtime stvarno su provjereni
- [x] dokumentacija, ADR i otvoreni kasniji gateovi su usklađeni

## Arhitekturne odluke

- ADR-0010 — odvojeni Teacher Program i globalni grade/level/curriculum referentni korijeni (`Accepted`).
- ADR-0003 — SQL Server + EF Core migrations ostaje nepromijenjen.
- ADR-0009 — Teacher-only ownership boundary ostaje nepromijenjen.

## Poznati rizici / tehnički dug

- Prazni referentni katalozi su namjerni; aplikacijski sadržaj ne treba seedati prije odobrenog sourcea/importa.
- Program lifecycle i management permissions nisu dovoljno dokumentirani za CRUD/UI i ostaju gate.
- Curriculum top-level root je stabilan, ali hierarchy/outcomes/version validity pripadaju Phase 5.1.
- `rowversion` nije uveden bez stvarnog concurrent write use casea; treba ga ponovno procijeniti uz prvi management endpoint.

## Otvorena pitanja

- Koji je odobreni source i sadržaj SchoolGrade, ProficiencyLevel i Curriculum kataloga/importa?
- Koji su Program rename/status/archive/delete lifecycle i management permissions?
- Koja je točna CurriculumOutcome hijerarhija i vremenska valjanost za Phase 5.1?

Ta pitanja ne blokiraju prazni Phase 2.1 foundation, ali blokiraju pripadajući data provisioning ili feature workflow.

## Točna početna točka za sljedeću fazu

Otvoriti **Phase 2.2 Student aggregate / profile foundation**. Student mora biti Teacher-owned osoba bez `UserAccounta`; treba koristiti zasebni obavezni SchoolGrade, dok su Program i organizacija nastave prema sourceu opcionalni. Prije migracije zaključati Student status, osnovne/optional kontakt podatke, Guardian cardinality, DeliveryMode–Group invariant i archive/delete granicu, bez readiness/Knowledge Model polja.
