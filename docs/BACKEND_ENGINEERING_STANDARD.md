# BACKEND_ENGINEERING_STANDARD

## Status

**MANDATORY v1.0 — 2026-08-23**

## 1. Platform

- C# / ASP.NET Core / .NET 10
- nullable reference types uključeni
- warnings koji ukazuju na stvarne probleme ne ignoriraju se bez razloga
- async I/O end-to-end

## 2. Design principles

Kod mora favorizirati:

- high cohesion
- low coupling
- explicit dependencies
- SOLID kada rješava realan problem
- composition over inheritance
- jednostavan kod prije patterna bez potrebe
- domenske invariante blizu domenskog modela

DDD/CQRS/repository pattern nisu cilj sami sebi. Uvode se samo kada odgovaraju složenosti use casea i postojećem baselineu.

## 3. Controllers/endpoints

HTTP boundary ne smije sadržavati poslovnu logiku.

Endpoint treba:

1. primiti/parsirati request
2. pokrenuti validation/auth boundary
3. pozvati application use case
4. mapirati rezultat u standardizirani HTTP response

Ne vraćati EF entitete izravno kao javni API contract.

## 4. API contracts

- request/response modeli su eksplicitni
- backward-incompatible promjena javnog API contracta zahtijeva versioning/migration odluku
- API ne smije slučajno izložiti interne/sensitive propertyje
- `ProblemDetails` ili projektni standardizirani error envelope koristi se konzistentno nakon faze 1.3

## 5. Validation

- validacija na API/application trust boundaryju
- frontend validation nije sigurnosna kontrola
- DB constraint ostaje zadnja zaštita integriteta
- business invariant nije samo DTO validator ako vrijedi neovisno o transportu

## 6. EF Core

- DbContext lifetime standardno scoped per request/unit of work
- ne skrivati EF Core iza generičkog repositoryja koji samo prepisuje `DbSet` API bez koristi
- repository koristiti kada daje stvarnu domensku granicu/semantiku
- read-only queryji preferiraju projekcije i `AsNoTracking`
- `Include` koristiti svjesno; ne učitavati cijele graphove iz komocije
- cancellation token propagirati do DB/network I/O gdje request lifecycle to podržava

## 7. Error handling

- očekivane business greške modelirati kontrolirano
- ne koristiti exceptions za uobičajeni validation/control flow
- neočekivane exceptione globalno hvatati, korelirati i logirati bez curenja internog stack tracea klijentu

## 8. Logging

- structured logging
- correlation/request identifier gdje je korisno
- logovi ne sadrže passwords, auth tokens, refresh tokens, API keys ili cijele sensitive payloade
- PII se logira samo kada je opravdano i minimizirano

## 9. Configuration

- strongly typed options za grupiranu konfiguraciju
- startup validation za obavezne vrijednosti
- bez hardcoded production secreta ili environment-specific URL-ova
- Development/Staging/Production razlike rješavati config slojem, ne `if` kaosom kroz business kod

## 10. Time

- trajni timestampovi u UTC
- business timezone prikaz/konverzija eksplicitna
- kod koji ovisi o trenutnom vremenu treba biti testabilan kroz clock/time abstraction kada logika to zahtijeva

## 11. External services

- timeout mora biti eksplicitan
- retry samo za sigurne/retriable operacije
- ne raditi beskonačne retry loopove
- idempotency razmotriti kod write operacija koje se mogu ponoviti
- mrežni poziv ne držati unutar DB transakcije

## 12. Performance

Za 10.000+ korisnika prioritet su:

- učinkoviti DB queryji
- pagination
- izbjegavanje N+1
- razuman payload size
- async I/O
- connection pooling
- caching samo uz definiranu invalidaciju

Ne optimizirati bez mjerenja, ali ne uvoditi očite O(n²)/full-table obrasce u request path.

## 13. Dependency policy

Nova NuGet biblioteka mora imati:

- konkretan razlog
- aktivno održavanje/licencu kompatibilnu projektu
- security review
- zapis u phase summaryju; značajna arhitekturna ovisnost ide u ADR

## 14. Code review blockers

Blocking su:

- business logika u controlleru
- server authorization samo na frontendu
- SQL injection mogućnost
- plaintext secret/password/token
- nedeterminističan destructive migration
- sync-over-async u hot request pathu
- N+1 nad velikim skupovima
- endpoint bez potrebne validacije/autorizacije
- feature iz buduće ROADMAP faze
