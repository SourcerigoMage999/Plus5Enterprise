# Scheduling and session foundation

## Status

**LOCKED foundation v1.0 — Phase 2.4 — 2026-08-28**

Ovaj dokument formalizira recurrence gate iz source specifikacija 3.3/3.4 i definira najmanji trajni model rasporeda. Ne uvodi feature API/UI, attendance, izvedeni sat, podsjetnike, obavijesti, pripremu sata ni Knowledge Model.

## Canonical granice

- `Session` je jedan konkretan kalendarski termin s UTC početkom i završetkom. Nije Group, redoviti raspored, priprema ni zapis onoga što se stvarno održalo.
- `RecurringSessionSeries` je verzionirana tjedna recurrence definicija i identitet skupa konkretnih Session instanci.
- Serija vrste `RegularGroupSchedule` jest canonical redoviti raspored grupe; nema druge kopije rasporeda na Group zapisu. Grupa može imati više serija, primjerice utorak i četvrtak.
- Serija vrste `IndividualRecurrence` pripada jednom Studentu. Dodatni jednokratni grupni ili individualni termin nema series vezu.
- Svaka generirana instanca ima jedinstvenu `(RecurringSessionSeriesId, SeriesOccurrenceDate)` kombinaciju. Generiranje je idempotentno i radi u ograničenom budućem horizontu koji application faza mora eksplicitno odabrati.
- “Samo ovaj termin” mijenja konkretni Session i označava ga kao series exception. Ne mijenja seriju ni druge instance.
- “Svi budući termini” ne prepisuje postojeću seriju: stara dobiva kraći `EndsOn` i `SupersededAtUtc`, a nova serija dobiva `PreviousSeriesId`. Buduće neodržane instance se u istoj transakciji zamjenjuju ili premještaju na novu seriju; povijesne, započete, održane i otkazane instance ostaju netaknute.
- Promjena dana, dodavanje/uklanjanje tjednog slota ili strukture redovitog rasporeda ide kroz budući Group workflow; jednostavan future-series time/location pomak može koristiti isti versioning contract.

## Vrijeme i DST

- Session čuva `StartsAtUtc` i `EndsAtUtc` kao apsolutne trenutke te `TimeZoneId` za prikaz izvornog lokalnog konteksta.
- Series čuva lokalni dan/timeslot, IANA/TimeZoneInfo identifikator i inclusive `StartsOn`/`EndsOn` datume. Ne sprema izvedeno trajanje.
- Overnight slot nije podržan u foundationu; `LocalEndTime` mora biti nakon `LocalStartTime` istoga dana.
- Generator ne smije tiho pomaknuti invalidno ili dvosmisleno lokalno vrijeme na DST prijelazu. Takva pojava prekida preview/save i traži eksplicitnu korisničku korekciju prije stvaranja UTC instance.

## Kontekst i lokacija

- Svaki Session i Series je Teacher-owned i ima točno jedan context: Group ili Student, usklađen s `DeliveryMode`/series vrstom.
- Composite FK-ovi fizički odbijaju Group, Student, Location ili Series drugog Teachera. Application transakcija dodatno potvrđuje da Session i odabrana Series imaju isti context.
- `Location` je Teacher-owned ponovno upotrebljiva fizička/druga lokacija s jedinstvenim normaliziranim nazivom. Arhiviranje ne briše povijesne Session veze.
- Session/Series može imati Location, HTTPS online meeting URL ili nijedno; ne oba istodobno. Raw videopoziv credential ili secret ne sprema se.

## Session status

- statusi su `Scheduled`, `InProgress`, `Held`, `Cancelled`
- dopušteni prijelazi: Scheduled → InProgress → Held; Scheduled/InProgress → Cancelled
- Held i Cancelled su terminalni; otkazivanje čuva redak i `CancelledAtUtc`
- stvarni početak/završetak, attendance, aktivnosti i rezultat održanog sata nisu Session polja; pripadaju zasebnom delivery/evidence modelu

## Conflict i concurrency contract

- konflikt postoji za neotkazane intervale kada je `candidate.Start < existing.End && candidate.End > existing.Start`; dodirivanje rubova nije preklapanje
- Teacher ne može imati dva preklapajuća termina; ista Location također se provjerava za preklapanje kada budući permissions dopuste dijeljene resurse
- SQL Server nema portable exclusion constraint za intervale. Budući create/reschedule/series-generation use case mora koristiti kratku `Serializable` transakciju, iste indeksirane overlap upite i ponovnu provjeru prije commita
- `rowversion` na Sessionu i Seriesu sprječava lost update postojećih redaka; unique series-occurrence indeks sprječava dvostruko generiranje
- conflict nikada nije odluka clienta i ne zaobilazi se samo potvrdom upozorenja bez buduće eksplicitne produktne odluke

## Persistence

- tablice: `Locations`, `RecurringSessionSeries`, `Sessions`
- svi business FK-ovi koriste `Restrict`; nema cascade ili hard deletea
- CHECK constrainti štite context XOR, enum vrijednosti, intervale, cancellation, paired series occurrence i physical/online location izbor
- indeksi pokrivaju Teacher/time/status kalendar i overlap query, Location overlap, Group/Student history, aktivne series slotove i jedinstvenu occurrence instancu
- nema seeda, backfilla ni automatskog stvaranja Session redaka u migraciji

## Izvan Phase 2.4

- Schedule/Session application use caseovi, API, calendar UI i conflict override
- recurrence preview horizon, background replenishment i notification dispatch
- boja, podsjetnici i poruke učenicima/Guardianima
- attendance, stvarni held-lesson zapis, plan/materijali/domaća zadaća
- shared/multi-Teacher room permissions i resource booking
- arbitrary RRULE, mjesečno ponavljanje, overnight i više termina u jednom series retku
