# Configuration, environments & secrets

## Status

**LOCKED foundation v1.0 — 2026-08-23**

Ovaj dokument definira konfiguracijski contract uveden u Phase 1.1. Vrijedi zajedno sa sigurnosnim, backend, frontend i Docker standardima.

## Podržani environmenti

API prihvaća isključivo sljedeće vrijednosti `ASPNETCORE_ENVIRONMENT`:

| Vrijednost | Namjena |
|---|---|
| `Development` | lokalni development i lokalni Docker Compose |
| `Staging` | produkcijski-slična provjera prije releasea |
| `Production` | produkcijski runtime |

Nepoznata vrijednost prekida startup. Environment nije business tenant, korisnička uloga niti feature flag.

## Backend konfiguracija

ASP.NET Core defaultni redoslijed izvora ostaje canonical: bazni `appsettings.json`, environment datoteka, Development user-secrets, environment varijable i command-line argumenti. Kasniji izvor ima prednost.

| Ključ | Environment varijabla | Obavezno | Secret | Pravilo |
|---|---|---:|---:|---|
| `Frontend:PublicOrigin` | `Frontend__PublicOrigin` | da | ne | apsolutni HTTP(S) origin bez credentialsa, patha, queryja ili fragmenta |
| `AllowedHosts` | `AllowedHosts` | da | ne | eksplicitni `;`-odvojeni host allowlist; `*` i `+` wildcardi nisu dopušteni |
| `ASPNETCORE_ENVIRONMENT` | isto | da | ne | `Development`, `Staging` ili `Production` |
| ASP.NET Core hosting URL/port | `ASPNETCORE_URLS` / `ASPNETCORE_HTTP_PORTS` | prema hostu | ne | runtime/deployment vrijednost, ne hardcodirati production URL u source |

`Frontend:PublicOrigin` je tipiziran i validira se na startupu. Development default je `http://localhost:5173`; Compose ga nadjačava s `http://localhost:8081`. `AllowedHosts` je u Developmentu ograničen na lokalne hostove. Staging i Production moraju obje vrijednosti dati kroz environment/deployment sloj.

## Frontend javna konfiguracija

Frontend smije primati samo javne vrijednosti kroz `VITE_*`, jer ih Vite ugrađuje u browser bundle.

| Ključ | Obavezno | Default | Pravilo |
|---|---:|---|---|
| `VITE_API_BASE_URL` | ne | `/api/v1` | root-relative path ili apsolutni HTTP(S) URL bez credentialsa, queryja i fragmenta |

Preferira se relative `/api/v1` kako bi isti frontend build ostao prenosiv između hostova. Apsolutna vrijednost dopuštena je samo kada deployment topology to stvarno zahtijeva. Parser se učitava pri startupu frontenda i neispravna vrijednost prekida inicijalizaciju.

`frontend/.env.example` je dokumentacijski primjer bez secreta. Stvarne lokalne `.env` datoteke nisu u Gitu.

## Secrets

Trenutačni Phase 1.1 runtime nema obaveznu secret vrijednost. Kada ih odgovarajuće faze uvedu, primjenjuje se:

- lokalni backend development: .NET user-secrets vezan uz `plus5-enterprise-api`
- lokalni frontend: nema server secreta; tajna nikada ne koristi `VITE_*`
- Staging/Production: environment ili deployment secrets mehanizam s ograničenim pristupom
- tajna se ne sprema u `appsettings*.json`, `.env.example`, Compose datoteku, image layer, log ili Git
- rotacija mora biti moguća bez promjene source koda

Primjer budućeg lokalnog backend unosa, tek nakon što odgovarajuća faza definira stvarni ključ:

```powershell
dotnet user-secrets set "Section:Key" "local-secret" --project .\backend\src\Plus5.Api\Plus5.Api.csproj
```

Placeholder `Section:Key` nije postojeći application contract i ne smije se koristiti u runtimeu.

## Fail-fast ponašanje

API ne pokreće listener ako je environment nepodržan, host allowlist nedostaje/koristi wildcard ili je obavezna grupirana konfiguracija neispravna. Frontend odbija neispravnu javnu API bazu. Poruke o grešci opisuju ime konfiguracijskog ključa i pravilo, ali ne ispisuju secret vrijednosti.

Nova grupirana backend konfiguracija mora dobiti strongly typed options, startup validation i testove. Novi frontend javni ključ mora dobiti tip, centralni parser/validation i mora biti naveden u `.env.example`. Značajna nova infrastrukturna ovisnost i dalje zahtijeva ADR.
