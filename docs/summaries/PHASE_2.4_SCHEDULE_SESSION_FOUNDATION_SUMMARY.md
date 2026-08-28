# Phase 2.4 — Schedule/session foundation

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja vlasnika`

## Datum

`2026-08-28`

## Cilj faze

Uvesti najmanji trajni temelj konkretnog termina, redovitog Group rasporeda i individualne recurrence potreban za buduće ekrane 3.1–3.4, uz formalizirana pravila “samo ovaj termin” i “svi budući termini”, bez feature API/UI ili lesson-delivery scopea.

## Implementirano

- `Session` kao konkretan UTC termin s individualnim ili grupnim kontekstom
- statusi `Scheduled`, `InProgress`, `Held`, `Cancelled` i terminalna povijest otkazivanja
- `RecurringSessionSeries` kao verzionirano tjedno wall-clock pravilo za redoviti Group raspored ili individualnu Student recurrence
- jedinstveni `(RecurringSessionSeriesId, SeriesOccurrenceDate)` occurrence identitet
- “samo ovaj termin” reschedule koji označava series exception
- “svi budući termini” contract koji supersedea staru seriju i povezuje novu preko `PreviousSeriesId`
- Teacher-owned, ponovno upotrebljiva i arhivabilna `Location`
- fizička lokacija ili apsolutni HTTPS meeting URL, nikada oboje
- `rowversion`, Teacher-first kalendarski/overlap indeksi, restriktivni composite ownership FK-ovi i CHECK constrainti
- migracija `20260828202336_AddSchedulingFoundation`
- `SCHEDULING_FOUNDATION.md`, ADR-0013 i eksplicitno odgođene schedule odluke

## Namjerno nije implementirano

- Schedule/Session application use caseovi, endpointi, routeovi ili calendar UI
- generation horizon, background replenishment ili arbitrary recurrence engine
- conflict override, shared/multi-Teacher room booking ili notification dispatch
- attendance, stvarni početak/završetak održanog sata ili lesson evidence
- priprema, materijali, aktivnosti, domaća zadaća ili Knowledge Model
- seed, backfill ili automatska materijalizacija Session redaka u migraciji

## Domain i database contract

- konkretni Session čuva UTC interval i izvornu vremensku zonu; Series čuva lokalni dan i wall-clock timeslot
- svaki Session/Series ima točno jedan Group ili Student context usklađen s načinom/vrstom
- composite FK-ovi uključuju `TeacherAccountId` i odbijaju cross-owner Group, Student, Location i Series veze
- application sloj dodatno mora potvrditi jednak context između Sessiona i povezane Series
- invalidna ili ambiguous DST vremena ne smiju se tiho pomicati; budući save/preview mora tražiti korekciju
- conflict koristi half-open overlap pravilo i buduću kratku `Serializable` transakciju s ponovnom provjerom prije commita
- Held i Cancelled Sessioni su terminalni; otkazivanje ne briše redak

## API i frontend promjene

Nema novih endpointa, request/response contracta, routeova ni ekrana. Phase 2.4 ne otvara novi trust boundary.

## Security / authorization

- owner identitet ostaje server-derived iz autentificirane Teacher sesije u budućem application/API sloju
- same-Teacher composite FK-ovi fizički štite sve poslovne reference
- raw credentiali ili secret za online sastanak nisu dio modela; prihvaća se samo apsolutni HTTPS URL
- nema novih PII logova, public contracta ni promjene auth/CSRF/CORS ponašanja

## Ovisnosti

Nema novih NuGet ili npm ovisnosti.

## Testovi i provjere

| Provjera | Rezultat |
|---|---|
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/domain/persistence testovi | PASS — 114/114 |
| architecture testovi | PASS — 4/4 |
| novi scheduling testovi | PASS — 11/11 |
| `dotnet format --verify-no-changes` | PASS |
| EF pending-model provjera | PASS — nema pending promjena |
| idempotent migration SQL generation | PASS |
| NuGet vulnerability audit | PASS — bez poznatih ranjivosti |
| `npm ci` i audit | PASS — 0 ranjivosti |
| frontend lint/typecheck/build | PASS |
| frontend testovi | PASS — 3 files, 13/13 |
| upgrade SQL Server migration | PASS — 6→7 migracija, sentinel očuvan |
| clean SQL Server migration | PASS — 7 migracija, 0 Location/Series/Session seed redaka |
| ponovljena migracija | PASS — no-op |
| stvarni SQL constraint behavior | PASS — 9 negativnih scenarija odbijeno; valjani Session i rowversion promjena potvrđeni |
| Docker build/runtime | PASS |
| health endpointi | PASS — API live 200, ready 200; frontend 200 |
| non-root runtime | PASS — API UID 1654, frontend `nginx` UID 101, migracije UID 1654 |
| cleanup | PASS — testni containeri, mreža i volume uklonjeni; imageovi ostavljeni |

## Self-review

- [x] scope je ograničen na Phase 2.4 foundation
- [x] Session je odvojen od recurrence pravila i stvarno održanog sata
- [x] redoviti Group raspored nema duplicirani zapis na Group modelu
- [x] “samo ovaj” i “svi budući” imaju eksplicitan, povijesno siguran contract
- [x] Teacher ownership i cross-owner reference fizički su zaštićeni
- [x] status, vrijeme, context, location i occurrence invariante imaju domain/DB zaštitu
- [x] tenant-first indeksi i concurrency contract podržavaju buduće overlap writeove
- [x] nema API/UI, seeda, notificationa, attendancea ili knowledge/readiness scopea
- [x] clean/upgrade migracija i constrainti potvrđeni su na stvarnom SQL Serveru
- [x] Docker build, health i non-root runtime ponovno su potvrđeni

## Docker Desktop recovery bilješka

Docker Desktop `4.69.0` prvotno je padao na nevaljanim `dockerInference` i `docker-secrets-engine` runtime reparse-pointima. Runtime folderi su povratno preimenovani u `.stale-phase24-20260828` backupe, a službeni Docker Desktop restart zatim je vratio Linux engine. Docker AI postavka nije promijenjena; volumeovi, imageovi i projektni podaci nisu obrisani.

## Točna početna točka za sljedeću fazu

Otvoriti **Phase 3.1 Screen 2.1 Student list**. Dostupni su Teacher-owned Program, Student, Group/GroupMembership i Schedule/Session temelji. Implementacija mora koristiti postojeće ownership i auth granice, ne smije uvoditi readiness postotke prije Knowledge Model faze i ne smije širiti Phase 3.1 na Student create/dossier/edit use caseove.
