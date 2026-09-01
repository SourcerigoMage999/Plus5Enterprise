# Phase 3.3 — Screen 2.2 Student digital dossier

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja`

## Datum

`2026-09-01`

## Isporučeno

- zaključan `STUDENT_DOSSIER.md` contract
- Teacher-authorized `GET /api/v1/students/{studentId}`
- owner-scoped administrativni profil, SchoolGrade, Program, DeliveryMode, aktivna Group i primarni Guardian
- stvarni sljedeći `Scheduled` i zadnji relevantni `Held` Session
- privacy-preserving `404` za missing, archived i cross-owner Student
- canonical responsive dossier s loading/error/not-found stanjima
- aktivirane dossier navigacije iz popisa i nakon create flowa
- neutralna stanja za buduće Knowledge/Evidence, readiness, materials, activity, communication i notes domene

## Namjerno nije implementirano

Edit/archive, Group detail, avatar/storage, poruke, schedule create, lesson plan, materijali, aktivnosti, readiness/Knowledge/Evidence izračuni i nastavničke bilješke.

## Promijenjene i dodane datoteke

- Application/Infrastructure/API dossier contract, EF query, endpoint, DI i route registracija
- backend query i API integration testovi
- frontend dossier API tipovi, ekran, stilovi, rute i list/create navigacija
- frontend dossier/create testovi
- `STUDENT_DOSSIER.md`, ROADMAP, Phase 3.2 boundary bilješke, ovaj summary i dva visual acceptance PNG-a

## Migracije i shema

Nema promjene sheme i nema nove migracije. Upit koristi postojeće Student, Guardian, Program, GroupMembership, Group, SchoolGrade i Session tablice.

## API contract

Dodano je read-only `GET /api/v1/students/{studentId}`. Uspjeh je `200`; anonimni pristup `401`; tuđi, arhivirani ili nepostojeći Student `404` bez ownership disclosurea.

## Arhitekturne odluke

- Read model ostaje Application contract s EF implementacijom u Infrastructure i eksplicitnim API DTO mapiranjem.
- Aktivna grupa pripada trenutačnoj membership projekciji; povijesni Group Session vrijedi samo ako njegov interval pada unutar membership intervala.
- Budući business moduli nisu simulirani. UI zadržava canonical strukturu kroz neutralna stanja i disabled akcije.
- Nije uvedena nova biblioteka, framework, servis ni ADR.

## Provjere

| Provjera | Rezultat |
|---|---|
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/domain/persistence testovi | PASS — 122/122 |
| architecture testovi | PASS — 4/4 |
| frontend testovi | PASS — 6 files, 21/21 |
| frontend lint/typecheck/build | PASS |
| NuGet locked restore/audit | PASS — nema prijavljenih poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| owner/archived/cross-owner dossier gate | PASS |
| authenticated create → dossier API journey | PASS |
| Docker Compose build/start/health chain | PASS — database/init/migrations/API/frontend |
| stvarna SQL Server 2025 dossier translacija i izvršavanje | PASS — puni read model, rollback transakcija |
| container runtime | PASS — live/ready/frontend HTTP 200; API UID 1654, frontend `nginx` |
| canonical PNG visual comparison | PASS |
| desktop screenshot comparison | PASS — 1536×1024, bez horizontalnog overflowa |
| mobile adaptation review | PASS — 390×844, bez horizontalnog overflowa |

## Rizici i otvorena pitanja

Knowledge/Evidence, storage/avatar, komunikacija, notes permissions i detaljni activity model ostaju postojeći projektni gateovi. Dosje ih ne preduhitruje. SQL Server translacija dossier upita potvrđena je 2026-09-02 na stvarnom SQL Serveru 2025; testni podaci vraćeni su rollbackom, a testni containeri i mreža uklonjeni su uz očuvan named volume i lokalne imageove.

## Sljedeća faza

Phase 3.4 — Screen 2.6 Edit student. Početna točka je zaključati edit concurrency/validation/archive granicu i canonical `2.6` vizual prije bilo kakvog write API-ja.
