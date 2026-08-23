# Phase 0.3 — Technology architecture decision

## Status

`DONE`

Commit/push gate: `AWAITING OWNER REVIEW`

## Datum

`2026-08-23`

## Cilj faze

Zaključati tehnološki i arhitekturni baseline koji omogućuje fazni razvoj PLUS 5 bez preuranjene infrastrukture te potvrditi da su svi obavezni engineering standardi međusobno usklađeni.

## Implementirano

- zaključan React + TypeScript + Vite SPA frontend sa strict TypeScript provjerama
- zaključan C# + ASP.NET Core / .NET 10 backend
- zaključan SQL Server persistence kroz EF Core i verzionirane migracije
- zaključan HTTPS REST/JSON API s eksplicitnim contractima i `/api/v1` versioning baselineom
- odabran modularni monolit s jasnim Domain, Application, Infrastructure i API granicama
- Docker zaključan kao standardni deployment artifact; VPS topology ostavljen odgovarajućoj release fazi
- definirani configuration/secrets, security, testing, database, backend, frontend i deployment standardi
- prihvaćeni ADR-0001–ADR-0005
- potvrđena međusobna usklađenost baselinea, standarda, ROADMAP-a i decision loga
- lokalno potvrđena dostupnost zaključanog toolchaina za sljedeću fazu
- ROADMAP 0.3 označen je `DONE`

## Namjerno nije implementirano

- repository/application bootstrap i feature business logika
- detaljni authentication UX, vrste računa, role i permissions model
- file upload/storage policy i konkretni object-storage provider
- background processing infrastruktura
- AI provider i privacy contract
- konkretni production VPS sizing, SLA, RPO/RTO i concurrency obećanje

## Promijenjene / dodane datoteke

Promjene napravljene tijekom pripreme baselinea i formalnog completiona faze:

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `docs/ARCHITECTURE_BASELINE.md` | existing baseline | Zaključani tehnološki i arhitekturni baseline |
| `docs/BACKEND_ENGINEERING_STANDARD.md` | existing standard | Obavezna backend pravila |
| `docs/FRONTEND_ENGINEERING_STANDARD.md` | existing standard | Obavezna frontend pravila |
| `docs/DATABASE_DESIGN_STANDARD.md` | existing standard | Obavezna persistence pravila |
| `docs/SECURITY_ENGINEERING_STANDARD.md` | existing standard | Security-by-design baseline |
| `docs/TESTING_QUALITY_STANDARD.md` | existing standard | Portfolio testova i quality gateovi |
| `docs/DOCKER_DEPLOYMENT_STANDARD.md` | existing standard | Container i deployment baseline |
| `docs/DECISION_LOG.md` | existing decisions | ADR-0001–ADR-0005 |
| `docs/ROADMAP.md` | changed | Status faze 0.3 postavljen na DONE |
| `docs/summaries/PHASE_0.3_TECHNOLOGY_ARCHITECTURE_DECISION_SUMMARY.md` | changed | Summary usklađen s obaveznim predloškom i rezultatima validacije |

## Domain / database promjene

- Novi entiteti/value objects: nema.
- Promijenjena pravila: nema business pravila; zaključani su engineering constraints.
- Migracije: nema.
- Backfill/data migration: nema.

## API promjene

- Nema implementiranih endpointa ni contracta.
- Zaključan je REST/JSON, HTTPS, standardizirani error contract i versioning baseline za budući javni API.

## Frontend promjene

- Nema routeova, ekrana, komponenti ni state managementa.
- Zaključani su React, TypeScript strict, Vite i frontend engineering granice.

## Security / authorization

- Zaključani su deny-by-default, least privilege, server-side authorization, input validation, secrets izvan source controla, restriktivan CORS i sigurno logiranje.
- Detaljni auth/business contract namjerno ostaje gate faze 1.6.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| cross-document architecture consistency review | PASS |
| provjera stacka prema `ARCHITECTURE_BASELINE.md` i ADR-0001–ADR-0005 | PASS |
| `.NET SDK` | PASS — `10.0.102` |
| `Node.js` | PASS — `v24.18.0` |
| `npm` | PASS — `11.16.0` |
| `Docker` | PASS — `29.4.0` |
| build / automated application tests | N/A — repository bootstrap je scope faze 0.4 |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build nije primjenjiv prije faze 0.4
- [x] relevantne dokumentacijske i toolchain provjere prolaze
- [x] migracije nisu primjenjive
- [x] auth/validation granice su evidentirane bez izmišljanja business contracta
- [x] dokumentacija je ažurirana

## Arhitekturne odluke

- ADR-0001 — React + TypeScript frontend
- ADR-0002 — ASP.NET Core / .NET backend
- ADR-0003 — SQL Server + EF Core migrations
- ADR-0004 — Modularni monolit prije mikroservisa
- ADR-0005 — Docker kao deployment artifact; VPS kasnije

Nije uvedena nova odluka izvan već prihvaćenih ADR-ova.

## Poznati rizici / tehnički dug

- Node/npm i Docker verzije potvrđene su kao lokalni toolchain, ali njihov reproducibilni project pinning pripada bootstrap fazi 0.4.
- Konkretan concurrency/SLA nije dokazan; 10.000+ ukupnih korisnika ostaje arhitekturni cilj, ne obećanje istovremenih korisnika.
- Auth, file storage, AI i production topology ostaju gated kako je navedeno u `OPEN_QUESTIONS.md` i ROADMAP-u.

## Otvorena pitanja

- Nema novih blokera za repository bootstrap.
- Postojeća business pitanja iz `OPEN_QUESTIONS.md` ostaju otvorena za pripadajuće buduće faze.

## Točna početna točka za sljedeću fazu

Otvoriti Phase 0.4 Repository/bootstrap. Izraditi minimalan clean-checkout buildable/testable repo prema zaključanom modularnom monolitu: .NET 10 backend projekti s architecture testovima, React + TypeScript + Vite frontend sa strict provjerama, osnovni Docker/dev setup i root README. Ne uvoditi business entitete, bazne tablice, auth flow ni feature ekrane.
