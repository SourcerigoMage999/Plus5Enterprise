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
- authorization prema deny-by-default Phase 1.6 identity contractu
- bounded pagination za potencijalno veliku listu
- API contract/integration test za success i relevantne failure grane

## Phase 1.6 authentication API

Svi endpointi koriste `/api/v1/auth`. Anonymous pristup je eksplicitno ograničen na `csrf`, registraciju, verifikaciju, login i recovery. `session`, `logout` i `change-password` zahtijevaju aktivnu Teacher sesiju.

| Metoda i ruta | Auth | Svrha |
|---|---|---|
| `GET /api/v1/auth/csrf` | anonymous | izdaje request token i `HttpOnly` anti-forgery cookie |
| `POST /api/v1/auth/register` | anonymous + CSRF + rate limit | Teacher self-registration |
| `POST /api/v1/auth/verify-email` | anonymous + CSRF + rate limit | jednokratna potvrda e-maila |
| `POST /api/v1/auth/resend-verification` | anonymous + CSRF + rate limit | generički resend flow |
| `POST /api/v1/auth/login` | anonymous + CSRF + rate limit | e-mail/password login i session cookie |
| `POST /api/v1/auth/forgot-password` | anonymous + CSRF + rate limit | generički enumeration-safe odgovor |
| `POST /api/v1/auth/reset-password` | anonymous + CSRF + rate limit | jednokratni reset i opoziv svih sesija |
| `GET /api/v1/auth/session` | Teacher | current session/account state |
| `POST /api/v1/auth/logout` | Teacher + CSRF | opoziv aktualne sesije |
| `POST /api/v1/auth/change-password` | Teacher + CSRF | promjena lozinke i opoziv svih sesija |

Browser client koristi `credentials: include` i za svaki write prvo dohvaća CSRF token. Auth cookie nije javni JavaScript contract i ne smije se kopirati u browser storage. Neaktivna/istekla/opozvana sesija vraća `401`; postojeći authorization denial vraća `403`.

## Phase 9.4 Lesson Builder goal-selection API

`/api/v1/lesson-plans` zahtijeva aktivnu Teacher sesiju. `participants` primjenjuje owner scope
prije pretrage i paginacije; `sessions` vraća samo buduće Scheduled termine istog ownera i
participant konteksta; `knowledge-components` vraća samo Active komponente Published modela.
`POST /api/v1/lesson-plans/` dodatno zahtijeva CSRF i atomarno stvara root, Draft v1 i exact
KC mappinge. Request ima bounded goal i 1–50 jedinstvenih KC identifikatora. Stabilni
business error kodovi dokumentirani su u `LESSON_BUILDER_GOAL_SELECTION.md`.

## Phase 9.5 Lesson structure API

`GET /api/v1/lesson-plans/{lessonPlanId}/structure/` vraća deterministic preview bez writea
ili već primijenjene canonical aktivnosti, target snapshot, KC kontekst, šest balance statusa
i odvojeni Material candidate count. `PUT .../target-duration` i `POST .../apply` zahtijevaju
Teacher sesiju, CSRF i `LessonPlanVersion.RowVersion`. Apply ne prihvaća proizvoljan activity
graph iz browsera i vrijedi samo za prazan Draft. Missing/foreign/archived vraća isti 404;
stale write vraća `409 concurrency_conflict`. Detalji su u
`LESSON_BUILDER_TIMELINE_BALANCE.md`.

## Phase 9.6 Lesson Plan editor API

`GET /api/v1/lesson-plans/{lessonPlanId}/editor/` vraća owner-scoped Draft plan context,
canonical aktivnosti, exact MaterialVersion attachmente i server-derived balance. Candidate
endpoint vraća samo current Active/Clean owner ili Shared+Use verzije, nakon access scopea i
uz bounded search/paginaciju.

`POST` i `DELETE .../activities/{activityId}/materials/{materialVersionId}` zahtijevaju
Teacher sesiju, CSRF i parent `LessonPlanVersion.RowVersion`; attachment se mijenja atomarno i
stale write vraća `409 concurrency_conflict`. Missing/foreign/archived aggregatei dijele isti
404. Response ne sadrži storage key, signed/public URL, scan detalje ni binary. Detalji su u
`LESSON_PLAN_EDITOR_MATERIAL_ATTACHMENTS.md`.

## Phase 9.7 Activity / Knowledge Block selection API

`GET /api/v1/lesson-plans/{lessonPlanId}/editor/activity-candidates` vraća samo current
Published verzije Active blokova koji odgovaraju participant i homework kontekstu Draft
plana. Search je bounded na 120 znakova, a opcionalni duration filter na 1–480 minuta.

`POST /api/v1/lesson-plans/{lessonPlanId}/editor/activities` prihvaća exact
`knowledgeBlockVersionId` i parent `rowVersion`, zahtijeva CSRF te atomarno dodaje novu
server-authoritative aktivnost na kraj Draft timelinea. Missing/foreign/archived dijele isti
404; lifecycle-nevaljan blok vraća `409 lesson_knowledge_block_unavailable`, a stale parent
`409 concurrency_conflict`. Detalji su u `LESSON_ACTIVITY_KNOWLEDGE_BLOCK_SELECTION.md`.
