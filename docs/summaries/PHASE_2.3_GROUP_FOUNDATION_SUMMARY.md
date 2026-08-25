# Phase 2.3 — Group foundation

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-25`

## Cilj faze

Uvesti najmanji trajni Group i GroupMembership domain/persistence temelj potreban za buduće ekrane 2.7, 2.8 i 2.9, uz strogi Teacher ownership i bez rasporeda, Sessiona, Knowledge Modela ili feature API/UI scopea.

## Implementirano

- `Group` kao Teacher-owned agregat s obaveznim same-Teacher Programom i SchoolGradeom
- `GroupStatus`: `Active`, `OnHold`, `Inactive`
- pozitivan kapacitet i zabrana njegova spuštanja ispod aktivnog broja članova
- arhiviranje samo grupe bez aktivnih članova, uz prijelaz u `Inactive`
- SQL Server `rowversion` i obavezno dodirivanje Group agregata za buduće konkurentne membership writeove
- vremenski `GroupMembership` s `JoinedAtUtc` i opcionalnim `LeftAtUtc`
- najviše jedna aktivna grupa po Studentu uz očuvanu povijest ponovnog ulaska
- Student domain prijelazi: ulazak atomarno preuzima Program grupe i `DeliveryMode.Group`; izlazak bez transfera čuva Program i prelazi u `Individual`
- EF konfiguracije, DbSetovi, indeksi, restriktivni composite ownership FK-ovi i CHECK constrainti
- migracija `20260825112801_AddGroupFoundation`
- `GROUP_FOUNDATION.md`, ADR-0012 i eksplicitno odgođene Group odluke

## Namjerno nije implementirano

- Group CRUD/application use caseovi, endpointi, routeovi ili UI
- raspored, ponavljanje, lokacija, trajanje ili konkretni Session
- materijali, ciljevi, bilješke, komunikacija ili obavijesti
- procijenjene razine, progress, mastery, readiness, Knowledge Model ili evidence
- automatska promjena Programa grupe s aktivnim članovima
- minimalan broj članova, draft stanje ili hard-delete pravila
- seed/backfill Group ili GroupMembership podataka

## Domain i database contract

- `Groups(Id, TeacherAccountId, ProgramId, SchoolGradeId, Name, NormalizedName, Description?, Capacity, Status, CreatedAtUtc, UpdatedAtUtc, ArchivedAtUtc?, RowVersion)`
- `GroupMemberships(Id, TeacherAccountId, GroupId, StudentId, JoinedAtUtc, LeftAtUtc?)`
- Group Program mora pripadati istom Teacheru; SchoolGrade mismatch sa Studentom ostaje upozorenje i konačna odluka Teachera, ne DB zabrana
- composite Group i Student membership FK-ovi uključuju Teacher ID i odbijaju cross-owner zapis
- filtered unique indeks nad `StudentId` gdje je `LeftAtUtc IS NULL` dopušta najviše jednu aktivnu grupu
- interval mora završiti u ili nakon vremena ulaska; arhivirana grupa mora biti `Inactive`; kapacitet je strogo pozitivan
- broj aktivnih članova nije denormaliziran: budući use case broji članove i izmjenjuje Group u istoj transakciji, čime `rowversion` serializira konkurentne pokušaje

## API i frontend promjene

Nema novih endpointa, request/response contracta, routeova ni ekrana. Phase 2.3 ne otvara Group trust boundary.

## Security / authorization

- Group ownership čuva obavezni FK na Teacher `UserAccount`.
- Composite Program FK fizički odbija Program drugog Teachera.
- Mirrored Teacher ID u članstvu služi samo composite ownership FK-ovima; canonical vlasništvo ostaje na Group i Student agregatima.
- Budući API mora owner ID izvesti iz autentificirane sesije i atomarno provesti membership, Student organization i Group capacity prijelaze.
- Nisu dodani secret, credential, PII logovi ni javni contracti.

## Ovisnosti

Nema novih NuGet ili npm ovisnosti.

## Testovi i provjere

| Provjera | Rezultat |
|---|---|
| locked NuGet restore | PASS |
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/domain/persistence testovi | PASS — 103/103 |
| architecture testovi | PASS — 4/4 |
| novi Group testovi | PASS — 8/8 |
| `dotnet format --verify-no-changes` | PASS |
| EF pending-model provjera | PASS — nema pending promjena |
| idempotent migration SQL generation | PASS |
| NuGet vulnerability audit | PASS — bez poznatih ranjivosti |
| `npm ci` i audit | PASS — 0 ranjivosti |
| frontend lint | PASS — 0 warninga i 0 grešaka |
| frontend testovi | PASS — 3 files, 13/13 |
| frontend typecheck/build | PASS |
| upgrade SQL migration | PASS — 5→6 migracija i postojeći Studenti očuvani |
| clean SQL migration | PASS — 6 migracija, Group tablice postoje, 0 seed redaka |
| ponovljena migracija | PASS — no-op |
| stvarni SQL constraint behavior | PASS — osam negativnih scenarija odbijeno; rowversion promjena potvrđena |
| Docker build/runtime | PASS |
| health endpointi | PASS — API live 200, ready 200; frontend health 200 |
| non-root runtime | PASS — API UID 1654, frontend `nginx` UID 101 |
| cleanup | PASS — Phase 2.3 testni containeri, mreža i volumei uklonjeni; imageovi ostavljeni |

## Self-review

- [x] scope je ograničen na Phase 2.3 foundation
- [x] Group ostaje odvojen od Programa, načina izvođenja i termina
- [x] Teacher ownership ne može se prijeći kroz Program ili članstvo
- [x] Student može imati najviše jednu aktivnu grupu, ali puna povijest ostaje sačuvana
- [x] capacity concurrency ima eksplicitan rowversion/transakcijski contract
- [x] Group SchoolGrade ne nameće automatsku zabranu Student grade mismatcha
- [x] arhiviranje ne ostavlja aktivne članove niti fizički briše povijest
- [x] nema schedule/Session, materials, goals, notes ili knowledge/readiness polja
- [x] model je normaliziran s bounded tipovima i eksplicitnim constraintima
- [x] migracija, testovi, auditi i non-root Docker runtime stvarno su provjereni
- [x] dokumentacija, ADR i kasniji gateovi su usklađeni

## Arhitekturne odluke

- ADR-0012 — vremenski GroupMembership, one-active-group i optimistic capacity concurrency (`Accepted`).
- ADR-0011 — Teacher-owned Student profil i organizacijski prijelazi ostaju nepromijenjeni.
- ADR-0010 — Teacher-owned Program i odvojeni globalni SchoolGrade ostaju nepromijenjeni.

## Poznati rizici / tehnički dug

- Cross-table capacity nije moguće izraziti SQL CHECK constraintom; budući application write mora obavezno provesti documented transaction/rowversion obrazac.
- Promjena Programa grupe s aktivnim članovima ostaje product gate jer može promijeniti Program svakog Studenta.
- Minimalan broj članova, draft ponašanje i eventualno fizičko brisanje nisu definirani.
- Windows Application Control blokira dio lokalnih testnih DLL/native modula; mjerodavni puni testovi izvršeni su u digest-pinnanim .NET 10 i Node 24 Linux containerima.

## Otvorena pitanja

- Smije li se Program grupe promijeniti dok postoje aktivni članovi i, ako smije, kako atomarno migrirati Student Program podatke?
- Postoji li minimalan broj članova, draft stanje ili ikada dopušten hard delete grupe?

## Točna početna točka za sljedeću fazu

Otvoriti **Phase 2.4 Schedule/session foundation**. Dostupni su Teacher-owned Student, Program i Group, temporalno članstvo te siguran kapacitet. Prije DB locka formalizirati pravila ponavljajućih termina i promjene serije iz source specifikacija 3.3/3.4; ne pretpostavljati raspored iz Group modela.
