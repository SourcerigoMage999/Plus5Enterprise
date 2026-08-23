# PLUS 5 Enterprise

PLUS 5 je aplikacija za organizaciju i izvođenje instrukcija/nastave. Repozitorij slijedi fazni razvoj definiran u [`docs/ROADMAP.md`](docs/ROADMAP.md), a dokumentacija u `docs/` je source of truth.

## Trenutačni status

Repository bootstrap sadrži samo tehnički temelj:

- .NET 10 modularni backend
- React + TypeScript + Vite frontend
- architecture testove za backend dependency smjer
- Docker build za API i frontend
- lokalni Docker Compose bez baze podataka

Business entiteti, persistence, auth i feature ekrani namjerno nisu dio ove faze.

## Preduvjeti

- .NET SDK `10.0.102` ili kompatibilan noviji feature band iz `10.0` linije
- Node.js `24.x`
- npm `11.16.0`
- Docker 29+ za container workflow

## Lokalna provjera

Backend:

```powershell
dotnet restore .\Plus5Enterprise.sln
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

API:

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
- `GET /health/ready` — aplikacija je spremna posluživati promet za trenutačni bootstrap scope

## Docker

Iz root direktorija:

```powershell
docker compose up --build
```

- frontend: `http://localhost:8081`
- API: `http://localhost:8080`

Compose u ovoj fazi ne pokreće SQL Server jer persistence foundation pripada ROADMAP fazi 1.2.

## Struktura

```text
backend/
  src/
    Plus5.Domain/          čiste domenske granice
    Plus5.Application/     use-case orchestration i portovi
    Plus5.Infrastructure/  implementacije vanjskih/persistence adaptera
    Plus5.Api/             HTTP i composition root
  tests/
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
- lokalne backend tajne koriste .NET user-secrets; trenutačni runtime još nema obaveznih secreta
- frontend `VITE_API_BASE_URL` je javna vrijednost s defaultom `/api/v1`; `VITE_*` nikada ne smije sadržavati secret
- lokalni frontend primjer nalazi se u `frontend/.env.example`, dok stvarne `.env` datoteke ostaju izvan Gita

Puni contract, precedence i pravila za Development/Staging/Production opisani su u [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md).

Prije svake implementacije slijediti [`docs/PROJECT_RULES.md`](docs/PROJECT_RULES.md) i obavezni redoslijed čitanja iz [`docs/README.md`](docs/README.md).
