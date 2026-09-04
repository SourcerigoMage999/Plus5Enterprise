# PRODUCT_SCOPE

## Proizvod

PLUS 5 je aplikacija za organizaciju i izvođenje instrukcija/nastave, s naglaskom na rad učitelja s učenicima, rasporedom, nastavnim materijalima, pripremom sata, interaktivnim radom, domaćim zadaćama, komunikacijom, izvještajima i praćenjem znanja.

## Glavni korisnički kontekst iz dostavljene dokumentacije

Implementacija je trenutačno **Teacher application**, a source paket od 2026-09-04
dokumentira i buduću **Student application**. Teacher stalna glavna navigacija uključuje:

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

Student source opisuje 44 detaljna ekrana kroz Početnu, self-study, Moje sate, Domaće
zadaće, pomoć/booking, napredak, Teachere, Poruke, Profil i Postavke. To proširuje budući
product scope, ali ne aktivni account scope: Student/Guardian login, minor consent,
permissions, payments i marketplace pravila ostaju blokirana pitanja Phase 17.

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

## Granica trenutačne dokumentacije — osvježeno 2026-09-04

Detaljno su opisani:

- 1.1 Radni stol učitelja
- 2.1–2.9 Učenici / Grupe
- 3.1–3.4 Raspored
- logika 2.4 procjene spremnosti i 2.5 detalja znanja
- 4.1 Biblioteka materijala
- 4.2 Pregled materijala
- 4.3 Novi materijal — izrada prezentacije
- 4.4–4.5 Uvoz i uređivanje materijala
- 5.1–5.6 Priprema sata / Lesson Builder
- 6.1–6.5 PLUS 5 Ploča / Live Lesson
- 7.1–7.2 Povijest sati
- 8.1–8.3 Domaće zadaće
- 9.1–9.2 Poruke
- 10.1–10.9 Izvještaji
- 11.1–11.3 Financije
- 12.1–12.7 Postavke
- 13.1 Centar obavijesti
- 14.1–14.3 Profil/account

Detaljni screen/lifecycle source ne zaključava automatski domenski, permission, storage, metric, finance ili delivery contract. Ti gateovi ostaju u `ROADMAP.md`, `OPEN_QUESTIONS.md` i `source_specs/DOCUMENTATION_BACKLOG.md`.

Dodatno su source-derived dokumentirani DS-001, 25 DRAFT Lesson Builder Knowledge
Blockova, master sitemap C/C1–C11, bazni FS-001–FS-003 te cijela studentska sitemap/screen
struktura. Selektivni merge i izvorne lokacije dokumentirani su u
`SOURCE_PACKAGE_AUDIT_2026_09_04.md`.
