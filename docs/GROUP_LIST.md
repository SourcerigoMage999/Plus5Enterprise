# Group list — Phase 3.5 / Screen 2.7

## Status

**DONE — implementation, SQL runtime and visual acceptance gates PASS — 2026-09-02.**

Canonical izvori: `source_specs/2.7_Grupe.md`, izvorni `2.7 Grupe.png`,
`GROUP_FOUNDATION.md`, `SCHEDULING_FOUNDATION.md` i projektni engineering standardi.
Ova faza ne otvara kreiranje/uređivanje grupe iz Phase 3.6/3.7.

## UI i navigacija

- `/students/groups`, dostupno preko poveznice **Grupe** na popisu učenika.
- Prema korisničkoj UI korekciji, **Grupe** je žuti navigacijski gumb jednakog stila i širine kao **Dodaj učenika**, postavljen iznad njega s razmakom.
- Postojeći PLUS 5 shell; sidebar **Učenici** ostaje aktivan.
- Breadcrumb, naslov, četiri organizacijske kartice, lijevi popis i desni detalj.
- URL čuva `search`, `programId`, `status`, `page` i odabranu `group`.
- Statusi: Aktivna, Na čekanju, Neaktivna. Search je literalni substring naziva.
- Grupe, članovi, kandidati i termini imaju stranice od 8 stavki u UI-u.
- Desktop odnos panela 44:56; na užim ekranima paneli se slažu okomito.
- Loading, empty, error/retry, nedostupan detalj i puni kapacitet imaju eksplicitna stanja.
- Tabovi imaju semantiku i navigaciju strelicama/Home/End; tablica se lokalno pomiče na uskom ekranu.

## Precizna semantika podataka

- Statistike su za sve nearhivirane grupe prijavljenog Teachera, ne samo trenutni filter.
- Ukupno učenika broji aktivna članstva (`LeftAtUtc == null`), ne povijesna članstva.
- Prosjek je broj članstava podijeljen brojem nearhiviranih grupa; za nula grupa prikazuje se nula.
- Slobodna mjesta zbrajaju kapacitet minus aktivna članstva **samo aktivnih** nearhiviranih grupa.
- **Termini ovaj tjedan** broji spremljene neotkazane grupne Sessione čiji početak pripada
  intervalu ponedjeljak 00:00 do idućeg ponedjeljka 00:00 u `Europe/Zagreb`.
  UI prikazuje početni datum tjedna. Nije riječ o trajanju u satima niti o procjeni iz pravila ponavljanja.
- Redoviti raspored dolazi iz postojećih serija važećih na današnji zagrebački datum
  (`StartsOn <= today <= EndsOn`). `SupersededAtUtc` ne briše povijesno važeći interval.
- Po grupi se vraća najviše 14 redovitih pravila, deterministički sortiranih; kad je granica
  dosegnuta, tab Raspored eksplicitno navodi da prikazuje prvih 14. Header/lista sažimaju prva dva.
- Tab Raspored odvojeno prikazuje važeća pravila i paginirane spremljene nadolazeće/tekuće
  neotkazane Sessione (`EndsAtUtc >= now`), u vremenskoj zoni konkretnog termina.
- Ne generiraju se termini; ne uvodi se druga kopija rasporeda na Group entitetu.
- URL videopoziva nije izložen ovom read modelu; prikazuje se samo oznaka Online.

## Članstvo

- Dodavanje nudi samo vlastite nearhivirane učenike bez aktivne grupe.
- Podudaranje programa **i** razreda daje prioritet, ali ne isključuje ostale učenike.
- Potvrda jasno navodi da učenik preuzima program grupe i grupni način rada.
- Uklanjanje završava postojeće članstvo, ne briše učenika; program ostaje isti,
  a način rada postaje individualni. Potrebna je eksplicitna UI potvrda.
- Premjesti otvara postojeći `/students/{id}/edit` workflow iz Phase 3.4; nema novog
  paralelnog obrasca za transfer niti automatskog transfera kroz Dodaj.
- Dodavanje je dopušteno samo u aktivnu nearhiviranu grupu koja ima slobodno mjesto.
- Brojanje kapaciteta, Student organizacija, članstvo i Group timestamp čine jednu transakciju.
- Oba client rowversiona moraju odgovarati učitanim Student/Group zapisima. Oba zapisa se
  ažuriraju čak i unutar istog clock ticka. SQL rowversion i unique active-membership indeks
  štite konkurentne upise; konflikt je 409 i traži ponovno učitavanje.

## API

Svi endpointi imaju Teacher policy i owner iz autentificiranih claims. Tuđi/nedostupni
identifikator vraća 404 bez otkrivanja postojanja zapisa. Nema client TeacherAccountId polja.

| Metoda / putanja | Contract |
|---|---|
| `GET /api/v1/groups` | Paginirana lista; search max 100, programId GUID, status 1–3 |
| `GET /api/v1/groups/overview` | Organizacijski brojevi i datum početka tjedna |
| `GET /api/v1/groups/{id}` | Header, kapacitet, aktualno članstvo, rowversion, bounded redovita pravila |
| `GET /api/v1/groups/{id}/students` | Paginirani trenutni članovi, razred, Student rowversion |
| `GET /api/v1/groups/{id}/candidates` | Paginirani negrupirani učenici, search i preporučeno podudaranje |
| `GET /api/v1/groups/{id}/sessions` | Paginirani nadolazeći/tekući neotkazani termini |
| `POST /api/v1/groups/{id}/members/{studentId}` | CSRF; `join`, Base64 `groupRowVersion`, `studentRowVersion`; uspjeh 204 |

Pagination default 1/25, max pageSize 100, stabilni secondary ID ordering i long offset guard.
Nevaljan zahtjev/CSRF: 400; bez sesije: 401; ne-Teacher: 403;
nedostupni podaci: 404; stale/capacity/status/membership konflikt: 409.
DTO-ovi su eksplicitni u API/Application slojevima, EF je samo u Infrastructure.
Program filter ponovno koristi postojeći owner-scoped `/students/overview` katalog.

## Namjerne granice i odstupanja od PNG-a

- `Sati tjedno` zamijenjeno je preciznijim **Termini ovaj tjedan** da se ne izmisli
  trajanje školskog sata ili broj generiranih termina.
- Nova grupa (3.6), Uredi (3.7), PDF (izvještaji) i otvaranje kalendara/detaila termina
  ostaju disabled s objašnjenjem. Tab Raspored ipak čita postojeće stvarne podatke.
- Materijali, bilješke, procjena razine i prisutnost imaju iskrene nedostupne sadržaje;
  nema postotaka, B1 vrijednosti, lažnih bilješki ni Knowledge upisa.
- Vidljive akcije Ukloni/Premjesti umjesto skrivenog trotočkastog izbornika; sačuvana je zona Akcije.
- Inicijali umjesto fotografija do zatvaranja storage/privacy gatea.
- Kandidati se prikazuju unutar detalja, s potvrdom i objašnjenjem promjene programa.
- Desktop screenshot comparison i mobile adaptation review izvršeni su na stvarnoj
  aplikaciji i odobrenom demo računu. Dokazi, mjerenja i preostala namjerna odstupanja
  nalaze se u `visual-acceptance/README.md`, odjeljak Phase 3.5.
- Zadržani su postojeći shell wordmark na bijeloj podlozi, 204 px sidebar i brand tokeni
  iz prethodnih prihvaćenih faza; nisu preslikani ilustrirani logo i svaki ton plave iz PNG-a.
- Mobilna tablica ima minimalnu širinu 544 px i imenovanu fokusabilnu scroll regiju;
  dokument se ne pomiče vodoravno. Status je uz naziv grupe, uz prelamanje na uskom ekranu.

## Runtime provjera

`GroupSqlRuntimeTests` je opt-in. Bez `PLUS5_TEST_SQL_CONNECTION_STRING` se eksplicitno
preskače, ne prikazuje lažni PASS. Dopušta samo lokalni SQL Server; za Compose koristiti
`127.0.0.1,1433` (port je vezan na IPv4, ne na `::1`).

Test stvara nasumično imenovanu zasebnu `Plus5_Phase35Test_<guid>` bazu, primjenjuje
migracije, izvršava projekcije i konkurentni upis zadnjeg mjesta, provjerava stale
rowversion i završavanje članstva te uklanja samo vlastitu testnu bazu u finally.
Potrebna je lokalna testna SQL vjerodajnica s pravom stvaranja/uklanjanja baze,
nikada production/app-login. Lozinka se ne sprema u test niti dokumentaciju.
