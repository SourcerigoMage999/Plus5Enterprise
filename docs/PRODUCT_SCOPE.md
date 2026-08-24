# PRODUCT_SCOPE

## Proizvod

PLUS 5 je aplikacija za organizaciju i izvođenje instrukcija/nastave, s naglaskom na rad učitelja s učenicima, rasporedom, nastavnim materijalima, pripremom sata, interaktivnim radom, domaćim zadaćama, komunikacijom, izvještajima i praćenjem znanja.

## Glavni korisnički kontekst iz dostavljene dokumentacije

Trenutačno je dokumentiran prvenstveno **Teacher application**. Stalna glavna navigacija uključuje:

- Radni stol
- Učenici
- Raspored
- Materijali
- Priprema sata
- PLUS 5 Ploča
- Domaće zadaće
- Poruke
- Izvještaji
- Financije
- Postavke

Dodatno postoje Centar obavijesti i korisnički/auth ekrani.

## Authentication scope

Za trenutni implementacijski scope:

- `Teacher` je jedini akter s korisničkim računom i interaktivnim pristupom aplikaciji
- Teacher se javno samostalno registrira te potvrđuje e-mail prije punog pristupa
- `Student` i `Guardian` nemaju korisnički račun niti login u Phase 1.6
- `Administrator` role/account nije dio trenutnog product scopea
- budući Student/Guardian accounti nisu zabranjeni, ali zahtijevaju zasebnu dokumentacijsku odluku prije implementacije

Detaljni business contract je u `AUTHENTICATION_REQUIREMENTS.md`.

## Ključni poslovni koncepti koji se već vide u specifikaciji

- Učitelj
- Učenik
- Roditelj/skrbnik
- Grupa
- Program
- Razred / razina
- Termin / sat
- Materijal
- Nastavna aktivnost / zadatak
- Kurikulum
- Knowledge Model
- Knowledge Component
- Evidence Event / dokaz znanja
- Procjena spremnosti
- Plan/priprema sata
- Domaća zadaća
- Poruka / razgovor
- Izvještaj
- Financijska stavka
- Obavijest

## Važan princip Knowledge Modela

Specifikacije za 2.4, 2.5 i 4.2 jasno upućuju da procjena znanja ne smije biti nasumična niti ručno upisan postotak. Rezultati aktivnosti trebaju biti povezani s kurikulumom i komponentama znanja, a procjena mora nastajati iz prikupljenih dokaza. Pomoć učeniku i vrsta dokaza također utječu na interpretaciju rezultata.

## Granica trenutačne dokumentacije

Detaljno su opisani:

- 1.1 Radni stol učitelja
- 2.1–2.9 Učenici / Grupe
- 3.1–3.4 Raspored
- 4.2 Pregled materijala
- 4.3 Novi materijal — izrada prezentacije
- logika 2.4 procjene spremnosti i 2.5 detalja znanja

Za 4.1 postoji PNG, ali dostavljeni DOCX je prazan. Za brojne ekrane 4.4–14.x postoji mapa/navigacijski opis, ali ne i potpuna detaljna specifikacija.
