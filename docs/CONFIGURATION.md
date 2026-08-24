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
| `ConnectionStrings:Plus5` | `ConnectionStrings__Plus5` | da | **da** | SQL Server database + runtime identity; `Encrypt=True`; nepouzdani certifikat samo u Developmentu |
| `Observability:TraceSamplingRatio` | `Observability__TraceSamplingRatio` | ne | ne | default `0.1`, Development `1.0`; raspon `(0, 1]` |
| `Observability:OtlpEndpoint` | `Observability__OtlpEndpoint` | ne | ne | prazno isključuje export; apsolutni HTTP(S) URI bez credentialsa/queryja/fragmenta; HTTPS izvan Developmenta |
| `Email:Host` | `Email__Host` | da | ne | SMTP host; Development default `localhost`, Compose `host.docker.internal` |
| `Email:Port` | `Email__Port` | da | ne | SMTP port 1–65535; Development default `1025` |
| `Email:UseSsl` | `Email__UseSsl` | da | ne | mora biti `true` izvan Developmenta |
| `Email:FromAddress` | `Email__FromAddress` | da | ne | valjana sender adresa |
| `Email:UserName` | `Email__UserName` | prema provideru | **da** | mora biti zadan zajedno s passwordom |
| `Email:Password` | `Email__Password` | prema provideru | **da** | mora biti zadan zajedno s usernameom |
| `DataProtection:CertificatePath` | `DataProtection__CertificatePath` | Staging/Production | ne | apsolutni path do montiranog PKCS#12 certifikata; zajedno s passwordom |
| `DataProtection:CertificatePassword` | `DataProtection__CertificatePassword` | Staging/Production | **da** | password certifikata; isključivo secrets sloj |
| `ASPNETCORE_ENVIRONMENT` | isto | da | ne | `Development`, `Staging` ili `Production` |
| ASP.NET Core hosting URL/port | `ASPNETCORE_URLS` / `ASPNETCORE_HTTP_PORTS` | prema hostu | ne | runtime/deployment vrijednost, ne hardcodirati production URL u source |

`Frontend:PublicOrigin` je tipiziran i validira se na startupu. Development default je `http://localhost:5173`; Compose ga nadjačava s `http://localhost:8081`. `AllowedHosts` je u Developmentu ograničen na lokalne hostove. `ConnectionStrings:Plus5` nikada nema committed default vrijednost. Staging i Production moraju sve obavezne vrijednosti dati kroz environment/deployment sloj.

Observability options su tipizirani i validiraju se prije pokretanja listenera. OTLP exporter nije registriran dok endpoint nije postavljen; repo ne sadrži observability credentials niti preuranjeni collector. Puni logging/telemetry contract nalazi se u `OBSERVABILITY.md`.

SMTP options su tipizirani i validiraju se pri startupu. Development očekuje lokalni SMTP capture server na host portu `1025`; Compose ne uvodi nedokumentirani produkcijski mail servis. Staging/Production moraju dostaviti TLS SMTP konfiguraciju, a credentials isključivo kroz environment/secrets sloj. Verifikacijski i recovery token nikada se ne logiraju.

ASP.NET Core Data Protection key ring dijeli se kroz bazu kako bi auth i CSRF cookieji ostali valjani nakon restarta i između više API instanci. Development smije spremiti nezaštićeni key XML u lokalnu bazu. Staging/Production moraju montirati PKCS#12 certifikat i dostaviti njegov password kroz secrets sloj; startup se prekida ako certifikat nedostaje, nije dostupan ili se ne može učitati. Certifikat i password nikada se ne spremaju u image, Git ili appsettings datoteke.

## Frontend javna konfiguracija

Frontend smije primati samo javne vrijednosti kroz `VITE_*`, jer ih Vite ugrađuje u browser bundle.

| Ključ | Obavezno | Default | Pravilo |
|---|---:|---|---|
| `VITE_API_BASE_URL` | ne | `/api/v1` | root-relative path ili apsolutni HTTP(S) URL bez credentialsa, queryja i fragmenta |

Preferira se relative `/api/v1` kako bi isti frontend build ostao prenosiv između hostova. Apsolutna vrijednost dopuštena je samo kada deployment topology to stvarno zahtijeva. Parser se učitava pri startupu frontenda i neispravna vrijednost prekida inicijalizaciju.

`frontend/.env.example` je dokumentacijski primjer bez secreta. Stvarne lokalne `.env` datoteke nisu u Gitu.

## Secrets

Od Phase 1.2 connection string i SQL credentials su obavezni secrets. Primjenjuje se:

- lokalni backend development: .NET user-secrets vezan uz `plus5-enterprise-api`
- lokalni frontend: nema server secreta; tajna nikada ne koristi `VITE_*`
- Staging/Production: environment ili deployment secrets mehanizam s ograničenim pristupom
- tajna se ne sprema u `appsettings*.json`, `.env.example`, Compose datoteku, image layer, log ili Git
- rotacija mora biti moguća bez promjene source koda

Lokalni Compose koristi necommitani root `.env` s tri odvojena secreta:

- `PLUS5_SQL_SA_PASSWORD` — samo SQL Server bootstrap/init
- `PLUS5_SQL_MIGRATION_PASSWORD` — schema migration identitet
- `PLUS5_SQL_APP_PASSWORD` — least-privilege API runtime identitet

Migration alat koristi `PLUS5_MIGRATION_CONNECTION_STRING`. `PLUS5_MIGRATION_ALLOW_UNTRUSTED_CERTIFICATE=true` dopušten je isključivo za lokalni Development container; Staging/Production moraju koristiti provjerljiv certifikat.

Primjer budućeg lokalnog backend unosa, tek nakon što odgovarajuća faza definira stvarni ključ:

```powershell
dotnet user-secrets set "Section:Key" "local-secret" --project .\backend\src\Plus5.Api\Plus5.Api.csproj
```

Placeholder `Section:Key` nije postojeći application contract i ne smije se koristiti u runtimeu.

## Fail-fast ponašanje

API ne pokreće listener ako je environment nepodržan, host allowlist nedostaje/koristi wildcard, connection string nedostaje/ne koristi encryption ili je obavezna grupirana konfiguracija neispravna. Frontend odbija neispravnu javnu API bazu. Poruke o grešci opisuju ime konfiguracijskog ključa i pravilo, ali ne ispisuju secret vrijednosti.

Nova grupirana backend konfiguracija mora dobiti strongly typed options, startup validation i testove. Novi frontend javni ključ mora dobiti tip, centralni parser/validation i mora biti naveden u `.env.example`. Značajna nova infrastrukturna ovisnost i dalje zahtijeva ADR.
