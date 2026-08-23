# PHASE 0.3 — Technology Architecture Decision Summary

## Status

**READY FOR IMPLEMENTATION / ARCHITECTURE LOCK PREPARED — 2026-08-23**

## Što je zaključano

- React + TypeScript + Vite frontend
- C# + ASP.NET Core / .NET 10 backend
- SQL Server + EF Core migrations
- modularni monolit kao početni arhitekturni stil
- security-first baseline
- 10.000+ korisnika kao arhitekturni cilj, uz odvojeno dokazivanje konkretnog concurrencyja load testom
- Docker kao standardni deployment artifact
- kasniji VPS deployment uz TLS, private DB, backup/restore i observability gateove

## Dodani tehnički standardi

- `DATABASE_DESIGN_STANDARD.md`
- `BACKEND_ENGINEERING_STANDARD.md`
- `FRONTEND_ENGINEERING_STANDARD.md`
- `SECURITY_ENGINEERING_STANDARD.md`
- `DOCKER_DEPLOYMENT_STANDARD.md`
- `TESTING_QUALITY_STANDARD.md`
- `ENGINEERING_CHECKLIST.md`

## Ažurirani dokumenti

- `ARCHITECTURE_BASELINE.md`
- `PROJECT_RULES.md`
- `AI_DEVELOPER_SYSTEM_PROMPT.md`
- `README.md`
- `DECISION_LOG.md`
- `OPEN_QUESTIONS.md`
- `ROADMAP.md`

## Namjerno nije zaključano

- detaljni authentication UX/role/permission business contract
- file upload policy i konkretni object storage provider
- AI provider/privacy contract
- konkretni production VPS sizing
- konkretni SLA/concurrent-user target

Ove odluke ostaju gated u ROADMAP fazama gdje postaju stvarni implementacijski zahtjev.

## Testovi

Nema koda. Dokumentacijska promjena; build/test nije primjenjiv.

## Sljedeća točka

Dovršiti ROADMAP 0.2 domain glossary prije 0.4 repository/bootstrapa, osim ako vlasnik projekta službeno promijeni redoslijed.
