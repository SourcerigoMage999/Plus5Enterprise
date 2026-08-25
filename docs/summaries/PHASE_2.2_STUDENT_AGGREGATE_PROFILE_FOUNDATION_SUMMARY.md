# Phase 2.2 — Student aggregate / profile foundation

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-25`

## Cilj faze

Uvesti najmanji trajni Student i Guardian domain/persistence temelj potreban za buduće ekrane 2.1, 2.2, 2.3 i 2.6, uz strogi Teacher ownership i bez readiness/Knowledge Model ili feature API/UI scopea.

## Implementirano

- `Student` kao Teacher-owned osoba bez `UserAccounta`
- obavezni `SchoolGrade` te opcionalni paired `Program` + eksplicitni `DeliveryMode`
- composite same-Teacher Student/Program relacija koja na razini baze sprječava cross-owner vezu
- `StudentStatus`: `Active`, `OnHold`, `Inactive`
- osnovni obavezni identitet i opcionalni profil/kontakt podaci iz source specifikacija
- `Archive` domain prijelaz koji postavlja `Inactive`, `ArchivedAtUtc` i `UpdatedAtUtc`
- nula ili više Student-owned `Guardian` kontakata, uz najviše jednog primarnog
- EF konfiguracije, DbSetovi, indeksi, restriktivni FK-ovi i CHECK constrainti
- migracija `20260825104604_AddStudentProfileFoundation`
- `STUDENT_FOUNDATION.md`, ADR-0011 i odgođene Student odluke

## Namjerno nije implementirano

- Student CRUD/application use caseovi, endpointi, routeovi ili UI
- `Group`, `GroupMembership` i feature write za `DeliveryMode.Group`
- Student/Guardian accounti, login ili auth credentiali
- fotografija/file storage
- bilješke, razgovori, poruke, notification preference ili privacy toggleovi
- ProficiencyLevel target/estimate, progress, mastery, readiness, Knowledge Model ili evidence
- hard-delete, production retention, legal erasure ili anonimizacija
- canonical Gender katalog ili poslovna logika nad slobodnim prikaznim podatkom
- seed/backfill Student, Guardian ili referentnih podataka

## Promijenjene / dodane datoteke

| Grupa | Datoteke | Razlog |
|---|---|---|
| domain | `backend/src/Plus5.Domain/Students/*.cs` | Student, Guardian i dva ograničena enum contracta |
| persistence | `StudentPersistenceConfigurations.cs`, `TeachingFoundationPersistenceConfigurations.cs`, `Plus5DbContext.cs` | 3NF schema, same-owner Program key/FK, constrainti i DbSetovi |
| migration | `20260825104604_AddStudentProfileFoundation*`, model snapshot | reproducibilna schema evolucija |
| tests | `StudentFoundationTests.cs` | domain i EF model invariante |
| docs | `STUDENT_FOUNDATION.md`, README, ROADMAP, PERSISTENCE, DECISION_LOG, OPEN_QUESTIONS i ovaj summary | source-of-truth, ADR, gates i handoff |

## Domain / database promjene

Novi modeli:

- `Student(Id, TeacherAccountId, SchoolGradeId, ProgramId?, FirstName, LastName, Nickname?, DateOfBirth?, SchoolName?, Gender?, Email?, Phone?, DeliveryMode?, Status, CreatedAtUtc, UpdatedAtUtc, ArchivedAtUtc?)`
- `Guardian(Id, StudentId, FirstName, LastName, Relationship?, Email?, Phone?, IsPrimary, CreatedAtUtc)`
- `DeliveryMode(Individual, Group)`
- `StudentStatus(Active, OnHold, Inactive)`

Migracija dodaje tablice `Students` i `Guardians` te alternate key `Programs(TeacherAccountId, Id)` potreban za composite ownership FK.

Integritet:

- Teacher, SchoolGrade, same-Teacher Program i Student/Guardian FK-ovi koriste `Restrict`
- Program i DeliveryMode moraju biti oba postavljena ili oba izostavljena
- Status i DeliveryMode ograničeni su na definirane vrijednosti
- `ArchivedAtUtc` zahtijeva `Inactive` status
- filtered unique indeks dopušta najviše jednog primarnog Guardiana po Studentu
- nema data migrationa, seeda ili izmjene postojećih redaka; dodani Program alternate key siguran je zbog postojećeg jedinstvenog PK-a

## API promjene

Nema novih endpointa, request/response contracta ni status kodova. Phase 2.2 ne otvara Student/Guardian trust boundary.

## Frontend promjene

Nema routea, ekrana, data fetchinga ni UI stanja.

## Security / authorization

- Student ownership čuva obavezni FK na Teacher `UserAccount`.
- Composite Program FK uključuje Teacher ID i fizički odbija Program drugog Teachera.
- Guardian ownership izvodi se kroz restriktivnu Student vezu.
- Student/Guardian e-mail i telefon nisu login identiteti niti su jedinstveni.
- Budući API mora owner ID izvesti iz autentificirane sesije i imati object-level authorization testove.
- PII nije dodan u logove, telemetry ni javne contracte.

## Ovisnosti

Nema novih NuGet ili npm ovisnosti.

## Testovi i provjere

| Provjera | Rezultat |
|---|---|
| locked NuGet restore | PASS |
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/domain/persistence testovi | PASS — 95/95 |
| architecture testovi | PASS — 4/4 |
| `dotnet format --verify-no-changes` | PASS |
| EF pending-model provjera | PASS — nema pending promjena |
| idempotent migration SQL generation | PASS |
| NuGet vulnerability audit | PASS — bez poznatih ranjivosti |
| `npm ci` i audit | PASS — 0 ranjivosti |
| frontend lint | PASS — 0 warninga i 0 grešaka |
| frontend testovi | PASS — 3 files, 13/13 |
| frontend typecheck/build | PASS |
| upgrade SQL migration | PASS — postojeći Program očuvan |
| clean SQL migration | PASS — 5 migracija, 2 nove tablice, 0 seed redaka |
| ponovljena migracija | PASS — no-op |
| stvarni SQL constraint behavior | PASS — pet negativnih scenarija odbijeno |
| Docker build/runtime | PASS |
| health endpointi | PASS — API live 200, ready 200; frontend health 200 |
| non-root runtime | PASS — API UID 1654, frontend `nginx` UID 101 |
| cleanup | PASS — testni containeri, mreža i volumei uklonjeni; imageovi ostavljeni |

## Self-review

- [x] scope je ograničen na Phase 2.2 foundation
- [x] Student i Guardian nisu `UserAccount`
- [x] Student je Teacher-owned i Program veza ne može prijeći Teacher granicu
- [x] SchoolGrade i DeliveryMode ostaju odvojeni pojmovi
- [x] Program i DeliveryMode optionalnost je konzistentna u domenu i bazi
- [x] Group/GroupMembership nije uveden unaprijed
- [x] nema readiness/progress/Knowledge/evidence polja
- [x] nema file-storage, messaging, notes ili privacy feature pretpostavki
- [x] arhiviranje čuva povijesni zapis; fizičko brisanje nije izmišljeno
- [x] model je u 3NF s bounded tipovima i eksplicitnim constraintima
- [x] migracija, testovi, auditi i non-root Docker runtime stvarno su provjereni
- [x] dokumentacija, ADR i kasniji gateovi su usklađeni

## Arhitekturne odluke

- ADR-0011 — Teacher-owned Student profil, child Guardian kontakti i arhiviranje (`Accepted`).
- ADR-0010 — odvojeni Teacher Program i globalni grade/level/curriculum korijeni ostaju nepromijenjeni.
- ADR-0009 — Teacher-only account i ownership boundary ostaju nepromijenjeni.

## Poznati rizici / tehnički dug

- `DeliveryMode.Group` je canonical vrijednost, ali feature write mora ostati zatvoren dok Phase 2.3 ne uvede atomarnu aktivnu `GroupMembership` invariantnu vezu.
- Gender je samo opcionalni bounded prikazni podatak; katalog, filtriranje i automatizacija nisu dopušteni bez odluke vlasnika proizvoda.
- `rowversion` nije uveden bez concurrent write use casea; ponovno ga procijeniti uz Student CRUD.
- Guardian je child jednog Studenta; eventualno dijeljenje istog stvarnog kontakta između braće/sestara zahtijevalo bi zasebni identity/merge contract.
- Windows Application Control blokirao je lokalni Rolldown native modul nakon `npm ci`; mjerodavni frontend test/build izvršen je u digest-pinnanom Node 24 Linux containeru, istom platformskom baselineu koji koristi Docker build.

## Otvorena pitanja

- Koja je production retention/legal-erasure/anonimizacija politika za Student i Guardian PII?
- Treba li Gender dobiti odobreni kontrolirani katalog ili trajno ostati slobodni opcionalni prikazni podatak?
- Koje su vremenska valjanost i dopuštene kardinalnosti `GroupMembershipa` u Phase 2.3?
- Koja file-storage politika vrijedi za fotografiju učenika?

## Točna početna točka za sljedeću fazu

Otvoriti **Phase 2.3 Group foundation**. Dostupni su Teacher-owned Program i Student, eksplicitni `DeliveryMode` te owner-safe relacije. Prije migracije zaključati Group status/lifecycle, kapacitet, Program/SchoolGrade veze, vremensku valjanost i kardinalnost `GroupMembershipa`, ponašanje pri promjeni Student DeliveryModea te invariantnu atomarnost aktivnog grupnog članstva. Ne uvoditi raspored/Session detalje iz Phase 2.4.
