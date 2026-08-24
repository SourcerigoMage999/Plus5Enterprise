# Logging & telemetry foundation

## Status

**LOCKED foundation v1.0 — 2026-08-24**

Ovaj dokument definira observability contract uveden u Phase 1.4. Vrijedi zajedno s backend, API, configuration, security, testing i Docker standardima.

## Ciljevi

- svaki zahtjev može se korelirati od HTTP odgovora do loga i distribuiranog tracea
- container logovi su strojno čitljivi bez posebnog log drivera
- osnovni traceovi i metrike mogu se poslati standardnom OTLP odredištu bez vendor lock-ina
- telemetrija ne smije postati sekundarni kanal za secrets, PII ili request payload

Observability pomaže dijagnostici; nije audit trail, business analytics niti zamjena za domenske događaje.

## Strukturirani logovi

API piše jedan JSON objekt po retku na standardni output. Timestamp je UTC, a logging scope uključuje W3C `TraceId` i `SpanId` kada postoji aktivnost.

Application default log razina je `Information`; ASP.NET Core framework default je `Warning`. Deployment ih može promijeniti standardnim `Logging__LogLevel__*` environment varijablama, ali debug/trace razine nisu production default.

Završetak zahtjeva zapisuje event `2000` sa strukturiranim poljima:

| Polje | Značenje |
|---|---|
| `RequestMethod` | HTTP metoda |
| `RouteTemplate` | route predložak, npr. `/api/v1/students/{id}`; `unmatched` ako endpoint nije pronađen |
| `StatusCode` | konačni HTTP status |
| `ElapsedMilliseconds` | trajanje zahtjeva |
| `TraceId` | canonical W3C trace ID |

Ne zapisuje se konkretni URL path, query string, headeri, body, cookie, authorization vrijednost, exception poruka ni puni korisnički payload. Globalni exception boundary smije zapisati exception tip i trace ID, ali ne njegove interne detalje.

Uspješni `GET /health/live` zahtjevi ne stvaraju completion log ni server trace kako container healthcheck ne bi stvarao nepotrebnu buku. `/health/ready` ostaje vidljiv jer provjerava vanjsku ovisnost i migration stanje.

## Korelacija

ASP.NET Core koristi W3C Trace Context. Valjani dolazni `traceparent` nastavlja se; inače server stvara novi trace. Svaki odgovor dobiva header:

```text
X-Trace-Id: <32 lowercase hexadecimal characters>
```

Isti ID koristi `ProblemDetails.traceId`, request completion log i OpenTelemetry trace. Klijent ga može priložiti prijavi problema, ali ne smije ga tretirati kao secret ili authorization dokaz.

## OpenTelemetry

API koristi službeni OpenTelemetry .NET SDK i ASP.NET Core/runtime instrumentaciju.

Resource identitet sadrži:

- `service.name=plus5-api`
- assembly verziju servisa
- runtime instance ID izveden iz imena hosta/containera
- `deployment.environment.name`

Traces koriste parent-based ratio sampler. Default ratio je `0.1`; Development koristi `1.0`. ASP.NET Core i .NET runtime metrička instrumentacija registrirane su neovisno o trace samplingu.

OTLP export je isključen dok `Observability:OtlpEndpoint` nije postavljen. Repozitorij namjerno ne dodaje collector, dashboard ili vendor servis. Production topology, retention, pristup, TLS/mTLS i exporter credentials moraju biti zaključani prije stvarnog vanjskog odredišta.

## Konfiguracija

| Ključ | Default | Pravilo |
|---|---:|---|
| `Observability:TraceSamplingRatio` | `0.1` (`1.0` u Developmentu) | veće od `0` i manje ili jednako `1` |
| `Observability:OtlpEndpoint` | prazno / export isključen | apsolutni HTTP(S) URI bez credentialsa, queryja ili fragmenta; HTTPS je obavezan izvan Developmenta |

Environment varijable su `Observability__TraceSamplingRatio` i `Observability__OtlpEndpoint`. Neispravna vrijednost prekida startup bez ispisivanja endpoint vrijednosti.

## Privacy i sigurnosna granica

Prije exporta server tracea uklanjaju se potencijalno osjetljivi HTTP tagovi, uključujući puni URL, path, query i user-agent. Route-template/low-cardinality tagovi koje daje instrumentacija ostaju dostupni za agregaciju.

Zabranjeno je dodavati sljedeće u log ili telemetry tag bez zasebnog security/privacy reviewa:

- passwords, tokens, API keys, session/cookie vrijednosti ili connection stringovi
- request/response body i upload sadržaj
- query string ili proizvoljni header
- ime, e-mail, bilješke, obrazovni rezultati ili drugi izravni PII učenika/skrbnika/nastavnika
- nekontrolirani exception message ili stack trace u javni odgovor

Novi enrichment mora biti bounded/low-cardinality, potreban za operacije i pokriven testom protiv curenja osjetljivih vrijednosti.

## Operativna provjera

Lokalni JSON logovi mogu se pratiti kroz container output:

```powershell
docker compose logs --follow api
```

Za korelaciju koristiti `X-Trace-Id` iz odgovora i tražiti jednaki `TraceId`. Stvarni collector, alert pravila, retention i produkcijski dashboardi ostaju release/deployment fazi kada postoji odabrana operativna topologija.
