# DATABASE_DESIGN_STANDARD

## Status

**MANDATORY v1.0 — 2026-08-23**

Ovaj dokument je obavezan za svaku PLUS 5 fazu koja uvodi ili mijenja trajne podatke.

## 1. Cilj

Baza mora biti:

- konzistentna
- normalizirana
- sigurna
- migrabilna
- dovoljno učinkovita za cilj 10.000+ korisnika
- razumljiva sljedećem senior developeru/AI-u bez oslanjanja na skriveni kontekst

## 2. Normalizacija

### Default: Treća normalna forma (3NF)

Svaka nova OLTP tablica mora defaultno zadovoljiti 1NF, 2NF i 3NF.

Praktično:

- jedna vrijednost po stupcu; bez CSV/JSON liste ID-eva kao zamjene za relaciju
- many-to-many veze modelirati junction tablicom
- podatak se ne duplicira u više tablica ako predstavlja istu canonical činjenicu
- ne spremati izvedene vrijednosti koje se pouzdano mogu izračunati, osim kada postoji dokumentiran performance/business razlog
- atribut pripada entitetu o kojem funkcionalno ovisi

### Denormalizacija

Dopuštena je samo kada:

1. postoji izmjeren problem ili jasan read-model zahtjev
2. definiran je canonical source
3. definirana je strategija sinkronizacije
4. postoje testovi konzistentnosti
5. odluka je dokumentirana; veća trajna odluka ide u ADR

Denormalizacija “za svaki slučaj” je zabranjena.

## 3. Modeliranje domene

- tablice i stupci moraju koristiti canonical nazive iz domain glossaryja
- jedan pojam ne smije imati različita imena kroz DB/API/backend bez jasnog mapping razloga
- relationship cardinality mora biti eksplicitna
- optional relationship mora biti stvarno poslovno optional, ne samo tehnički nullable
- junction tablica mora imati unique/composite constraint koji onemogućava duplikat relacije

## 4. Primarni ključevi

- svaka trajna aggregate/entity tablica ima stabilan primarni ključ
- GUID/UUID je dopušten i preferiran kada ID putuje kroz granice sustava
- slučajni GUID ne smije nekritički postati clustered hot-path ključ ako uzrokuje fragmentaciju
- strategija fizičkog clustered indeksa mora biti svjesna query/write patterna
- natural key se dodatno štiti `UNIQUE` constraintom kada predstavlja poslovnu jedinstvenost

AI ne smije mijenjati ID strategiju postojećeg modula bez migration plana.

## 5. Data types

SQL Server pravila:

- Unicode tekst: `nvarchar(n)` s realnim maksimalnim `n`; `nvarchar(max)` samo kada je opravdano
- datum/vrijeme: `datetime2`; u aplikaciji se trajni trenutci tretiraju kao UTC
- datum bez vremena: `date`
- novac/precizni decimalni izračuni: `decimal(p,s)` s eksplicitnom preciznošću; nikada `float`
- bool: `bit`
- binarni sadržaj velikih datoteka ne stavljati u glavnu OLTP bazu bez posebne arhitekturne odluke

Maksimalne duljine moraju biti usklađene s validacijom na API razini.

## 6. Nullability

- `NOT NULL` je default kada poslovni model zahtijeva vrijednost
- `NULL` znači “vrijednost može legitimno ne postojati”, a ne “nismo odlučili”
- ne koristiti magic vrijednosti (`0`, `-1`, prazan string, `1900-01-01`) kao zamjenu za null/business state

## 7. Referential integrity

- veze moraju imati stvarne foreign key constraintove osim ako postoji dokumentiran razlog protiv
- delete behavior mora biti eksplicitno konfiguriran
- `CASCADE DELETE` nije default za poslovno važne podatke
- brisanje roditelja ne smije slučajno izbrisati audit/evidence/history podatke
- soft delete uvodi se samo kada business/retention zahtjev to traži; nije univerzalni pattern

## 8. Constraints

Baza mora čuvati integritet, ne samo aplikacija.

Koristi gdje je primjenjivo:

- primary key
- foreign key
- unique constraint/index
- check constraint za stabilne DB-invariante
- required/not-null constraint

Aplikacijska validacija nije zamjena za DB constraint kod konkurentnih requestova.

## 9. Concurrency

Kada dva korisnika/procesa realno mogu uređivati isti zapis:

- optimistic concurrency je default
- koristiti SQL Server `rowversion`/EF concurrency token kada je potrebno
- conflict se mora pretvoriti u kontrolirani application/API rezultat
- lost update se ne smije tiho dogoditi

Ne dodavati `rowversion` svakoj tablici bez use-casea.

## 10. Audit vremena

Za poslovno relevantne entitete razmotriti:

- `CreatedAtUtc`
- `UpdatedAtUtc`

Audit polja ne uvoditi mehanički u čiste lookup/junction tablice bez vrijednosti.

Ako postoji zahtjev za pravim audit trailom, to je zaseban model; `UpdatedAtUtc` nije audit trail.

## 11. Indeksi

Indeks mora odgovarati query patternu.

Obavezno reviewati:

- FK stupce koji se često joinaju/filtriraju
- unique business keyeve
- list/search endpoint filter + sort kombinacije
- indekse za status/date work queue upite

Zabranjeno:

- indeksirati svaki stupac
- stvarati redundantne indekse
- dodavati indeks bez razumijevanja write troška

Kod performance problema koristiti execution plan i mjerenje, ne nagađanje.

## 12. Query pravila

- nema nekontroliranog `SELECT *` u hot pathu
- read endpointi trebaju projektirati samo potrebna polja
- izbjeći N+1 query problem
- potencijalno velike liste moraju imati pagination
- server-side filter/sort za velike kolekcije
- `AsNoTracking`/ekvivalent koristiti za EF read-only queryje gdje je primjenjivo
- ograničiti maksimalni page size

## 13. Transactions

- transakcija obuhvaća najmanju potrebnu atomsku poslovnu operaciju
- ne držati DB transakciju otvorenom preko poziva prema emailu, storageu, AI-u ili drugom mrežnom provideru
- više promjena koje moraju biti atomske spremaju se u istoj transakciji
- za pouzdanu integraciju s vanjskim async sustavima koristiti outbox kada faza to stvarno zahtijeva

## 14. Migracije

EF Core migrations su source of truth za schema evolution.

Svaka migration promjena mora:

1. imati smislen naziv
2. biti pregledana kao SQL/schema promjena, ne samo C# diff
3. biti reproducibilna od clean baze
4. imati provjeru upgrade patha kada mijenja postojeće podatke
5. ne sadržavati destruktivan gubitak podataka bez eksplicitnog plana

Produkcijski deployment ne smije ovisiti o ručnom klikanju u DB GUI-u.

### Production migration pravilo

Migracije se izvršavaju kao kontrolirani deployment korak. Više API instanci ne smije se utrkivati u automatskom pokretanju migracija na startupu.

## 15. Seed podaci

- seed samo za stabilne sistemske podatke ili dev/test podatke u odgovarajućem environmentu
- produkcijski business podaci se ne seedaju kroz migraciju bez razloga
- seed mora biti idempotentan/determinističan gdje je potrebno

## 16. Sensitive data

- lozinke se nikada ne spremaju u plaintextu
- tokeni/secret vrijednosti spremaju se hashirano ili šifrirano prema threat modelu
- minimizirati PII
- ne spremati osjetljive podatke koji nisu potrebni poslovnom zahtjevu
- retention/deletion pravila moraju se definirati prije modula koji ih zahtijevaju

## 17. JSON u SQL Serveru

JSON stupac je dopušten za fleksibilni payload samo kada sadržaj nije core relational business model.

Nije dopušteno koristiti JSON da bi se izbjeglo pravilno modeliranje Student, Group, Session, Knowledge Component, Evidence Event ili druge ključne domene.

## 18. DB review checklist za svaku fazu

Prije approvala reviewer mora odgovoriti:

- Je li model u 3NF ili je denormalizacija opravdana?
- Jesu li cardinality i nullability ispravni?
- Postoje li potrebni PK/FK/UNIQUE/CHECK constrainti?
- Jesu li delete pravila sigurna?
- Jesu li tipovi i duljine razumni?
- Postoji li concurrency rizik?
- Jesu li query patterni indeksirani bez over-indexinga?
- Postoji li migration i rollback/data migration rizik?
- Postoji li PII/security rizik?
- Je li model ograničen na trenutnu ROADMAP fazu?
