# Create student

## Status

**LOCKED v1.0 — 2026-09-01**

Izvršni contract za Phase 3.2 i ekran 2.3 “Novi učenik”. Nadopunjuje `STUDENT_FOUNDATION.md`, `GROUP_FOUNDATION.md`, API/auth/frontend standarde i izvorni `2.3_Novi_uU010denik.md`.

## Scope i pravila

- Teacher stvara samo Student zapis u vlastitom ownership scopeu; Teacher ID dolazi isključivo iz autentificiranog identiteta.
- Obavezni su ime, prezime, SchoolGrade i status; zadani status je Active.
- Škola, datum rođenja, spol, e-mail, telefon i jedan primarni Guardian su opcionalni.
- Ako je započet unos Guardiana, ime i prezime Guardiana postaju obavezni.
- Program i DeliveryMode moraju biti zadani zajedno ili oba izostavljena. Program nije obavezan.
- Individual delivery ne smije imati Group. Group delivery zahtijeva postojeći aktivni, nearhivirani, nezasićeni Group istog Teachera i odabranog Programa.
- GroupMembership, Student i promjena Group kapaciteta spremaju se atomarno. `rowversion` konflikt vraća kontrolirani `409`.
- Cross-owner Program/Group reference tretira se kao `not_found`; API ne otkriva postojanje tuđih resursa.
- Referentne opcije dolaze iz baze. Nema hardkodiranog ili implicitnog seeda SchoolGrade/Program kataloga.

## API

- `GET /api/v1/students/create-options?programId={id}` vraća SchoolGrade, Teacher Program i eligible Group opcije.
- `POST /api/v1/students` zahtijeva Teacher autorizaciju i valjan CSRF token.
- Uspjeh vraća `201 Created`, Student ID i `Location` header.
- Validation vraća standardni `400`; stale/unavailable organizacijski odabir `404` ili `409` sa stabilnim problem codeom.

## UI i navigacija

- `/students/new` otvara formu iz dominantne akcije na popisu učenika.
- Cancel se vraća na `/students`; uspješan save ide na `/students/{id}`.
- Dok Phase 3.3 dossier nije implementiran, detaljna ruta prikazuje samo iskrenu success boundary poruku bez izmišljenih dossier podataka.
- Ako nema SchoolGrade opcija, submit je onemogućen uz operativnu poruku o nedostajućem katalogu.
- Program omogućuje DeliveryMode; Group kontrola postoji samo za group delivery. Desne kartice daju live sažetak.

## Out of scope

Student dossier/edit/archive, više Guardiana, Program/Grade/Group CRUD, upload fotografije, komunikacija te Knowledge/Evidence/readiness/progress.

## Vizualni izvor i odstupanja

Canonical `2.3 Novi učenik.png` mjerodavan je za layout i vizualnu hijerarhiju. Tekstualna specifikacija ima prednost nad PNG oznakom da je Program obavezan. Guardian se prikazuje kao odvojeno ime i prezime jer zaključani domen ne sprema neodredivo puno ime. Responsive prilagodba smije složiti stupce bez mijenjanja business pravila.
