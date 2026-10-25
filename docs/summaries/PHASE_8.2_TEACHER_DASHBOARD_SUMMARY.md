# Phase 8.2 — Teacher dashboard

## Status

**FINAL LOCKED — odobreno 2026-10-26.**

## Implementirano

- stvarni Teacher dashboard na `/`;
- canonical quick actions s aktivnim postojećim routeovima i poštenim disabled budućim
  akcijama;
- današnji raspored, next-session kartica i Student status pregled nad Phase 8.1 API-jem;
- loading, error/retry i empty stanja;
- responsive desktop/mobile layout bez document overflowa;
- minimalni canonical visual acceptance evidence;
- SQL Server-compatible bounded hydration korekcija Phase 8.1 dashboard read queryja bez
  promjene API contracta.

## Sačuvane odluke

- Teacher identitet dolazi isključivo iz autentificirane sesije;
- osobno ime ne izvodi se iz e-maila;
- no-data se ne prikazuje kao lažna nula ili postotak;
- Student status nije readiness/performance metrika;
- Lesson Plan, Board, messaging, homework, finance, notifications, activities i
  recently-used Materials nisu preuranjeno implementirani;
- nema nove migracije, tablice, write endpointa ni dependencyja.

## Validacija

- frontend targeted: PASS — Dashboard + route integration 8/8;
- frontend lint: PASS;
- frontend typecheck: PASS;
- frontend production build: PASS;
- backend targeted correction build: PASS — 0 warninga, 0 grešaka;
- backend dashboard tests: PASS — 3/3;
- stvarni SQL/API runtime: PASS — dashboard HTTP 200 u oba viewporta;
- visual acceptance: PASS — 1536×1024 i 390×844, scrollWidth == clientWidth,
  browser/page errors 0, business writeovi 0;
- full regression: N/A — targeted phase validation is sufficient;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni;
- Docker infrastructure validation: N/A — infrastructure unchanged.

Windows sandbox je pri prvom Vitest pokretanju vratio poznati EPERM; isti unaprijed određeni
targeted filter jednom je pokrenut izvan sandboxa i prošao bez širenja scopea.

## Odvojeni maintenance/security finding

Container restore, koji nije bio dio planiranog dependency-audit gatea, prema aktualnim NuGet
advisory podacima prijavljuje postojeće transitive `System.Security.Cryptography.Pkcs`
9.0.11/10.0.11 i `System.Security.Cryptography.Xml` 10.0.11 ranjivosti. Dependency graph nije
promijenjen u 8.2 i dashboard path ih ne uvodi niti koristi. Sukladno STRICT VALIDATION BUDGET-u
nije napravljen opportunistic dependency upgrade; finding treba zasebni security-maintenance
review Solution Architecta.

## Dokumentacija i evidence

- `TEACHER_DASHBOARD_SCREEN.md`;
- `TEACHER_DASHBOARD_READ_MODEL.md`;
- `FRONTEND_FOUNDATION.md`;
- `visual-acceptance/phase-8.2/README.md`;
- `ROADMAP.md`;
- ovaj summary.

## Scope namjerno nije implementiran

- Lesson Plan i pokretanje Lesson Sessiona/Boarda;
- poruke, obavijesti i activity feed;
- homework i test/ispit workflow;
- finance podaci;
- recently-used Materials bez usage relacije;
- overall readiness, predicted grade ili izvedeni performance bandovi.

## Review handoff

Phase 8.2 je Solution Architect odobrio i označena je kao FINAL LOCKED.
