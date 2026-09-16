# Nova grupa — Phase 3.6 / Screen 2.8

## Status
**Implementirano — završni SA review. 2026-09-11.** Pripremni dio ranije APPROVED;
write pravila zaključana su u ADR-0015. Nema automatskog otvaranja 3.7 ili commita/pusha.

Izvori: [2.8 Nova grupa](source_specs/2.8_Nova_grupa.md), canonical teacher PNG 2.8,
[Group foundation](GROUP_FOUNDATION.md), [Scheduling foundation](SCHEDULING_FOUNDATION.md),
[DS alignment](DESIGN_SYSTEM_ALIGNMENT.md) i obavezni engineering standardi.

## Zaključani create contract
- Nova grupa je Active; 0 članova je legitimno. Broj članova ne mijenja status.
- Capacity > 0; nema MinimumStudents ni automatske deaktivacije (rejected behavior).
- Naziv je triman, do 160 znakova, unique normalized name po Teacheru; opis opcionalan do 1000.
- Program se bira iz postojećih vlastitih programa, SchoolGrade iz postojećeg kataloga.
- Početni članovi su vlastiti nearhivirani učenici bez aktivne grupe. Grade jednakost nije
  uvjet članstva. Pri spremanju preuzimaju Group program i DeliveryMode.Group.
- Raspored je opcionalan. Bez slotova spremaju se samo grupa i eventualna članstva.
- S rasporedom je StartsOn obavezan, EndsOn opcionalan i inclusive. Nema limita 12 mjeseci.
- Svaki slot ima dan i početak/završetak u minutama istoga dana, u Europe/Zagreb.
  Lokacija je opcionalan vlastiti nearhivirani Location; nema location CRUD-a.
- Grupa, 0..N članstva, 0..N canonical Series i početni Sessioni spremaju se atomski.

## Bounded materialization i konflikti
Početni prozor je 84 lokalna dana od max(danas u Europe/Zagreb, StartsOn).
Gornja granica je exclusive; raniji inclusive EndsOn skraćuje prozor. Već započeti
termini ne generiraju se. Budući StartsOn ima vlastiti početni prozor, bez ograničenja
cijelog životnog vijeka serije. Null EndsOn ostaje null, nije sentinel datum.

Invalidno/dvosmisleno lokalno DST vrijeme prekida save. Dodirivanje rubova termina nije
preklapanje. Vlastiti slotovi ne smiju se međusobno preklapati. Provjeravaju se stvarni
neotkazani Teacher/Location Session intervali te canonical pravila u početnom prozoru
koja još nemaju materijaliziranu instancu. Postojeći occurrence, uključujući cancelled ili
rescheduled exception, autoritet je nad svojim pravilom. Različite zone postojećih
serija prevode se u UTC. Čitanje pravila je u batchovima od 100.

Sve reference, aktivna članstva, capacity, Student rowversion i overlap provjere su
unutar iste kratke Serializable transakcije kao write. SQL unique indeksi i rowversion
dodatno arbitriraju race; deadlock/concurrency/unique race vraća kontrolirani 409, bez
automatskog retryja ili djelomičnog commita. Nema Save anyway.

Phase 4.6 replenishment mora ponovno provjeriti DST/konflikte pri širenju prozora.
Početni save ne jamči da su svi beskonačni budući termini generirani ili nekonfliktni.
Nema background workera u ovoj fazi.

## API
Svi endpointi su Teacher-only; owner dolazi iz sesije. Tuđi/nedostupni ID je 404.

| Putanja | Contract |
|---|---|
| GET /api/v1/groups/create-candidates | programId, schoolGradeId obavezni; search max 100; page >=1, pageSize 1–100 (default 25, UI 8) |
| GET /api/v1/groups/create-locations | page >=1, search max 100; stranica 25, owner-scoped |
| POST /api/v1/groups | CSRF; name, programId, schoolGradeId, capacity, description, members, slots, startsOn, endsOn, locationId |

Members nose studentId i Base64 Student rowVersion (8 bajtova). Slotovi nose dayOfWeek
(0 nedjelja – 6 subota), start/end. Tehnički limit jednog create zahtjeva je 100 početnih
članova i 14 slotova; Capacity nije time ograničen. Za veće članstvo koristi se postojeći
membership workflow nakon createa. Server je autoritet, ne frontend count.

Uspjeh: 201, Location header i { id, sessionCount }. Nevaljano/CSRF/DST: 400;
bez sesije 401, pogrešna rola 403, nedostupna referenca 404; dupli naziv, stale/membership
ili schedule conflict 409. Response DTO-ovi su eksplicitni, bez EF entiteta i osjetljivih
podataka. Katalozi programa/razreda ponovno koriste students/create-options.

Kandidati: vlastiti nearhivirani učenici bez aktivnog članstva; sort razred, program,
prezime, ime, ID. Ostali razredi ostaju dostupni. DTO sadrži ID, ime/prezime, razred,
naziv programa, podudaranje i rowversion. Read je AsNoTracking, s long offset zaštitom.

## Frontend i navigacija
/students/groups/new je dostupan preko + Nova grupa. Obrazac ima live sažetak,
paginaciju/pretragu, lokalni odabir kroz stranice i uklanjanje skrivenih odabira.
Loading/error/retry/empty, pending i server konflikt vidljivi su bez gubitka obrasca.
Single-flight ref i disabled forma sprječavaju dvostruki submit. Nakon uspjeha navigacija
otvara /students/groups?group={id} i prikazuje potvrdu s nazivom nove grupe.

Postojeći React Router koristi data-router adapter za useBlocker: interne poveznice,
Back/Forward traže potvrdu, native modal drži fokus i Escape ostavlja obrazac.
Refresh/zatvaranje koristi beforeunload. Spremanje onemogućuje potvrdu napuštanja;
uspješan save i obavezni auth redirecti ne ostaju blokirani. Nema localStorage/draft
persistencije niti novog frontend state frameworka.

## Migracija
AllowOpenEndedGroupSeries mijenja samo Series.EndsOn u nullable i read model prihvaća
otvorene serije. Nema backfilla ni izmjene starih datuma. Down eksplicitno odbija rollback
dok postoje null EndsOn retci; ne upisuje izmišljeni završni datum. SQL test pokriva
upgrade s prethodne migracije i taj guard; postojeći runtime suite pokriva clean create.

## Scope i vizualne iznimke
Sačuvani su canonical trostupčani gornji dio, osnovni podaci, kapacitet/lokacija,
raspored, donji odabir učenika, desna summary zona, plava glavna akcija, žuti aktivni
Učenici i prihvaćeni 204 px shell/logo. DS-001 je baza; usporedba nije pixel-perfect.

SA odluke nadjačavaju PNG minimum/auto-deactivation i završni datum. Opis je 1000 prema
foundationu, ne PNG brojaču 200. Način rada je informativno Grupa prema tekstualnom
sourceu. Nema boje grupe, Knowledge procjena, fotografija, zasebnih notes/goals/materials
upisa ili privacy/sharing kontrola; neutralne zone ostaju iz odobrenog pripremnog dijela.
Native date/time rendering ovisi o browser/OS localeu; sažetak prikazuje 24-satno vrijeme.

[Visual acceptance i dokazi](visual-acceptance/phase-3.6/README.md).
