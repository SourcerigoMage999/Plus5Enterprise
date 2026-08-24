# API conventions, validation & error contract

## Status

**LOCKED v1.0 — 2026-08-24**

Ovaj dokument je canonical HTTP boundary contract za PLUS 5 i primjenjuje se na sve javne business endpointe od Phase 1.3 nadalje.

## Scope

Ovaj contract zaključava:

- URL versioning i route-group granicu
- REST/JSON request/response pravila
- server-side validation boundary
- standardizirani `ProblemDetails` error format
- sigurno globalno ponašanje za neočekivane exceptione
- osnovni bounded pagination contract

Ne definira business resurse, auth/authorization model, CORS, rate limiting, OpenAPI UI, logging/telemetry platformu ni idempotency ključ za konkretne write use caseove. Ti detalji ostaju svojim ROADMAP fazama.

## Versioning i routes

- Svi javni business endpointi počinju s `/api/v1`.
- Novi endpointi koriste `MapVersionOneApi()`; ne sastavljaju version prefix ručno.
- Resource pathovi koriste pluralne imenice i kebab-case kada imaju više riječi.
- Verzija se povećava samo za stvarno backward-incompatible javno ponašanje i zahtijeva zasebnu migration/versioning odluku.
- Operativni `/health/live` i `/health/ready` endpointi nisu business API i ostaju neversionirani.

## JSON i HTTP ponašanje

- Request i response modeli su eksplicitni API contracti; EF/domain entiteti se ne vraćaju izravno.
- JSON koristi ASP.NET Core web defaults, uključujući camelCase propertyje.
- Klijent šalje `Content-Type: application/json` za JSON body.
- Endpoint vraća najuži smisleni status: `200`, `201` s `Location`, `204`, `400`, `401`, `403`, `404`, `409` ili drugi standardni status prema use caseu.
- Očekivane validation/business greške modeliraju se kao kontrolirani rezultati, ne kao exceptions za uobičajeni control flow.
- Async endpoint propagira request cancellation do application/DB/network I/O granice.

## Validation

- `AddValidation()` uključuje built-in .NET 10 Minimal API validation.
- Request contract koristi DataAnnotations za transportna pravila i `IValidatableObject` samo kada je potreban složeniji object-level transport check.
- Frontend validation služi UX-u; server validation je autoritet.
- Business invariant ostaje u Domain/Application sloju čak i kada sličan transport check postoji na DTO-u.
- Nevaljan request vraća `400` i `HttpValidationProblemDetails` s `code=validation_failed` te `errors` mapom.
- Tekst validation poruke nije stabilni machine contract. Klijent se veže uz field path i `code`.

## ProblemDetails contract

Svi automatski 4xx/5xx odgovori bez vlastitog bodyja koriste `application/problem+json` i RFC `ProblemDetails` oblik proširen sljedećim obaveznim poljima:

| Polje | Contract |
|---|---|
| `type` | stabilni `urn:plus5:problem:<code>` za projektne default probleme |
| `title` | siguran ljudski čitljiv sažetak; nije machine branching vrijednost |
| `status` | HTTP status kao broj |
| `instance` | samo request path; query string se ne vraća |
| `code` | stabilni snake_case machine code |
| `traceId` | request/trace identifikator za korelaciju |
| `errors` | samo validation problem; mapa field patha na poruke |

Primjer:

```json
{
  "type": "urn:plus5:problem:validation_failed",
  "title": "Request validation failed.",
  "status": 400,
  "instance": "/api/v1/example-resources",
  "code": "validation_failed",
  "traceId": "00-example-trace-id-00",
  "errors": {
    "name": [
      "The Name field is required."
    ]
  }
}
```

Default kodovi:

| HTTP | `code` |
|---:|---|
| 400 validation | `validation_failed` |
| 400 ostalo | `invalid_request` |
| 401 | `authentication_required` |
| 403 | `forbidden` |
| 404 | `not_found` |
| 405 | `method_not_allowed` |
| 409 | `conflict` |
| 413 | `payload_too_large` |
| 415 | `unsupported_media_type` |
| 429 | `too_many_requests` |
| 500 | `internal_error` |
| 503 | `service_unavailable` |

Novi business-specific code mora biti stabilan, documented i testiran. Klijent ne smije granati ponašanje po `title` ili `detail` tekstu.

## Neočekivani exceptioni

- `GlobalExceptionHandler` hvata neočekivane exceptione i vraća `500 internal_error`.
- Klijent nikada ne dobiva exception tip, poruku, stack trace, connection string, query string ni druge interne detalje.
- Server bilježi samo exception tip i `traceId` u ovoj fazi. Strukturirani logging/telemetry enrichment pripada Phase 1.4.
- `detail` se uklanja iz defaultnog 500 odgovora u svim environmentima, uključujući Development.

## Pagination

- Potencijalno velike liste koriste `PaginationQuery`.
- Default je `page=1&pageSize=25`.
- `page` mora biti najmanje 1.
- `pageSize` mora biti 1–100; klijent ne može zatražiti neograničenu listu.
- `PagedResponse<T>` vraća `items`, `page`, `pageSize`, `totalCount` i izračunati `totalPages`.
- Offset pagination je početni contract. Ako mjerenja ili stabilnost velikih promjenjivih skupova zatraže cursor pagination, to je eksplicitna API contract odluka, ne tiha zamjena.

## Endpoint completion gate

Svaki novi javni endpoint mora imati:

- `/api/v1` route-group mapping
- eksplicitni request/response contract
- server-side validation
- kontrolirani status/error mapping
- authorization kada Phase 1.6 zaključa identity contract
- bounded pagination za potencijalno veliku listu
- API contract/integration test za success i relevantne failure grane
