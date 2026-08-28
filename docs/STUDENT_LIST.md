# Student list

## Status

**LOCKED v1.0 — 2026-08-29**

Ovaj dokument je izvršni contract za Phase 3.1, ekran 2.1 “Popis učenika”. Nadopunjuje `STUDENT_FOUNDATION.md`, `GROUP_FOUNDATION.md`, `SCHEDULING_FOUNDATION.md`, `API_CONVENTIONS.md` i autentikacijski contract.

## Scope

- popis samo aktivnog Teacherovog ownership scopea; arhivirani Student redci nisu vidljivi
- pretraga po imenu, prezimenu, punom imenu ili nadimku
- filteri Program, DeliveryMode, StudentStatus i SchoolGrade
- bounded offset pagination, default `page=1&pageSize=25`, maksimum 100
- stabilno sortiranje po prezimenu, imenu i identifikatoru
- tablični i kartični prikaz s URL-backed filter/page/view stanjem
- overview ukupnog broja, statusa, programa i dostupnih filter-opcija
- loading, unfiltered empty, filtered empty, safe error i retry stanja

## API contract

Oba endpointa zahtijevaju autentificirani Teacher policy. `TeacherAccountId` čita se isključivo iz server-side claims identiteta i nije dio requesta.

### `GET /api/v1/students`

Opcionalni query parametri:

| Parametar | Contract |
|---|---|
| `page` | integer ≥ 1; default 1 |
| `pageSize` | integer 1–100; default 25 |
| `search` | trimani tekst, najviše 100 znakova |
| `programId` | ne-prazan GUID |
| `deliveryMode` | `1` individualno, `2` grupa |
| `status` | `1` aktivan, `2` na čekanju, `3` neaktivan |
| `schoolGradeId` | ne-prazan GUID |

Response koristi standardni `PagedResponse<T>`. Student redak sadrži osnovni identitet, razred, opcionalni Program/DeliveryMode, aktivnu grupu, status i vrijeme zadnjeg održanog termina.

### `GET /api/v1/students/overview`

Vraća owner-scoped ukupne/status brojeve, broj učenika bez programa, raspodjelu po programu te Program i SchoolGrade opcije koje su stvarno prisutne u Teacherovoj nearhiviranoj bazi učenika.

## Izvedena polja

- aktivna grupa dolazi samo iz `GroupMembership` retka bez `LeftAtUtc`
- “zadnji sat” je najnoviji `Session` sa statusom `Held`
- individualni Student koristi vlastiti Session context
- grupni Student koristi Session trenutne aktivne grupe
- planirani, otkazani i termini u tijeku ne ulaze u “zadnji sat”
- Phase 3.1 ne računa napredak; UI prikazuje neutralno “Nije dostupno”

## Security i privacy

- svaki Student, Program, GroupMembership, Group i Session dio upita ograničen je Teacher ownershipom
- klijent ne šalje i ne bira tenant/Teacher identifikator
- response ne izlaže Guardian kontakt, adresu, bilješke ni druge nepotrebne PII podatke
- 401/403 koriste postojeću centralnu auth navigaciju; neočekivane greške ostaju sigurni ProblemDetails odgovori

## Out of scope

- create/edit/archive Studenta
- digitalni dosje i detaljni Student route
- komunikacija s roditeljem/učenikom
- Group CRUD ili membership promjene
- readiness/knowledge/progress postotak
- attendance ili lesson evidence

UI akcije koje pripadaju tim fazama moraju ostati onemogućene i jasno označene; ne smiju biti mrtvi linkovi niti simulirati spreman feature.

## Visual acceptance gate

Canonical PNG naveden u `SOURCE_DOCUMENT_INDEX.md` nije pohranjen u repozitoriju. Phase 3.1 ne smije dobiti završni `LOCKED` status dok se stvarni desktop prikaz i mobilna prilagodba ne usporede s tim PNG-om.

Obavezni dokaz prije zaključavanja:

- canonical PNG visual comparison
- stvarni desktop screenshot comparison
- mobile adaptation review
- dokumentiran popis namjernih odstupanja, ako ih ima
- ponovljeni accessibility i responsive smoke test nakon vizualnih dorada
