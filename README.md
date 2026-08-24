# PLUS 5 Enterprise

PLUS 5 je aplikacija za organizaciju i izvođenje instrukcija/nastave. Repozitorij slijedi fazni razvoj definiran u [`docs/ROADMAP.md`](docs/ROADMAP.md), a dokumentacija u `docs/` je source of truth.

## Trenutačni status

Repozitorij trenutačno sadrži cross-cutting tehnički temelj:

- .NET 10 modularni backend
- React + TypeScript + Vite frontend
- architecture testove za backend dependency smjer
- Docker build za API i frontend
- SQL Server 2025 + EF Core persistence foundation
- kontrolirane EF Core migracije i database-aware readiness
- versionirani `/api/v1` contract, built-in validation i standardizirani sigurni `ProblemDetails` odgovori
- JSON stdout logovi, W3C trace korelacija te vendor-neutral OpenTelemetry traces/metrics temelj
- responsive frontend app shell, centralna SPA navigacija, pristupačni route placeholderi i CSS design tokeni

Business entiteti, auth i feature ekrani još nisu implementirani.

## Preduvjeti

- .NET SDK `10.0.102` ili kompatibilan noviji feature band iz `10.0` linije
- Node.js `24.x`
- npm `11.16.0`
- Docker 29+ za container workflow

## Lokalna provjera

Backend:

```powershell
dotnet restore .\Plus5Enterprise.sln
dotnet tool restore
dotnet build .\Plus5Enterprise.sln --no-restore --configuration Release
dotnet test .\Plus5Enterprise.sln --no-build --configuration Release
```

Frontend:

```powershell
Set-Location .\frontend
npm ci
npm run lint
npm run build
```

## Pokretanje u developmentu

API za lokalni host development zahtijeva valjani `ConnectionStrings:Plus5` kroz user-secrets i dostupnu migriranu SQL Server bazu. Najjednostavniji potpuni razvojni workflow je Docker Compose opisan niže.

API nakon konfiguriranja baze:

```powershell
dotnet run --project .\backend\src\Plus5.Api\Plus5.Api.csproj
```

Frontend:

```powershell
Set-Location .\frontend
npm run dev
```

API health endpointi:

- `GET /health/live` — proces je aktivan
- `GET /health/ready` — SQL Server je dostupan i nema neprimijenjenih EF Core migracija

## Docker

Kopirati lokalni secret primjer i postaviti tri različite snažne lozinke:

```powershell
Copy-Item .\.env.example .\.env
```

Zatim iz root direktorija:

```powershell
docker compose up --build --wait
```

- frontend: `http://localhost:8081`
- API: `http://localhost:8080`
- SQL Server: `127.0.0.1:1433` (samo lokalni loopback)

Compose redoslijed je `database` → `database-init` → jednokratni `migrations` → `api` → `frontend`. Migracije se ne izvršavaju na startupu svake API instance. Named volume `plus5-sql-data` čuva lokalne DB podatke nakon `docker compose down`.

Detaljni migration, identity i schema contract nalazi se u [`docs/PERSISTENCE.md`](docs/PERSISTENCE.md).

Versioning, validation, error i pagination pravila nalaze se u [`docs/API_CONVENTIONS.md`](docs/API_CONVENTIONS.md).

Logging, trace ID, telemetry privacy i opcionalni OTLP contract nalaze se u [`docs/OBSERVABILITY.md`](docs/OBSERVABILITY.md).

Frontend shell, route registry, design tokeni i accessibility foundation nalaze se u [`docs/FRONTEND_FOUNDATION.md`](docs/FRONTEND_FOUNDATION.md).

## Struktura

```text
backend/
  src/
    Plus5.Domain/          čiste domenske granice
    Plus5.Application/     use-case orchestration i portovi
    Plus5.Infrastructure/  implementacije vanjskih/persistence adaptera
    Plus5.Api/             HTTP i composition root
  tests/
    Plus5.Api.Tests/
    Plus5.ArchitectureTests/
frontend/                  React + TypeScript + Vite
docs/                      izvršna projektna dokumentacija
```

Dependency smjer:

```text
Api -> Application
Api -> Infrastructure -> Application -> Domain
                         Infrastructure -> Domain
```

Domain ne ovisi o drugim PLUS 5 projektima. Application ovisi samo o Domainu. Architecture testovi čuvaju ta pravila.

## Konfiguracija i tajne

- podržani API environmenti su `Development`, `Staging` i `Production`; nepoznata vrijednost prekida startup
- `AllowedHosts` mora biti eksplicitni host allowlist; wildcard vrijednosti nisu dopuštene
- obavezni backend `Frontend__PublicOrigin` koristi tipiziranu startup validaciju
- `ConnectionStrings__Plus5` je obavezni backend secret; ne zapisuje se u `appsettings`, Compose source ni frontend
- lokalni Compose koristi odvojene `sa`, migration i least-privilege application lozinke iz necommitane `.env` datoteke
- frontend `VITE_API_BASE_URL` je javna vrijednost s defaultom `/api/v1`; `VITE_*` nikada ne smije sadržavati secret
- lokalni frontend primjer nalazi se u `frontend/.env.example`, dok stvarne `.env` datoteke ostaju izvan Gita

Puni contract, precedence i pravila za Development/Staging/Production opisani su u [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md).

Prije svake implementacije slijediti [`docs/PROJECT_RULES.md`](docs/PROJECT_RULES.md) i obavezni redoslijed čitanja iz [`docs/README.md`](docs/README.md).
