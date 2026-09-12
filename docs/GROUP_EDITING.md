# Uredi grupu — Phase 3.7 / Screen 2.9

## Status

**Implementirano — završni SA review, 2026-09-11.** Business blocker promjene Programa
zatvoren je izričitom SA odlukom u ADR-0016. Nema automatskog otvaranja Phase 4 niti
commita/pusha.

Izvori: [2.9 Uredi grupu](source_specs/2.9_Uredi_grupu.md), canonical teacher PNG 2.9,
[Group foundation](GROUP_FOUNDATION.md), [Scheduling foundation](SCHEDULING_FOUNDATION.md),
[Group create](GROUP_CREATION.md), [Group list](GROUP_LIST.md) i projektni standardi.

## Zaključani edit contract

- Uređuje se postojeći owner-scoped, nearhivirani `Group`; nema paralelne kopije zapisa.
- Naziv, opis, Program, SchoolGrade, capacity i status slijede postojeći Group contract.
- Unique normalized naziv i SQL `rowversion` provjeravaju se pri svakom spremanju.
- Capacity se ne može smanjiti ispod stvarnog broja aktivnih članstava.
- Promjena `ProgramId` dopuštena je samo kada grupa nema aktivna članstva.
- Uz aktivne učenike Program control je zaključan s objašnjenjem; backend isto pravilo
  ponovno provjerava u transakciji i vraća kontrolirani konflikt.
- Nema masovne promjene Student Programa i nema automatskog zatvaranja članstava.
- Promjena statusa nema skrivene side-effecte nad članstvima, Programom ili rasporedom.

## Raspored i povijesna konzistentnost

Raspored ostaje opcionalan. Neizmijenjeni raspored ne stvara nove serije ni Sessione.
Promjena postojećeg rasporeda vrijedi od eksplicitnog budućeg lokalnog datuma, strogo
nakon današnjeg datuma u `Europe/Zagreb`. Uklanjanje rasporeda vrijedi od sutra.

Pri promjeni se postojeća aktivna pravila supersedeaju i skraćuju do dana prije datuma
promjene, odnosno do vlastitog `StartsOn` ako buduće pravilo još nije počelo. Njihovi
budući `Scheduled` Sessioni od tog datuma se otkazuju; povijesni,
`Held`, `InProgress`, već otkazani i raniji occurrence zapisi ostaju očuvani. Nova pravila
čuvaju `PreviousSeriesId`, otvoreni `EndsOn` ostaje null i materijalizira se početnih
12 tjedana prema ADR-0015. Dodavanje rasporeda grupi koja ga nema može početi danas ili
u budućnosti.

Invalidno/dvosmisleno DST vrijeme, interni overlap, Teacher konflikt ili Location konflikt
odbijaju cijeli write. Trenutačne serije grupe izuzimaju se iz conflict provjere zato što
se atomarno zamjenjuju. Nema `Save anyway`. Group detalji, supersede/cancel operacije,
successor serije i novi Sessioni spremaju se u jednoj Serializable transakciji.

## Članstva

Donja zona ponovno koristi zaključani Phase 3.5 add/remove membership workflow. Članstvo
je zasebna eksplicitna radnja s vlastitim CSRF, ownership, capacity i rowversion provjerama.
Dok su glavni podaci grupe nespremljeni, članovske akcije su zaključane kako refresh nakon
njihova writea ne bi tiho odbacio promjene forme. Nakon membership promjene edit read model
se ponovno učitava i Program/capacity pravila koriste aktualno stanje.

## API

Svi endpointi su Teacher-only; owner dolazi iz autentificirane sesije. Missing,
arhivirani i cross-owner ID vraćaju privacy-preserving 404.

| Metoda / putanja | Contract |
|---|---|
| `GET /api/v1/groups/{id}/edit` | Group detalji, member count, rowversion i aktualne nesupersedane serije |
| `PUT /api/v1/groups/{id}` | CSRF; detalji/status/capacity/rowversion i cijeli opcionalni raspored |

Uspjeh vraća 200 i `{ id, sessionCount }`. Nevaljan unos, CSRF ili lokalno vrijeme vraća
400; bez sesije 401, pogrešna rola 403, nedostupna referenca 404; duplicate naziv,
stale rowversion, nedopuštena promjena Programa, prenizak capacity ili schedule konflikt
vraćaju 409. DTO-ovi su eksplicitni; EF entiteti i ownership polja ne izlaze iz API-ja.

## Frontend, navigacija i stanja

`/students/groups/:groupId/edit` dostupan je preko **Uredi** na odabranoj grupi. Canonical
četverostupčani desktop prikaz sadrži osnovne podatke, način rada/capacity, raspored i
sažetak, a ispod članove i neutralne buduće zone. Mobile prikaz slaže panele okomito bez
horizontalnog overflowa. Loading/error/retry, server validation, single-flight save,
uspješna potvrda i potpuni SPA/refresh dirty-navigation gate su vidljivi.

Nakon uspješnog spremanja korisnik se vraća na odabranu grupu u `/students/groups` i vidi
potvrdu. Membership write se ne predstavlja kao dio glavnog Savea. Materijali, ciljevi,
Knowledge/readiness, bilješke i postavke privatnosti ne spremaju inertne podatke.

## Namjerne granice

- Canonical akcija brisanja nije hard delete: retention/audit foundation to ne dopušta.
- Archive UI ostaje zaključan jer lifecycle grupe s postojećim rasporedom još nema
  zaključene posljedice; gumb je disabled s objašnjenjem, bez lažnog writea.
- Location CRUD, arbitrary recurrence, overnight termini, conflict override i Phase 4
  replenishment nisu dio 3.7.
- Browser/OS određuje prikaz native date/time kontrola; sažetak koristi 24-satno vrijeme.
- Učenikovi Knowledge/Evidence rezultati ne mijenjaju se uređivanjem grupe.

[Visual acceptance i dokazi](visual-acceptance/phase-3.7/README.md).
