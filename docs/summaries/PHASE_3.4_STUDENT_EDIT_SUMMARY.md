# Phase 3.4 — Screen 2.6 Edit student

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja`

## Datum

`2026-09-02`

## Cilj faze

Omogućiti Teacheru sigurno owner-scoped uređivanje i arhiviranje postojećeg učenika kroz canonical ekran 2.6, bez preuranjenog Knowledge, storage, notes ili privacy modela.

## Implementirano

- zaključan `STUDENT_EDIT.md` contract prema source DOCX-u i canonical PNG-u
- `GET /api/v1/students/{id}/edit`, CSRF-zaštićeni `PUT /api/v1/students/{id}` i `POST /api/v1/students/{id}/archive`
- SQL `rowversion` za Student optimistic concurrency i kontrolirani `409`
- atomski Individual/Group transferi, završavanje i stvaranje GroupMembershipa te capacity/concurrency zaštita
- uređivanje postojećih i dodavanje novih Guardian kontakata uz najviše jedan primarni kontakt
- archive umjesto hard deletea, uz završavanje aktivnog članstva i očuvanu povijest
- canonical trostupčani desktop ekran, responsive mobile stacking, potvrda arhiviranja i navigacija iz popisa/dosjea
- neutralne buduće zone za Knowledge/progress, notes i privacy/visibility

## Namjerno nije implementirano

Hard delete/legal erasure, Guardian removal, avatar/upload, Knowledge/Evidence/readiness, proficiency target, nastavničke bilješke, audit-history UI, privacy/analytics toggles, Board access i communication account linking.

## Domain / database promjene

- `Student.UpdateAdministrativeDetails` i `Guardian.Update/ClearPrimary`
- Student `RowVersion` concurrency token
- migracija `20260901224924_AddStudentEditingConcurrency`
- nema destruktivnog backfilla; SQL Server popunjava postojeće retke rowversion vrijednostima

## API i sigurnost

- svi endpointi zahtijevaju Teacher policy; write endpointi zahtijevaju antiforgery token
- Teacher ID dolazi iz autentificiranog identiteta
- nepostojeći, arhivirani i cross-owner Student vraćaju privacy-preserving `404`
- validation vraća `400`; stale/capacity/unavailable stanja stabilne `409` problem kodove

## Provjere

| Provjera | Rezultat |
|---|---|
| backend Release build/test | PASS — 126/126, 0 warninga i grešaka |
| architecture testovi | PASS — 4/4 |
| frontend testovi | PASS — 7 files, 23/23 |
| frontend lint/typecheck/build | PASS |
| NuGet locked restore/audit | PASS — nema prijavljenih poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker Compose build/start/health | PASS — database/init/migrations/API/frontend |
| stvarna SQL Server migracija | PASS — non-null `Students.RowVersion` (`timestamp/rowversion`) |
| stvarni authenticated edit save | PASS — save kroz UI/API/SQL i povratak na dosje |
| container runtime | PASS — live/ready/frontend HTTP 200; API UID 1654, frontend UID 101 `nginx` |
| canonical desktop usporedba | PASS — 1536×1024, bez horizontalnog overflowa |
| mobile adaptation | PASS — 390×844, bez horizontalnog overflowa |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build i relevantni testovi prolaze
- [x] migracija, auth, ownership, CSRF, validation i concurrency granice provjerene
- [x] canonical visual acceptance i dokumentacija ažurirani

## Arhitekturne odluke

Nema novog ADR-a. Faza primjenjuje postojeće ownership, retention, permissions i Knowledge/Evidence gateove.

## Rizici i otvorena pitanja

Guardian removal, legal erasure, avatar storage, notes permissions, privacy toggles i audit-history prikaz ostaju postojeći projektni gateovi. Archive je reverzibilan samo budućim eksplicitnim lifecycle contractom; ova faza ne uvodi restore UI.

## Sljedeća faza

Phase 3.5 — Screen 2.7 Groups. Student, Program, DeliveryMode i vremenski GroupMembership sada imaju siguran write lifecycle potreban za owner-scoped pregled grupa.
