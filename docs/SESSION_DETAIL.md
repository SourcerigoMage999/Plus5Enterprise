# Session detail contract

## Status

**Phase 4.2 implementation contract — 2026-09-12.**

Ovaj dokument razrađuje Screen 3.2 nad postojećim canonical `Session` modelom. Izvorni
`3.2 Detalji termina.png` vodi layout, proporcije i vizualnu hijerarhiju;
`SCHEDULING_FOUNDATION.md` ostaje authority za Session, ownership, vrijeme, status i granicu
prema održanom satu.

## Scope i granica

Phase 4.2 je Teacher-only read-only detalj jednog konkretnog Sessiona:

- otvara se iz Screen 3.1 kalendarske kartice ili podsjetnika na `/schedule/:sessionId`
- breadcrumb vraća na datum termina i povezuje stvarni Group ili Student kontekst
- prikazuje delivery mode, kontekst, naslov, datum/vrijeme u canonical zoni, trajanje,
  fizičku/online lokaciju, Program, SchoolGrade, status, bilješku i stvarnu povijest kreiranja
- grupni roster projicira članstva koja vrijede na `Session.StartsAtUtc`; individualni termin
  ima svog canonical Studenta
- svaki učenik povezan je sa stvarnim digitalnim dosjeom
- missing i cross-owner ID daju isti `404` bez potvrde postojanja tuđeg zapisa

Session detail nije Group detail i ne mijenja trajni raspored grupe. Nema nove baze termina,
entiteta, migracije ni dependencyja.

## API

```http
GET /api/v1/schedule/{sessionId}
```

Endpoint zahtijeva Teacher policy. Owner dolazi samo iz provjerene sesije; client ne šalje
Teacher ID. Projekcija ne izlaže Teacher ID, online meeting URL, rowversion ni EF entitete.
Online termin izlaže samo signal `online`, ne povjerljivi join URL.

Response sadrži canonical Session polja, kontekst Programa/razreda/lokacije, series signale,
stvarne audit timestampove i participants listu. Group participants određuju se temporalno:

```text
JoinedAtUtc <= Session.StartsAtUtc
AND (LeftAtUtc IS NULL OR LeftAtUtc > Session.StartsAtUtc)
```

Upiti su bounded jednim owner-scoped Sessionom i njegovim kontekstom; nema N+1 upita po
učeniku.

## UI stanja i buduće zone

Ekran slijedi canonical header, sažetak termina, učenike, tri sadržajna stupca, povezano,
povijest i donji timezone/info strip. Loading, privacy-preserving not-found/error s retryjem i
prazan roster eksplicitni su.

Source prikazuje sadržaje čiji canonical modeli još nisu dostupni. Oni ostaju vidljivi radi
vizualne hijerarhije, ali pošteno zaključani:

- Tema/cilj/Knowledge Components, plan sata i Pokreni sat čekaju Phase 9–10
- materijali čekaju zaključane Phase 5–6 contracte
- attendance i sažetak stvarno održanog sata čekaju delivery/evidence model Phase 10–11
- domaća zadaća čeka Phase 12
- notification slanje čeka Phase 16 contract
- Uredi/Otkaži termin čekaju Phase 4.4 write contract; dupliciranje nije dio 4.2

`Session.Notes` se prikazuje kao stvarna napomena uz termin, ali se u 4.2 ne uređuje. Niti
jedna zaključana kontrola ne daje lažni uspjeh ili client-only write.

## Responsive i accessibility

Desktop zadržava stalni PLUS 5 shell, žuti Raspored, horizontalni session summary i tri glavna
stupca. Na užim širinama sadržaj se slaže; tablica učenika ostaje imenovana i tipkovnicom
fokusabilna lokalna scroll regija. Cijeli dokument nema horizontalni overflow.

## Testni i visual gate

Prije completiona obavezno je dokazati:

- auth i privacy-preserving 404
- group/individual kontekst, temporalni roster te cross-owner zaštitu na stvarnom SQL-u
- stvarne veze Calendar → Session detail → Group/Student i povratak na datum
- loading, error/retry, prazna stanja i zaključane buduće akcije
- canonical desktop 1536×1024 i mobile 390×844 usporedbu bez document overflowa
- Release build, architecture testove, format, frontend test/lint/typecheck/build, vulnerability
  audite te Docker health i non-root runtime

## Izvan Phase 4.2

Create/edit/cancel/duplicate Session writes, promjene jednog ili budućih termina, recurrence
replenishment, attendance, LessonPlan, live lesson, Evidence, Homework, Material i Notification
modeli nisu dio ove faze.
