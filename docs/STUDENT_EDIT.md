# Edit student

## Status

**LOCKED v1.0 — 2026-09-02**

Izvršni contract za Phase 3.4 i ekran 2.6 “Uredi učenika”. Nadopunjuje `STUDENT_FOUNDATION.md`, `GROUP_FOUNDATION.md`, `STUDENT_CREATE.md`, `STUDENT_DOSSIER.md` i izvorni `2.6_Uredi_uU010denika.md`.

## Scope i pravila

- Teacher uređuje samo nearhiviranog Studenta u vlastitom ownership scopeu; Teacher ID dolazi isključivo iz autentificiranog identiteta.
- Uređuju se postojeća Student polja: ime, prezime, nadimak, datum rođenja, SchoolGrade, škola, spol, e-mail, telefon, Program, DeliveryMode i status.
- Program i DeliveryMode ostaju eksplicitan par ili oba izostaju. Group se bira samo za `Group` način rada i mora biti aktivna, nearhivirana Group istog Teachera i odabranog Programa.
- Promjena `Individual → Group`, izlazak iz Group ili transfer između Groupa atomarno ažurira Student organizaciju, vremenski GroupMembership i oba pogođena Group retka uz capacity/concurrency zaštitu.
- Guardian kontakti mogu se dodavati i uređivati. Postojeći Guardian ne može se preuzeti iz drugog Studenta; najviše jedan kontakt može biti primarni.
- Phase 3.4 ne uvodi brisanje Guardian kontakta jer retention i buduće communication reference nisu zaključane.
- Student dobiva SQL `rowversion` optimistic-concurrency token. Stale edit/archive vraća kontrolirani `409` i ne prepisuje novije podatke.
- Product “delete” je arhiviranje uz eksplicitnu potvrdu. Arhiviranje postavlja Student status `Inactive`, završava aktivno GroupMembership članstvo i čuva povijest. Fizičko brisanje nije dopušteno.

## API

- `GET /api/v1/students/{studentId}/edit` vraća owner-scoped edit model, Guardians i concurrency token.
- `PUT /api/v1/students/{studentId}` zahtijeva Teacher autorizaciju i CSRF te sprema cijeli administrativni edit.
- `POST /api/v1/students/{studentId}/archive` zahtijeva Teacher autorizaciju, CSRF i concurrency token.
- Missing, archived i cross-owner Student/Guardian/Program/Group reference ne otkriva tuđe postojanje.
- Validation je `400`, stale/capacity/unavailable stanje kontrolirani `409`, a ownership/missing `404`.

## UI i navigacija

- `/students/{id}/edit` otvara se iz dosjea i akcije na popisu.
- “Spremi promjene” vraća na dosje uz potvrdu; “Otkaži” se vraća bez spremanja.
- “Arhiviraj učenika” otvara potvrdu i nakon uspjeha vraća na popis.
- Forma slijedi canonical raspored za osnovne podatke, program/grupu, kontakte i dodatne informacije, ali obavezno dodaje zasebni DeliveryMode koji PNG izostavlja.
- Avatar/upload nije aktivan bez storage contracta. Knowledge procjena, ciljana razina, bilješke, analitika, PLUS 5 Ploča i visibility toggles nisu lažno spremivi dok njihovi modeli i permissions nisu zaključani.

## Out of scope

Hard delete/legal erasure, Guardian removal, avatar/storage, Knowledge/Evidence/readiness, proficiency target, notes, audit-history UI, organization sharing, reports/privacy toggles, Board access i communication account linking.

## Vizualni izvor i odstupanja

Canonical `2.6 Uredi učenika.png` mjerodavan je za layout, proporcije i hijerarhiju. Tekstualni source ima prednost kada traži zaseban DeliveryMode i zabranjuje ručno uređivanje procijenjenog znanja. Buduća polja prikazuju se neutralno ili se izostavljaju; ne spremaju se inertni booleani ni lažni Knowledge podaci.
