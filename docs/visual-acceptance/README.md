# Visual acceptance

## Phase 3.1 — Student list

## Source

Canonical vizual uspoređen je iz dostavljenog paketa koji sadrži ukupno 78 PNG mockupova:

`Za programera - novo.zip/Za programera - novo/2.0 Učenici/2.1 Popis učenika/2.1. Popis učenika.png`

PNG je mjerodavan vizualni izvor; tekstualni Phase 3.1 contract ostaje mjerodavan za sigurnost, ownership, API i fazni scope.

## Dokazi

- `PHASE_3.1_STUDENT_LIST_DESKTOP_1536.png` — desktop 1536×1024
- `PHASE_3.1_STUDENT_LIST_MOBILE_390.png` — mobilna prilagodba 390×844

## Rezultat usporedbe

- PASS — stalni 204 px PLUS 5 sidebar i žuti aktivni Students element
- PASS — naslov, podnaslov, dominantna Add Student akcija i globalni profil/notification prostor
- PASS — search, četiri filtera i card/table toggle
- PASS — centralni Student prikaz, statusi, progress tretman, akcije i pagination
- PASS — desne Student overview i Programs kartice
- PASS — spacing, typography, colors, borders, radii, control sizes, proportions i visual hierarchy
- PASS — mobilna prilagodba bez preklapanja; filteri se slažu, tablica ostaje vodoravno pomična

## Namjerna odstupanja

- Inicijali zamjenjuju fotografije jer zaključani Student/API contract nema odobren avatar izvor.
- Progress je neutralna crtica sa sivom trakom jer Knowledge/Evidence model još nije implementiran; lažni postotci nisu dopušteni.
- Create, dossier, communication i edit kontrole vizualno su prisutne, ali onemogućene do svojih faza.
- Export i Groups summary nisu dio zaključanog Phase 3.1 contracta pa nisu simulirani klijentski izvedenim podacima.
- API zadržava default `pageSize=25`; broj vidljivih redaka ovisi o stvarnim podacima, a ne o broju redaka u mockupu.

## Phase 3.2 — Create student

### Source

Canonical `Za programera - novo/2.0 Učenici/2.3. Novi učenik/2.3 Novi učenik.png` uspoređen je s renderiranim ekranom. PNG vodi izgled, a `STUDENT_CREATE.md` i izvorni tekst vode ponašanje i sigurnost.

### Dokazi

- `PHASE_3.2_STUDENT_CREATE_DESKTOP_1536.png` — desktop 1536×1024
- `PHASE_3.2_STUDENT_CREATE_MOBILE_390.png` — mobilna prilagodba 390×844

### Rezultat

- PASS — PLUS 5 shell, žuti aktivni Učenici element, breadcrumb, naslov i globalni profil/notification prostor
- PASS — centralna sekcionirana forma, dvije desne kartice, status hijerarhija i footer akcije
- PASS — proportions, spacing, typography, colors, borders, radii, shadows i control sizes
- PASS — desktop 1536×1024 bez preklapanja
- PASS — mobilni stacking i sticky akcije bez horizontalnog overflowa

### Namjerna odstupanja

- Program nema obaveznu zvjezdicu jer tekstualni source izričito ispravlja PNG i dopušta Student bez Programa.
- Guardian ime i prezime odvojena su radi zaključanog domenskog modela i pouzdane validacije.
- Fotografija nije uvedena bez odobrenog storage/avatar contracta; shell koristi postojeće inicijale.
- Nakon spremanja prikazuje se minimalna success boundary ruta; stvarni digitalni dosje pripada Phase 3.3.
