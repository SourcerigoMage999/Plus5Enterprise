# Phase 0.4 — Repository/bootstrap

## Status

`DONE`

Commit/push gate: `AWAITING OWNER REVIEW`

## Datum

`2026-08-23`

## Cilj faze

Stvoriti minimalan, reproducibilan i testabilan repository bootstrap prema zaključanoj React + TypeScript / .NET 10 / Docker arhitekturi, bez business feature logike.

## Implementirano

- root `Plus5Enterprise.sln` s .NET 10 projektima Domain, Application, Infrastructure i API
- jednosmjerne project reference granice modularnog monolita
- četiri architecture testa koji iz compiled assembly metapodataka blokiraju zabranjene `Plus5.*` ovisnosti
- warnings-as-errors, nullable, C# 14, code-style analyzers i deterministic build kroz `Directory.Build.props`
- `global.json` s minimalnim SDK-om `10.0.102` i kompatibilnim roll-forwardom na noviji .NET 10 feature band
- minimalni API bez demo/business endpointa
- operativni `GET /health/live` i `GET /health/ready` endpointi
- React 19 + TypeScript 6 + Vite 8 strict frontend bootstrap bez feature routeova ili app shella
- npm lock datoteka, Node/npm engine granice, typecheck, lint i production build naredbe
- root `.gitignore`, `.dockerignore`, `.editorconfig` i `.gitattributes`
- multi-stage API i frontend Docker buildovi s digest-pinnanim base imageovima
- non-root runtime za oba containera
- lokalni Docker Compose za API i frontend, bez preuranjenog SQL Server servisa
- root README s clean-checkout build/test/dev/Docker uputama i granicama scopea
- ROADMAP 0.4 označen je `DONE`

## Namjerno nije implementirano

- business entiteti, value objecti, use caseovi i feature endpointi
- SQL Server/EF Core paketi, DbContext, migracije ili seed
- auth flow, korisnici, role i permissions
- API versioned business contract i standardizirani error envelope iz faze 1.3
- frontend routing, app shell, design tokeni i feature ekrani iz faze 1.5+
- environment/secrets contract iz faze 1.1
- hosted CI workflow; reproducibilne lokalne naredbe i lockovi su postavljeni, a CI se može uvesti kada se zaključi pipeline scope

## Promijenjene / dodane datoteke

Sve datoteke osim `docs/ROADMAP.md` dodane su u ovoj fazi.

| Područje | Datoteke |
|---|---|
| Root tooling | `.dockerignore`, `.editorconfig`, `.gitattributes`, `.gitignore`, `Directory.Build.props`, `global.json`, `Plus5Enterprise.sln`, `README.md`, `docker-compose.yml` |
| Domain | `backend/src/Plus5.Domain/AssemblyReference.cs`, `backend/src/Plus5.Domain/Plus5.Domain.csproj` |
| Application | `backend/src/Plus5.Application/AssemblyReference.cs`, `backend/src/Plus5.Application/Plus5.Application.csproj` |
| Infrastructure | `backend/src/Plus5.Infrastructure/AssemblyReference.cs`, `backend/src/Plus5.Infrastructure/Plus5.Infrastructure.csproj` |
| API | `backend/src/Plus5.Api/Program.cs`, `backend/src/Plus5.Api/Plus5.Api.csproj`, `backend/src/Plus5.Api/Dockerfile`, `backend/src/Plus5.Api/appsettings.json`, `backend/src/Plus5.Api/appsettings.Development.json`, `backend/src/Plus5.Api/Properties/launchSettings.json` |
| Architecture tests | `backend/tests/Plus5.ArchitectureTests/DependencyRulesTests.cs`, `backend/tests/Plus5.ArchitectureTests/Plus5.ArchitectureTests.csproj` |
| Frontend tooling | `frontend/.gitignore`, `frontend/.oxlintrc.json`, `frontend/package.json`, `frontend/package-lock.json`, `frontend/tsconfig.json`, `frontend/tsconfig.app.json`, `frontend/tsconfig.node.json`, `frontend/vite.config.ts` |
| Frontend source | `frontend/index.html`, `frontend/src/main.tsx`, `frontend/src/App.tsx`, `frontend/src/App.css`, `frontend/src/index.css`, `frontend/README.md` |
| Frontend container | `frontend/Dockerfile`, `frontend/nginx.conf` |
| Documentation | `docs/ROADMAP.md`, `docs/summaries/PHASE_0.4_REPOSITORY_BOOTSTRAP_SUMMARY.md` |

## Domain / database promjene

- Novi entiteti/value objects: nema; assembly reference markeri nisu business modeli.
- Promijenjena pravila: samo compile-time dependency smjer slojeva.
- Migracije: nema.
- Backfill/data migration: nema.

## API promjene

- `GET /health/live` → HTTP 200 `Healthy` kada proces radi.
- `GET /health/ready` → HTTP 200 `Healthy` za trenutačni bootstrap bez vanjskih dependencyja.
- Health endpointi su operativni i nisu business API pod `/api/v1`.
- Nema javnog business contracta niti breaking promjene.

## Frontend promjene

- Dodan minimalni React entry point i statična bootstrap status poruka.
- Nema routinga, server-state sloja, formi ni business statea.
- TypeScript strict provjere dolaze iz Vite `react-ts` baselinea bez `any` zaobilaženja.

## Security / authorization

- Nema auth implementacije ni nedokumentiranih authorization pretpostavki.
- Secrets i `.env` datoteke isključeni su iz Git/Docker contexta.
- API i frontend runtime containeri rade kao non-root korisnici (`1654` i `nginx`).
- Base imageovi su pinnani digestom.
- Produkcijski HTTPS ostaje obavezan; lokalni Development/Compose koriste eksplicitne HTTP portove.

## Ovisnosti

- Backend production projekti nemaju vanjske NuGet package ovisnosti izvan .NET shared frameworka.
- Test projekt koristi Microsoft.NET.Test.Sdk, xUnit, xUnit VS runner i coverlet collector iz službenog .NET 10 xUnit predloška.
- Frontend koristi React/ReactDOM, TypeScript, Vite React plugin i oxlint iz službenog Vite React TypeScript predloška.
- `package-lock.json` i NuGet project versions zaključavaju restore; vulnerability provjere nisu pronašle ranjive pakete.
- Nije potreban novi ADR jer odabir implementira postojeće ADR-0001, ADR-0002, ADR-0004 i ADR-0005; test/lint paketi nisu production architectural dependency.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `dotnet restore .\Plus5Enterprise.sln` | PASS |
| `dotnet build .\Plus5Enterprise.sln --no-restore --configuration Release` | PASS — 0 warnings, 0 errors |
| `dotnet test .\Plus5Enterprise.sln --no-build --configuration Release` | PASS — 4/4 architecture testa |
| `dotnet format .\Plus5Enterprise.sln --verify-no-changes --no-restore` | PASS |
| `dotnet list .\Plus5Enterprise.sln package --vulnerable --include-transitive` | PASS — nema poznatih ranjivih paketa |
| `npm ci` / audit | PASS — 0 vulnerabilities |
| `npm run lint` | PASS |
| `npm run build` | PASS — TypeScript + Vite production build |
| lokalni API `/health/live` i `/health/ready` | PASS — HTTP 200 / `Healthy` |
| `docker compose config --quiet` | PASS |
| `docker compose build` | PASS — API i frontend image |
| container runtime smoke test | PASS — API, frontend health i frontend root HTTP 200 |
| container user inspection | PASS — API `1654`, frontend `nginx` |

## Self-review

- [x] scope nije proširen izvan faze
- [x] nema nedokumentiranih business pretpostavki
- [x] build prolazi bez warninga
- [x] relevantni testovi prolaze
- [x] migracije nisu uvedene
- [x] auth/validation nisu proizvoljno definirani
- [x] Docker runtime je non-root i bez secreta
- [x] dokumentacija je ažurirana

## Arhitekturne odluke

- Implementirani ADR-0001, ADR-0002, ADR-0004 i ADR-0005.
- ADR-0003 ostaje zaključan, ali SQL Server/EF Core konkretno se uvode tek u Phase 1.2.
- Nema novog ADR-a.

## Poznati rizici / tehnički dug

- Hosted CI još nije uveden; lokalni clean-checkout gateovi dokumentirani su i reproducibilni.
- API readiness trenutačno provjerava samo vlastiti proces jer nema baze ni vanjskih servisa; dependency provjere dodaju se tek kada ti dependencyji postoje.
- Docker base digestovi zahtijevaju redovito kontrolirano osvježavanje radi security patchova.
- Produkcijski reverse-proxy/forwarded-header/TLS topology ostaje release/deployment gate.

## Otvorena pitanja

- Nema novih business ili arhitekturnih blokera za Phase 1.1.
- Postojeća auth, file, readiness-algorithm i kasnija business pitanja ostaju u `OPEN_QUESTIONS.md`.

## Točna početna točka za sljedeću fazu

Otvoriti Phase 1.1 Configuration, environments & secrets. Definirati tipiziranu backend konfiguraciju, environment validation, frontend public-config boundary, lokalne development primjere bez secreta i testove configuration failure ponašanja. Ne uvoditi persistence, API business contract, logging infrastrukturu, routing/app shell ni auth iz kasnijih podfaza.
