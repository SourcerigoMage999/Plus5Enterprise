# Student digital dossier

## Status

**LOCKED v1.0 — 2026-09-01**

Izvršni contract za Phase 3.3 i administrativni/core dio ekrana 2.2 “Digitalni dosje učenika”. Nadopunjuje `STUDENT_FOUNDATION.md`, `GROUP_FOUNDATION.md`, `SCHEDULING_FOUNDATION.md`, sigurnosne/frontend standarde i izvorni `2.2_Digitalni_dosje_uU010denika.md`.

## Scope i pravila

- Dosje je dostupan samo autentificiranom Teacheru na `/students/{studentId}`.
- Teacher ID dolazi isključivo iz autentificiranog identiteta; ne prima se iz URL-a, queryja ni tijela zahtjeva.
- Upit vraća samo nearhiviranog Studenta u vlasništvu trenutnog Teachera. Nepostojeći, arhivirani i cross-owner ID imaju isti `404` rezultat.
- Administrativni profil sadrži stvarno spremljene osobne/kontakt podatke, SchoolGrade, opcionalni Program, DeliveryMode, aktivnu GroupMembership i primarnog Guardiana.
- Sljedeći termin je prvi budući `Scheduled` Session za Studenta ili njegovu trenutačnu aktivnu Group.
- Zadnji održani sat je najnoviji `Held` Session: individualni Student Session ili Group Session čiji je termin unutar razdoblja Studentova članstva.
- Dosje ne izračunava readiness, Knowledge/Evidence napredak, rezultate aktivnosti ni sadržaj nastavničkih bilješki.

## API

- `GET /api/v1/students/{studentId}` zahtijeva Teacher policy.
- Uspjeh vraća `200` s profilom, primarnim Guardianom te opcionalnim sljedećim i zadnjim održanim Sessionom.
- Neispravan/prazan, nepostojeći, arhivirani ili tuđi Student vraća `404` bez potvrđivanja njegova postojanja.
- API je read-only i ne uvodi promjenu baze ni novu migraciju.

## UI i navigacija

- Ime učenika, tablična akcija i kartična akcija na `/students` vode na stvarni dosje.
- Nakon uspješnog stvaranja `/students/new` vodi izravno na novi dosje.
- Ekran zadržava PLUS 5 shell, aktivni Students element, breadcrumb, profil, status, glavne akcije i canonical card hijerarhiju.
- Loading, recoverable error i privacy-preserving not-found stanja su eksplicitna.
- Akcije za poruke, zakazivanje i uređivanje vidljive su radi vizualne hijerarhije, ali su onemogućene do pripadajućih faza.
- Readiness, napredak, materijali, aktivnosti, komunikacijska povijest i bilješke prikazuju iskrena neutralna stanja bez izmišljenih podataka.

## Out of scope

Uređivanje/arhiviranje Studenta, Group detail, upload fotografije, poruke, stvaranje termina, plan sata, materijali, aktivnosti, Knowledge/Evidence/readiness izračuni i privatne nastavničke bilješke.

## Vizualni izvor i odstupanja

Canonical `2.2 Digitalni dosje učenika.png` mjerodavan je za layout, proporcije, vizualnu hijerarhiju i responsive interpretaciju. Inicijali zamjenjuju fotografiju jer storage/avatar contract nije zaključan. Budući moduli ne simuliraju se canonical primjerima ili postotcima; prikazuju se neutralno dok njihovi modeli i permissions contracti ne budu zaključani.
