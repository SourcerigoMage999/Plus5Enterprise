# Schedule calendar contract

## Status

**Phase 4.1 implementation contract — 2026-09-12.**

Ovaj dokument razrađuje Screen 3.1 source bez proširenja u Session detail/create/edit ili
recurrence write scope. `SCHEDULING_FOUNDATION.md` ostaje authority za canonical Session,
ownership, vrijeme, konflikt i series granice. Canonical PNG `3.1 Početna.png` vodi layout,
proporcije i vizualnu hijerarhiju.

## Scope i granica

Phase 4.1 je owner-scoped read-only pregled postojećih `Session` zapisa:

- default je tjedan od ponedjeljka do nedjelje; dnevni prikaz je alternativni pogled istog
  ekrana i istog read modela
- prethodni/sljedeći period, `Danas` i mini-kalendar mijenjaju URL stanje
- filteri su Grupa, Program i Učionica; `Prikaži samo moje termine` je uvijek uključeno jer
  Teacher nikad ne dobiva cross-owner podatke
- prikazuju se Group i Individual Sessioni osim `Cancelled`
- fizička lokacija, online signal, program, kapacitet i aktivno članstvo prikazuju se samo iz
  canonical zapisa
- raspored se ne duplicira u zasebnu calendar tablicu niti se iz URL-a izvodi ownership

`+ Novi termin`, klik na termin, detaljni izvještaj i centar podsjetnika pripadaju kasnijim
fazama. Kontrole mogu biti vizualno prisutne prema canonical PNG-u, ali moraju biti stvarno
onemogućene i objasniti granicu; ne smiju biti inertni lažni uspjeh.

## API

Teacher-only endpoint:

```http
GET /api/v1/schedule?from=YYYY-MM-DD&to=YYYY-MM-DD&groupId=&programId=&locationId=
```

`from` je uključen, `to` isključen. Oba datuma su obavezna, `to > from`, a raspon smije biti
najviše 31 dan. Prazan GUID i nevaljan raspon daju `400 invalid_calendar_range`; anonimni
poziv daje `401`. Teacher ID uzima se isključivo iz provjerene sesije.

Response sadrži:

- `timeZoneId`, `from`, `to`
- kronološki poredane `items`
- najviše dva sljedeća owner-scoped podsjetnika
- eksplicitni `summary`
- Group, Program i Location opcije prisutne u zadanom rasponu prije aktivnih filtera

DTO ne izlaže EF entitete, Teacher ID, bilješke ili druge osjetljive podatke. Upit prvo
ograničava SQL po owneru, vremenskom rasponu i statusu, zatim primjenjuje odabrane filtre na
taj bounded skup. Nema N+1 upita po Sessionu.

## Vrijeme i prikaz

Canonical prikaz koristi `Europe/Zagreb`. Granice lokalnih datuma pretvaraju se u UTC na
backendu, a Session UTC timestampovi u lokalni datum i vrijeme na klijentu. Time se tjedan,
DST i ponoć ne određuju browserovom proizvoljnom vremenskom zonom.

URL je navigation state:

```text
/schedule?date=2026-09-16
/schedule?date=2026-09-16&view=day&groupId=<guid>
```

Ne uvodi se paralelni globalni calendar store. Week/day, datum i filtri preživljavaju
Back/Forward i izravno otvaranje linka.

## Precizne metrike

Source izraz “učenici” nije prikazan kao jedan dvosmislen broj. UI i API koriste:

- `Jedinstvenih učenika`: unija Student ID-eva koji sudjeluju u vidljivim individualnim
  terminima i aktivnim GroupMembershipima na početku svakog vidljivog grupnog termina
- `Planiranih dolazaka`: jedan dolazak po individualnom terminu plus broj aktivnih članstava
  na početku svakog grupnog termina
- `Slobodnih mjesta`: zbroj `max(0, Group.Capacity - activeMembershipCountAtSessionStart)`
  po vidljivom grupnom terminu

Uz njih se prikazuju broj grupnih, individualnih i ukupnih termina. Cancelled i cross-owner
zapisi ne utječu ni na jednu metriku.

## Stanja i responsive ponašanje

Ekran mora imati loading, error s retryjem i pošteno empty stanje. Desktop zadržava stalni
PLUS 5 sidebar, žuti aktivni Raspored, centralni kalendar i desnu summary zonu. Na užim
viewportovima desne kartice se slažu ispod kalendara; sam tjedni grid ostaje imenovana,
tipkovnicom fokusabilna lokalno vodoravno pomična regija. Document-level horizontalni
overflow nije dopušten.

## Testni i visual gate

Prije Phase 4.1 completiona obavezno je dokazati:

- auth/ownership, cancelled/range/filter ponašanje i precizne metrike na stvarnom SQL Serveru
- API validation i prazan rezultat
- week/day/date/filter URL flow, loading/error/retry/empty i zaključane buduće akcije
- canonical desktop 1536×1024 usporedbu i mobile 390×844 prilagodbu bez document overflowa
- Release build, architecture testove, format, frontend lint/typecheck/build, vulnerability
  audite, Docker health i non-root runtime

## Izvan Phase 4.1

Session detail, create/edit, “samo ovaj termin” ili buduća serija, recurrence replenishment,
conflict override, notification delivery, izvještaji, attendance evidence, Knowledge Model i
nova migracija nisu dio ove faze.
