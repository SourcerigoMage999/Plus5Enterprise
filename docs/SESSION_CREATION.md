# Session creation contract

## Status

**Phase 4.3 implementation contract — 2026-09-12.**

Ovaj dokument zaključava write granicu za Screen 3.3 Novi termin. Primjenjuje
`SCHEDULING_FOUNDATION.md`, ADR-0012 i ADR-0015 te source-spec `3.3_Novi_termin.md` bez
uvođenja kasnijih notification/delivery modela. Phase 4.4 edit/cancel naknadno je zaključan u
`SESSION_EDITING.md`; ne mijenja ovaj create contract.

## Poslovni opseg

Teacher može stvoriti:

- jedan dodatni konkretni termin za postojeću aktivnu grupu
- jedan konkretni termin za aktivnog individualnog učenika
- tjednu individualnu seriju, sa završnim datumom ili bez njega

Redoviti raspored grupe ostaje canonical dio Group create/edit workflowa. Screen 3.3 zato ne
stvara grupnu recurrence seriju: za grupu uvijek nastaje samo dodatni konkretni Session, a UI
upućuje na uređivanje grupe kada postoji redoviti raspored.

Dupliciranje s `/schedule/:sessionId` otvara `/schedule/new?duplicate=:sessionId` i unaprijed
popunjava stvarne Session podatke. Ono ne klonira izvornu seriju niti automatski uključuje
ponavljanje; spremanje uvijek prolazi isti create contract.

## Ulaz i validacija

Obavezni su način rada, owner-scoped Group ili Student, lokalni datum, početak i završetak.
Vrijedi `EndsAt > StartsAt`; početak ne smije biti u prošlosti. Naziv do 200 i napomena do
2.000 znakova su opcionalni te se trimaju. Prazna vrijednost sprema se kao `null`.

Lokacija može biti:

- bez lokacije
- postojeća aktivna fizička lokacija istog Teachera
- potpuna HTTPS poveznica za online sat

Fizička i online lokacija međusobno su isključive. Nepoznat/cross-owner kontekst ili lokacija
vraća privacy-preserving not-found rezultat. Neaktivna ili arhivirana grupa/učenik nije
dostupan za novi termin.

## Lokalno vrijeme i recurrence

Datum i vrijeme tumače se isključivo u `Europe/Zagreb`, a u Session se spremaju UTC granice i
timezone ID. Nevažeće ili dvosmisleno DST lokalno vrijeme odbija se; sustav ne nagađa offset.

Individualna tjedna recurrence sprema canonical `RecurringSessionSeries` vrste
`IndividualRecurrence` i atomski materijalizira početni horizont od 12 tjedana. `EndsOn` je
opcionalan; kada je raniji, uključivo skraćuje horizont. Beskonačno generiranje, replenishment,
mjesečna/arbitrary recurrence i overnight termini nisu dio 4.3.

## Atomski write i konflikti

`POST /api/v1/schedule` radi u Serializable transakciji. Za jednokratni termin sprema jedan
Session; za recurrence sprema seriju i sve početne Session instance ili ništa. Prije commita
ponovno provjerava:

- autentificiranog Teachera i ownership konteksta/lokacije
- valjanost i dostupnost konteksta
- Teacher intervalni konflikt
- konflikt fizičke lokacije
- konflikt prema već materijaliziranim Sessionima i relevantnim recurrence pravilima
- concurrent write rezultat

Conflict blokira write. Nema `Save anyway`, djelomičnog spremanja ni client-only uspjeha.

## API contract

```http
POST /api/v1/schedule
X-CSRF-TOKEN: <token>
Content-Type: application/json
```

Request koristi `deliveryMode`, `contextId`, `title`, `notes`, `date`, `startsAt`, `endsAt`,
`repeatWeekly`, `endsOn`, `locationId` i `onlineMeetingUrl`. Uspjeh vraća `201 Created`,
`Location: /api/v1/schedule/{id}` te prvi `id` i `sessionCount`.

Neuspjesi koriste standardni Problem Details contract i stabilne kodove:
`invalid_request`, `schedule_context_not_found`, `schedule_context_unavailable`,
`schedule_conflict`, `invalid_local_time` i `concurrency_conflict`. Endpoint je Teacher-only,
CSRF-protected i ne prima Teacher ID od klijenta.

## Frontend i navigation contract

`/schedule/new` je stvarni Screen 3.3. Datum se može prenijeti kroz `?date=YYYY-MM-DD`, a
dupliciranje kroz `?duplicate=<sessionId>`. Context picker ponovno koristi bounded, paginirane
Group/Student upite; fizička lokacija koristi owner-scoped katalog. Uspješan create zamjenjuje
route novim `/schedule/:sessionId` detaljem.

Obrazac ima loading, empty, error/retry, inline validation, saving i unsaved-navigation zaštitu.
Sažetak odražava isključivo trenutni unos. Boja, podsjetnici i automatske obavijesti ostaju
vidljivi, ali stvarno disabled jer pripadaju otvorenim kasnijim contractima.

## Visual acceptance

Canonical `3.3 Novi termin.png` obavezan je vizualni izvor uz tekstualni spec. Desktop čuva
tri stupca: osnovni podaci, datum/vrijeme i sažetak; donji red čine lokacija, dodatne postavke i
obavijesti. Mobile sadržaj slaže u jedan stupac bez document overflowa. Prihvaćeni DS-001 shell,
logo, žuti Raspored, profil i notification prostor ostaju globalni contract.

Namjerno odstupanje od PNG-a je onemogućena grupna recurrence: tekstualni source izričito
razlikuje konkretni dodatni termin od redovitog rasporeda grupe, a zaključani scheduling model
ne dopušta drugu canonical kopiju grupnog rasporeda.

## Izvan Phase 4.3

Edit/cancel, `this occurrence`/`future series` promjene, replenishment, reminders,
notifications, attendance, LessonPlan, live lesson, Evidence, Homework i Material writeovi.
