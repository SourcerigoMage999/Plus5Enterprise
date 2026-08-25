# Group and membership foundation

## Status

**LOCKED foundation v1.0 — Phase 2.3 — 2026-08-25**

Ovaj dokument definira najmanji trajni Group i GroupMembership contract potreban za buduće ekrane 2.7, 2.8 i 2.9. Ne uvodi feature API/UI, raspored, Session, materijale, ciljeve, bilješke niti Knowledge Model podatke.

## Zaključane granice

- `Group` je Teacher-owned trajna organizacijska jedinica za grupnu nastavu; nije Program ni raspored.
- Svaka Group pripada točno jednom Programu istog Teachera i jednom `SchoolGrade` zapisu.
- Group SchoolGrade opisuje ciljanu organizacijsku školsku godinu. Student drugog SchoolGradea može biti ponuđen niže na listi, ali Teacher zadržava konačnu odluku; grade jednakost zato nije DB invarijanta članstva.
- `GroupStatus` je `Active`, `OnHold` ili `Inactive`.
- Arhiviranje postavlja `Inactive` i dopušteno je tek kada nema aktivnih članstava. Product workflow ne radi hard delete.
- `GroupCapacity` je pozitivan maksimalni broj aktivnih članova. Ne može se smanjiti ispod trenutačnog broja aktivnih članova.
- Svaki membership write mora u istoj DB transakciji provjeriti aktivni count, promijeniti članstvo i ažurirati Group. Group `rowversion` pretvara paralelni write u kontrolirani concurrency conflict umjesto tihog prekoračenja kapaciteta.
- `GroupMembership` je vremenski valjana veza s `JoinedAtUtc` i opcionalnim `LeftAtUtc`; završeni redak se ne prepisuje niti briše radi ponovnog ulaska.
- Student može imati najviše jedno aktivno GroupMembership članstvo ukupno, ali može imati više povijesnih članstava i ponovno pristupiti istoj grupi u novom razdoblju.
- Group i Student u članstvu moraju pripadati istom Teacheru. Mirrored `TeacherAccountId` u junction tablici služi isključivo tenant/ownership FK zaštiti; canonical owner ostaje na Group i Student zapisima.
- Pri ulasku u grupu Studentov Program postaje Program grupe, a `DeliveryMode` postaje `Group`, atomarno s novim aktivnim članstvom.
- Pri izlasku bez neposrednog transfera aktivno članstvo završava, Program ostaje, a `DeliveryMode` postaje `Individual` u istoj transakciji.
- Transfer završava staro i stvara novo članstvo te ostavlja `DeliveryMode.Group`; oba članstva i Student organizacija spremaju se atomarno.

## Group

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `TeacherAccountId` | obavezni ownership FK na Teacher `UserAccount` |
| `ProgramId` | obavezni composite same-Teacher FK na Program |
| `SchoolGradeId` | obavezni FK na globalni SchoolGrade |
| `Name` | obavezni trimani naziv, najviše 160 znakova |
| `NormalizedName` | invariant uppercase oblik; jedinstven po Teacheru |
| `Description` | opcionalni kratki opis, najviše 1000 znakova |
| `Capacity` | obavezni cijeli broj veći od nule |
| `Status` | obavezni `Active`, `OnHold` ili `Inactive` |
| `CreatedAtUtc` | obavezni UTC trenutak stvaranja |
| `UpdatedAtUtc` | obavezni UTC trenutak zadnje promjene |
| `ArchivedAtUtc` | opcionalni UTC trenutak arhiviranja; zahtijeva `Inactive` |
| `RowVersion` | SQL Server optimistic-concurrency token |

Jedan Teacher ne može imati dvije grupe istog normaliziranog naziva. Program se na Group ekranu samo bira; ne stvara se niti definira.

## GroupMembership

| Polje | Pravilo |
|---|---|
| `Id` | stabilni GUID |
| `TeacherAccountId` | ownership scope, mora odgovarati i Group i Student owneru |
| `GroupId` | obavezni composite same-Teacher FK na Group |
| `StudentId` | obavezni composite same-Teacher FK na Student |
| `JoinedAtUtc` | obavezni UTC početak članstva |
| `LeftAtUtc` | opcionalni UTC završetak; mora biti isti ili kasniji od početka |

`LeftAtUtc IS NULL` znači aktivno članstvo. Filtered unique indeks na Studentu jamči najviše jednu aktivnu grupu. Jedinstvena Group/Student/Joined kombinacija sprječava duplikat istog povijesnog intervala.

## Capacity i concurrency contract

CHECK constraint može jamčiti samo da je Group capacity pozitivan; SQL CHECK ne može sigurno brojiti retke druge tablice. Zato budući application use case za add/remove/transfer mora:

1. učitati Group s `rowversionom` i aktivni membership count
2. provjeriti status, arhiviranje i capacity
3. atomarno ažurirati Student Program/DeliveryMode i membership retke
4. pozvati Group membership-change prijelaz kako bi se Group redak ažurirao
5. spremiti sve u jednoj transakciji
6. mapirati `DbUpdateConcurrencyException` u kontrolirani conflict rezultat

Isti contract vrijedi za smanjenje kapaciteta i arhiviranje. Direktni client count ili Teacher ID nikada nisu autoritet.

## Persistence contract

- tablice su `Groups` i `GroupMemberships`
- svi tekstovi koriste bounded `nvarchar(n)`, a trajni trenuci `datetimeoffset(7)`/UTC application contract
- Group ima restriktivne FK-ove prema UserAccountu, same-Teacher Programu i SchoolGradeu
- GroupMembership ima dva restriktivna composite same-Teacher FK-a prema Group i Student zapisima
- nema cascade deletea; završetak članstva zapisuje `LeftAtUtc`
- CHECK constrainti štite Group capacity/status/archive i membership vremensku valjanost
- indeksi pokrivaju Teacher list/filter, Program/SchoolGrade lookup, aktivne članove grupe, jedino aktivno članstvo Studenta i povijesni interval
- nema seed/backfill redaka niti izmjene postojećih Student/Program vrijednosti

## Security

- budući API izvodi Teacher ID iz autentificirane sesije i provjerava object-level ownership za Group, Student i membership
- client-supplied Teacher ID, active count, capacity availability ili ownership nisu autoritet
- composite ownership FK-ovi fizički odbijaju cross-Teacher članstvo i cross-Teacher Program vezu
- list endpointi moraju biti bounded/paginirani; archived grupe nisu dio uobičajenog aktivnog prikaza
- Group nema Student/Guardian PII kopije; članstvo referencira canonical Student zapis

## Dokumentacijski gateovi

- Promjena Programa grupe s aktivnim članovima nije implementirana dok vlasnik proizvoda ne odluči mijenjaju li se svi aktivni Student Programi, prekidaju članstva ili se promjena odbija.
- `RegularGroupSchedule`, termini, trajanje, lokacija i conflict detection pripadaju Phase 2.4 i ne spremaju se u Group foundation.
- Minimalni broj članova, draft grupe i pravo fizičko brisanje nisu zaključani.

## Izvan Phase 2.3

- Group CRUD/application use caseovi, API contracti, pretraživanje i UI
- redoviti raspored, Session instance, recurrence, lokacija i trajanje
- materijali grupe, LearningGoal/ciljevi i bilješke
- ProficiencyLevel target/estimate i svi mastery/readiness/evidence podaci
- attendance, progress i izvedene organizacijske statistike
- hard-delete, retention/erasure i pravi audit trail
