# Recurrence materialization replenishment

## Status

**LOCKED — Phase 4.6 contract — 2026-09-16**

Ovaj dokument zaključava operational contract kojim aktivne open-ended
`RecurringSessionSeries` održavaju konkretne `Session` zapise u rolling 12-week horizontu.
Vrijedi zajedno sa `SCHEDULING_FOUNDATION.md`, ADR-0015, ADR-0017 i Phase 4.5 consistency
invarijantama.

## Ownership i cadence

- Vlasnik je `Plus5.Api` backend `BackgroundService`; nema frontend triggera, SQL Agenta ni
  vanjskog crona.
- Worker pokreće catch-up odmah nakon API starta. Deployment redoslijed mora prethodno
  završiti kontroliranu EF migraciju i učiniti bazu spremnom.
- Nakon startup runa replenishment se pokreće svakih 6 sati.
- Cadence nije javni API niti korisnička postavka.

## Canonical cilj

Svaki run za aktivnu, open-ended i nesuperseded seriju održava konkretne Sessione od
trenutnog lokalnog datuma serije do exclusive granice 84 lokalna dana kasnije. Ne stvara
prošle termine, ne mijenja postojeće Sessione i dodaje samo nedostajuće occurrencee.

Aktivna serija u ovom contractu znači:

- `EndsOn IS NULL`
- `SupersededAtUtc IS NULL`
- pripada aktivnoj, nearhiviranoj Group ili aktivnom, nearhiviranom Student kontekstu.

Postojeći `(RecurringSessionSeriesId, SeriesOccurrenceDate)` redak uvijek je autoritativan,
neovisno o tome je li običan Scheduled occurrence, ručna iznimka, `Cancelled`, `InProgress`
ili `Held`. Worker ga ne regenerira. Superseded serija nikada se ne nastavlja materializirati.

Za individualnu seriju naslov i bilješke novih Sessiona preuzimaju se iz posljednjeg
ne-exception Session predloška serije; recurrence vrijeme, kontekst, lokacija/online link i
zona uvijek dolaze iz canonical serije.

## Bounded processing i transakcije

- Jedan query batch sadrži najviše 50 serija.
- Svaka serija obrađuje se u zasebnoj kratkoj Serializable transakciji.
- Conflict rule lookup radi u stranicama od najviše 100 serija.
- Ne postoji jedna transakcija nad cijelim runom.
- Unique indeks `UX_Sessions_Series_Occurrence` ostaje zadnja zaštita idempotentnosti.

Ponovljeni ili paralelno zatraženi run mora dati isti konačni skup Sessiona kao jedan run.

## Multi-instance lease

Globalni replenishment u jednom trenutku smije izvršavati samo jedna API instanca.
`ScheduleMaterializationLeases` je SQL-backed lease store:

- lease ima owner i UTC expiry
- početni lease traje 15 minuta i obnavlja se prije svake serije
- izgubljen ili istekao lease odmah zaustavlja aktualni run
- druga instanca koja ne dobije lease bilježi `LeaseSkipped` i završava bez greške
- uredan završetak oslobađa lease; pad instance sigurno se oporavlja istekom.

Lease ne zamjenjuje DB unique/transaction zaštite.

## Occurrence validacija

Prije inserta svaki kandidat ponovno provjerava:

- postoji li već konkretni Session ili preserved occurrence datum
- je li serija još open-ended, aktivna i nesuperseded
- invalidno ili ambiguous lokalno vrijeme u zoni serije
- Teacher overlap nad stvarnim Sessionima i još nematerializiranim canonical pravilima
- Location overlap nad istim izvorima kada serija ima Location.

Business conflict ili DST problem blokira samo taj occurrence. Worker ne pomiče termin, ne
mijenja lokaciju, ne otkazuje konfliktni zapis, ne radi override i ne mijenja recurrence
seriju. Ostali occurrencei iste serije i ostale serije nastavljaju se obrađivati.

Problematični occurrence ponovno se evaluira u sljedećem redovnom 6-satnom runu. Kada uvjet
nestane, Session se normalno stvara.

## Durable issue evidence

`ScheduleMaterializationIssues` trajno čuva:

- `Id`
- `RecurringSessionSeriesId`
- `OccurrenceLocalDate`
- `IssueType`
- `FirstSeenAtUtc`
- `LastSeenAtUtc`
- `AttemptCount`
- nullable `ResolvedAtUtc`.

Dozvoljeni tipovi su `TeacherConflict`, `LocationConflict`, `InvalidLocalTime`,
`AmbiguousLocalTime`, `ConcurrencyFailure` i `MaterializationFailure`. Unique identitet je
`(RecurringSessionSeriesId, OccurrenceLocalDate, IssueType)`: ponovljeni problem povećava
brojač i osvježava `LastSeenAtUtc`, a uspješno materializiran ili već autoritativno postojeći
occurrence postavlja `ResolvedAtUtc`. Povijest se ne briše.

Tablica ne sadrži exception message, stack trace, connection string, PII ili sensitive
payload.

## Retry i failure ponašanje

- Business conflict i DST problem nemaju retry loop unutar runa.
- SQL deadlock, timeout i drugi eksplicitno klasificirani tranzijentni kvarovi imaju najviše
  tri pokušaja s exponential backoffom.
- Nakon trećeg concurrency kvara transakcija serije ostaje rollbackana, bilježi se
  `ConcurrencyFailure` gdje je SQL ponovno dostupan i prelazi se dalje.
- Kada je baza nedostupna, aktualni run završava; sljedeći scheduled run ponovno pokušava.
- Neočekivani problem jedne serije bilježi `MaterializationFailure` bez spremanja detalja
  exceptiona u business tablicu i ne smije rušiti zdrave serije.
- Nema beskonačnih retryjeva.

## Structured logging

Svaki run strukturirano bilježi početak, lease acquired/skipped/lost, broj pregledanih serija,
stvorenih Sessiona, preskočenih occurrencea, pronađenih konflikata, zapisanih/riješenih issuea,
trajanje, retryje i konačni failure. Logovi ne sadrže sadržaj bilješki, identitete učenika,
credentials ili connection string.

## Izvan Phase 4.6

- Teacher/Admin UI i javni API za materialization issuee
- ručni run endpoint
- Teacher conflict override / Save anyway
- automatsko pomicanje termina ili promjena recurrence serije
- arbitrary recurrence, overnight termini, reminders i notification delivery
- vanjski scheduler, message broker ili zaseban worker servis.
