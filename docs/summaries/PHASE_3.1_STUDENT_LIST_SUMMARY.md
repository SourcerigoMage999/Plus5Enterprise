# Phase 3.1 — Screen 2.1 Student list

## Status

`DONE — AWAITING OWNER REVIEW`

Commit/push gate: `READY — nije izvršen bez odobrenja`

## Datum

`2026-08-29`

## Cilj faze

Isporučiti prvi stvarni Teacher feature ekran: siguran, pretraživ, filtriran i paginiran popis vlastitih učenika, bez širenja na create/dossier/edit ili izmišljeni Knowledge napredak.

## Implementirano

- autorizirani `GET /api/v1/students` i `GET /api/v1/students/overview`
- server-derived Teacher ownership i isključivanje arhiviranih učenika
- pretraga imena/prezimena/nadimka, četiri filtera, stabilno sortiranje i bounded pagination
- Program, SchoolGrade, aktivna GroupMembership i zadnji `Held` Session projekcije
- centralni frontend API client s cookie credentials i postojećim 401/403 eventima
- `/students` ekran s tabličnim i kartičnim prikazom te URL-backed stanjem
- loading, filtered/unfiltered empty, error/retry, pagination i responsive stanja
- status/program overview bez dodatnog frontend business izračuna
- neutralni “Nije dostupno” napredak; buduće akcije su onemogućene i označene
- zaključan `STUDENT_LIST.md` contract

## Namjerno nije implementirano

- Student create, dossier, edit ili archive
- Group CRUD i membership promjene
- komunikacijski flowovi
- readiness, Knowledge Model ili progress postotak
- novi domain entiteti, migracije, tablice, paketi ili seed podaci

## Domain / database promjene

Nema promjene domenskog modela, sheme, migracije ili backfilla. Read model koristi postojeće Student, Program, SchoolGrade, GroupMembership, Group i Session tablice.

## API promjene

- novi read-only `/api/v1/students` resource s eksplicitnim request/response contractima
- default pagination parametri stvarno su opcionalni na HTTP granici
- nevaljani parametri vraćaju standardni validation `400`
- anonimni zahtjev vraća `401`; Teacher ID nikada nije dio requesta
- promjena je aditivna i nije breaking

## Frontend promjene

- centraliziran shared API boundary; auth API koristi isti client
- Student route više nije placeholder
- oba view moda, filteri, search, page i clear-filter ponašanje imaju stvarna stanja
- nema lažnih podataka ni dead-link akcija

## Security / authorization

- svi query korijeni i povezani Teacher-owned resursi scopeani su claims identitetom
- arhivirani i cross-owner Student redci ne izlaze iz persistence boundaryja
- UI ne prima nepotrebne kontaktne/Guardian podatke
- postojeći cookie, CSRF i fail-closed auth contract nije oslabljen

## Testovi i provjere

| Provjera | Rezultat |
|---|---|
| backend Release build | PASS — 0 warninga, 0 grešaka |
| API/domain/persistence testovi | PASS — 116/116 |
| architecture testovi | PASS — 4/4 |
| frontend testovi | PASS — 4 files, 16/16 |
| format/lint/typecheck/build | PASS — 0 upozorenja |
| auth/validation endpoint journey | PASS — 401/200/400 |
| owner/archive/filter/last-held query testovi | PASS |
| stvarna SQL Server EF translacija | PASS |
| Docker image build i runtime | PASS |
| health endpointi | PASS — live, ready i frontend 200 |
| non-root runtime | PASS — API 1654, frontend `nginx` |
| cleanup | PASS — testni containeri, mreža, volume i privremeni probe uklonjeni; imageovi ostavljeni |
| canonical PNG visual comparison | PASS — canonical `2.1. Popis učenika.png` iz dostavljenog ZIP-a |
| desktop screenshot comparison | PASS — 1536×1024 |
| mobile adaptation review | PASS — 390×844 |

## Self-review

- [x] scope je ograničen na Phase 3.1 read feature
- [x] API i UI slijede zaključane conventions/auth/frontend standarde
- [x] ownership se izvodi na serveru i testiran je protiv cross-owner curenja
- [x] nema migracije ni preuranjenog write modela
- [x] nema izmišljenog progress postotka
- [x] svi loading/empty/error/filter/pagination boundaryji su eksplicitni
- [x] EF query potvrđen je na stvarnom SQL Server provideru
- [x] Docker health i non-root runtime su potvrđeni
- [x] stvarni desktop ekran uspoređen je s canonical PNG-om
- [x] spacing, typography, colors, proportions i visual hierarchy prošli su visual acceptance
- [x] mobilna prilagodba vizualno je pregledana nakon završnih dorada

## Arhitekturne odluke

Nema novog ADR-a. Implementacija primjenjuje postojeće ADR/auth/API/frontend/domain contracte.

## Poznati rizici / tehnički dug

- offset pagination je namjerno početni API contract; cursor pagination zahtijeva zasebnu odluku ako mjerenja pokažu potrebu
- zadnji grupni sat prati trenutnu aktivnu grupu; povijesni dossier semantics pripada Phase 3.3

## Vizualna prihvatljivost i namjerna odstupanja

Dokazi usporedbe nalaze se u `docs/visual-acceptance/`. Inicijali se koriste umjesto fotografija jer odobreni Student/API contract nema avatar izvor. Napredak ostaje neutralna crtica i siva traka jer Knowledge/Evidence model još nije dostupan. Create, dossier, communication i edit kontrole vidljive su radi vizualne hijerarhije, ali ostaju onemogućene do pripadajućih faza. Export i Groups summary nisu dodani jer nisu dio zaključanog Phase 3.1 contracta; ne prikazuju se klijentski izvedeni ili lažni podaci.

## Točna početna točka za sljedeću fazu

Otvoriti **Phase 3.2 Screen 2.3 Create student** prema canonical source paketu i zaključanim Phase 3.1 contractima.
