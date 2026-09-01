# Phase 3.2 — Screen 2.3 Create student

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja`

## Datum

`2026-09-01`

## Isporučeno

- zaključan `STUDENT_CREATE.md` contract
- `GET /api/v1/students/create-options` i CSRF-zaštićeni `POST /api/v1/students`
- claims-derived Teacher ownership i neotkrivanje cross-owner referenci
- Student bez Programa, individualni Student ili atomska Group dodjela
- opcionalni primarni Guardian te kontrolirani capacity/concurrency konflikti
- canonical `/students/new` forma s live sažetkom i svim ključnim stanjima
- aktivirana `+ Dodaj učenika` akcija i minimalna Phase 3.3 success boundary
- responsive shell ispravak koji uklanja mobilni horizontalni overflow

## Namjerno nije implementirano

Student dossier/edit/archive, Program/SchoolGrade/Group CRUD ili seed, više Guardiana, avatar/storage, komunikacija i Knowledge/Evidence/readiness podaci.

## Security i podaci

Teacher ID nije dio requesta. Program i Group upiti su owner-scoped, a tuđi ID vraća `not_found`. Write zahtijeva cookie autorizaciju i CSRF. Group membership i student spremaju se u jednoj transakciji uz optimistic concurrency zaštitu. Nema promjene sheme ni migracije.

## Provjere

| Provjera | Rezultat |
|---|---|
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/domain/persistence testovi | PASS — 120/120 |
| architecture testovi | PASS — 4/4 |
| frontend testovi | PASS — 5 files, 19/19 |
| frontend lint/typecheck/build | PASS |
| NuGet audit | PASS — nema poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| auth/CSRF/create endpoint journey | PASS — 401/400/200/201 |
| canonical PNG visual comparison | PASS |
| desktop screenshot comparison | PASS — 1536×1024 |
| mobile adaptation review | PASS — 390×844, bez horizontalnog overflowa |

## Poznata fazna granica

`/students/{id}` zasad prikazuje samo potvrdu spremanja. Administrativni digitalni dosje i njegov read API implementiraju se u Phase 3.3.

## Sljedeća faza

Phase 3.3 — Screen 2.2 Student digital dossier.
