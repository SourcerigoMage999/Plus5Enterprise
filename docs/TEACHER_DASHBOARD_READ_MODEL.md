# Teacher dashboard read model

## Status

**Phase 8.1 — FINAL LOCKED — odobreno 2026-10-25.**

Ovaj dokument definira owner-scoped read model i API za budući Screen 1.1. Mjerodavni
business izvori su `source_specs/Radi_stol_uU010ditelja.md`, `source_specs/CROSS_ROLE_BASELINE.md`
i zaključani Student/Schedule contracti. Phase 8.1 ne implementira dashboard UI.

## Granica read modela

Dashboard vraća samo činjenice koje već imaju canonical izvor u postojećim modulima:

- današnji datum u `Europe/Zagreb` i UTC vrijeme izračuna;
- sve Teacherove današnje termine osim otkazanih, kronološki poredane;
- prvi budući termin sa statusom `Scheduled` kao sljedeći sat;
- osnovni Session kontekst: individualni Student ili Group, razred, Program, naslov, vrijeme,
  lokacija/online signal, status i broj sudionika;
- ukupan broj nearhiviranih Studenata i raspodjelu po canonical `StudentStatus` vrijednostima.

Grupni broj sudionika računa se iz membershipa aktivnih na početku konkretnog termina.
Individualni termin ima jednog sudionika. `TeacherAccountId` uvijek dolazi iz provjerene
sesije i svaki dio upita ograničen je istim ownerom prije projekcije.

## API

```http
GET /api/v1/dashboard
```

Endpoint zahtijeva Teacher policy, nema query parametre i vraća:

- `timeZoneId`, `today`, `generatedAtUtc`;
- `todaySessions`;
- nullable `nextSession`;
- `students` s `totalCount`, `activeCount`, `onHoldCount` i `inactiveCount`.

Response ne izlaže Session bilješke, Teacher ID, Student kontakt, Guardian podatke, storage
ključeve niti druge nepotrebne osobne ili sigurnosne podatke. Anonymous zahtjev vraća `401`.
Prazan dashboard legitimno vraća praznu listu, `nextSession = null` i nulte brojeve.

## Namjerno odgođene canonical sekcije

Phase 8.1 ne izmišlja podatke za dijelove source mockupa čiji canonical izvor još ne postoji:

- pripremljenost i aktivnosti sljedećeg sata čekaju Lesson Plan iz Phase 9;
- `Pokreni sat` čeka Lesson Session/Ploču iz Phase 10;
- obavijesti, poruke i nedavne aktivnosti čekaju vlastite event/permission contracte;
- domaće zadaće čekaju Homework contract;
- `Korišteni materijali` čekaju stvarnu usage vezu; `recently added` nije zamjena za
  `recently used`;
- financijski sažetak čeka finance contract;
- izvrsni/vrlo dobri/dobri/rizični Student bandovi nisu izvedeni iz statusa niti browser
  prosjeka jer ne postoji zaključana ukupna Student readiness metrika;
- osobni pozdrav imenom čeka canonical Teacher profile podatak; e-mail local-part nije ime.

Phase 8.2 smije prikazati samo dostupne 8.1 podatke i poštene disabled/omitted buduće
sekcije prema ovom contractu.

## Acceptance

- owner/foreign scope, današnja lokalna granica, cancelled exclusion, next-session izbor,
  membership count i Student status counts pokriveni su ciljanim query testovima;
- endpoint auth boundary i identity-derived owner pokriveni su ciljanim API testom;
- SQL Server runtime korekcija od 2026-10-26 uklonila je provider-neprevodive korelirane
  `Single()` projekcije i zamijenila ih bounded owner-scoped batch hydrationom; stvarni
  dashboard API potvrđen je s HTTP 200 bez promjene response contracta;
- nema schema/migration, write, frontend ni infrastrukturne promjene.
