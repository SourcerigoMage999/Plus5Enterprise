# Session editing contract

## Status

**Phase 4.4 implementation contract — 2026-09-13.**

Ovaj dokument zaključava write granicu za Screen 3.4 Uredi termin. Primjenjuje
`SCHEDULING_FOUNDATION.md`, ADR-0012, ADR-0013 i source-spec `3.4_Uredi_termin.md` bez
uvođenja notification, reminder, delivery ili Evidence modela.

## Poslovni opseg

Teacher može uređivati vlastiti budući `Scheduled` Session ili ga otkazati bez brisanja.
Način rada i Group/Student kontekst ostaju nepromjenjivi. Promjena može vrijediti:

- samo za odabrani termin — datum, vrijeme, naziv, napomena i lokacija
- za odabrani i sve buduće termine aktivne serije — samo vrijeme i lokacija

`Samo ovaj termin` je zadani i najsigurniji izbor. Promijenjena instanca serije postaje
`IsSeriesException`; canonical recurrence pravilo ostaje nepromijenjeno.

`Svi budući termini` dostupan je samo za neizmijenjenu instancu aktivne serije. Datum, naziv i
napomena tada su zaključani. Jednostavna promjena vremena/lokacije supersedira staru seriju,
stvara successor s `PreviousSeriesId` i materializira njegov početni horizont od 12 tjedana.
Stari budući `Scheduled` Sessioni koji nisu ručne iznimke od efektivnog datuma dobivaju status
`Cancelled`. Postojeće ručne iznimke te započeti, održani, otkazani i raniji zapisi ostaju
sačuvani; successor ne materializira drugi Session za njihove occurrence datume. Nema in-place
izmjene recurrence povijesti niti tihog poništavanja ranije eksplicitne occurrence odluke.

Promjena dana ili strukture redovitog grupnog rasporeda nije Session edit. Ona ide kroz
Screen 2.9 Uredi grupu i njegov versioning contract.

## Ulaz i validacija

Obavezni su lokalni datum, početak, završetak, scope i osmobajtni `RowVersion`. Vrijedi
`EndsAt > StartsAt`; početak ne smije biti u prošlosti. Naziv do 200 i napomena do 2.000
znakova trimaju se, a prazna vrijednost postaje `null`.

Lokacija može ostati prazna, biti aktivna fizička lokacija istog Teachera ili potpuna HTTPS
poveznica. Fizička i online lokacija međusobno su isključive. Lokalno vrijeme tumači se u
`Europe/Zagreb`; nevažeći ili dvosmisleni DST unos odbija se.

Owner-scoped query za nedostajući i tuđi Session vraća isti not-found ishod. `Held` i
`Cancelled` Sessioni nisu dostupni za edit/cancel. Backend je konačni authority za status,
scope, ownership, RowVersion i conflict pravila.

## Atomski write, konflikti i concurrency

Update i cancel rade u `Serializable` transakciji. Save ponovno provjerava Teacher i lokacijski
intervalni konflikt te relevantna recurrence pravila. Odabrana instanca ili buduće instance
stare serije koje transakcija zamjenjuje ne računaju se kao konflikt same sa sobom.

Conflict blokira write. Preview je samo korisnički signal; save uvijek ponavlja autoritativnu
provjeru. Nema `Save anyway`, djelomičnog uspjeha ili client-only promjene. RowVersion i SQL
unique/deadlock ishodi mapiraju se u concurrency konflikt.

Cancel mijenja status samo odabranog Sessiona u `Cancelled`, postavlja vrijeme otkazivanja i
čuva zapis/povijest. Ne briše Session, ne otkazuje cijelu seriju i ne šalje lažnu obavijest.

## API contract

```http
GET  /api/v1/schedule/{sessionId}/edit
PUT  /api/v1/schedule/{sessionId}
POST /api/v1/schedule/{sessionId}/conflicts
POST /api/v1/schedule/{sessionId}/cancel
```

Svi write endpointi su Teacher-only i CSRF-protected. Update i preview primaju `title`,
`notes`, `date`, `startsAt`, `endsAt`, `locationId`, `onlineMeetingUrl`, `scope` i
`rowVersion`. Cancel prima `rowVersion`. Update vraća odredišni `id` i `sessionCount`; kod
future-series promjene `id` pripada odgovarajućoj instanci successor serije.

Neuspjesi koriste Problem Details i stabilne kodove `invalid_request`,
`schedule_context_not_found`, `schedule_session_unavailable`, `schedule_conflict`,
`invalid_local_time` i `concurrency_conflict`.

## Frontend i navigation contract

`/schedule/:sessionId/edit` je stvarni Screen 3.4. Otvara se iz Session detaila, učitava
owner-scoped aktualne podatke i nakon uspješnog updatea ili cancela zamjenjuje route odredišnim
Session detailom. Cancel prethodno traži izričitu potvrdu.

Obrazac ima loading, not-found/error/retry, inline validation, debounced conflict preview,
saving/cancelling i dirty-navigation zaštitu. Conflict isključuje save. Dupliciranje ostaje
eksplicitni create-prefill flow. Boja, reminders i notifications ostaju vidljivi, ali disabled.

## Visual acceptance

Canonical `3.4 Uredi termin.png` obavezan je vizualni izvor uz tekstualni spec. Desktop čuva
tri stupca s osnovnim podacima, vremenom/lokacijom, trenutnim terminom, scopeom, conflictima i
akcijama. Mobile sadržaj slaže u jedan stupac bez document overflowa. Prihvaćeni DS-001 shell,
logo, žuti Raspored, profil i notification prostor ostaju globalni contract.

## Izvan Phase 4.4

Promjena Group/Student konteksta, promjena strukture grupnog rasporeda, cijela-series cancel,
recurrence replenishment, arbitrary/mjesečna recurrence, reminders, notifications,
attendance, LessonPlan, live lesson, Evidence, Homework i Material writeovi.
