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

## Phase 3.3 — Student digital dossier

### Source

Canonical `Za programera - novo/2.0 Učenici/2.2 Digitalni dosje učenika/2.2 Digitalni dosje učenika.png` uspoređen je sa stvarnim administrativnim dosjeom. PNG vodi izgled, a `STUDENT_DOSSIER.md` i fazne granice vode ponašanje i podatke.

### Dokazi

- `PHASE_3.3_STUDENT_DOSSIER_DESKTOP_1536.png` — desktop 1536×1024
- `PHASE_3.3_STUDENT_DOSSIER_MOBILE_390.png` — mobilna prilagodba 390×844

### Rezultat

- PASS — PLUS 5 shell, žuti aktivni Učenici element, breadcrumb, identitet/status i globalni profil/notification prostor
- PASS — profil, readiness, plan rada, napredak, materijali, zadnji sat, aktivnosti, komunikacija i bilješke slijede canonical hijerarhiju
- PASS — proportions, spacing, typography, colors, borders, radii, shadows i control sizes
- PASS — desktop prikaz bez horizontalnog overflowa (`1536` viewport, `1521` document width)
- PASS — mobilni stacking bez horizontalnog overflowa (`390` viewport, `375` document width)

### Namjerna odstupanja

- Inicijali zamjenjuju fotografiju jer storage/avatar contract nije zaključan.
- Readiness, Knowledge/Evidence napredak, materijali, aktivnosti i bilješke koriste neutralna stanja; canonical demo postotci i sadržaj nisu stvarni podaci.
- Kartica komunikacije prikazuje samo stvarno spremljenog primarnog Guardiana; povijest poruka pripada Phase 9.
- Sljedeći i zadnji sat koriste postojeće Session podatke, bez preuranjenog lesson-plan ili activity modela.
- Poruka roditelju, zakazivanje i uređivanje ostaju vidljive, ali onemogućene do pripadajućih faza.

## Phase 3.4 — Edit student

### Source

Canonical `Za programera - novo/2.0 Učenici/2.6 Uredi učenika/2.6 Uredi učenika.png` uspoređen je sa stvarnim owner-scoped edit ekranom. PNG vodi raspored, proporcije i hijerarhiju; `STUDENT_EDIT.md` i tekstualni source imaju prednost za sigurnost, arhiviranje i fazni scope.

### Dokazi

- `phase-3.4-student-edit-desktop-1536x1024.png` — desktop viewport 1536×1024
- `phase-3.4-student-edit-mobile-390x844.png` — mobilna prilagodba 390×844

### Rezultat

- PASS — PLUS 5 shell, žuti aktivni Učenici element, breadcrumb, `2.6 Uredi učenika`, podnaslov i globalni profil/notification prostor
- PASS — tri primarne kartice za osnovne podatke, program/grupu i kontakte te tri donje canonical zone
- PASS — header save/cancel/archive akcije i puna footer napomena
- PASS — spacing, typography, colors, borders, radii, shadows, control sizes i proportions izvedeni su iz canonical PNG-a
- PASS — desktop bez horizontalnog overflowa (`1536` viewport, `1521` document width)
- PASS — mobilni stacking bez horizontalnog overflowa (`390` viewport, `375` document width)

### Namjerna odstupanja

- `Arhiviraj učenika` zamjenjuje canonical fizičko brisanje jer zaključani retention/audit contract zabranjuje hard delete.
- DeliveryMode je zaseban izbor od Programa i Grupe prema tekstualnom sourceu i domenskom modelu.
- Fotografija/upload nisu prikazani bez zaključanog storage/avatar contracta.
- Knowledge/progress, nastavničke bilješke i privacy/analytics toggles neutralne su buduće zone; ne spremaju lažne podatke niti inertne postavke.
- Guardian kontakti su uređivi unosi umjesto read-only kartica; uklanjanje kontakta ostaje izvan faze dok retention i communication reference nisu zaključane.

## Phase 3.5 — Groups — PASS (2026-09-02)

### Izvor i način provjere

Canonical `Za programera - novo/2.0 Učenici/2.7 Grupe/2.7 Grupe.png` uspoređen je
sa stvarnom aplikacijom na `http://localhost:8081/students/groups` nakon frontend
Docker rebuilda. Neizmijenjena kopija: [canonical PNG](phase-3.5-groups-canonical.png).
SHA-256: `6CB493C4249606A64507E5AA27FBD63B4324A9536FDC33DBF452939F623A452F`.

Edge 152.0.4191.53, Windows, headless browser, device scale factor 1; desktop viewport
1536×1024 i mobile viewport 390×844. To je responsive browser review, ne fizički telefon.
Snimke su full-page osim zasebne mobilne potvrde, pa njihova visina može nadmašiti viewport.
Nema AI-generiranih slika, uređivanja screenshotova, lažnih API odgovora ni DOM zamjene.

Normalna prijava odobrenim demo računom. Postojeći podaci u trenutku snimanja:
Demo grupa Orion, Demo Matematika 7, Ana Demo i Luka Demo, kapacitet 2/2, bez rasporeda.
Nisu mijenjana članstva: potvrde su otvorene i otkazane. Lozinka, cookie i token nisu
spremljeni u dokumentaciju ili screenshotove.

### Dokazi

- [Desktop](phase-3.5-groups-desktop-1536x1024.png)
- [Mobile — cijela stranica](phase-3.5-groups-mobile-390x844.png)
- [Mobile — pomaknuta tablica i potvrda](phase-3.5-groups-mobile-actions-confirmation.png)
- [Raspored — stvarno prazno stanje](phase-3.5-groups-schedule-desktop.png)
- [Prazan status filter](phase-3.5-groups-empty-desktop.png)
- [Desktop potvrda](phase-3.5-groups-confirmation-desktop.png)

### Usporedba i rezultat

| Područje | Rezultat i dokaz |
|---|---|
| Canonical PNG visual comparison | PASS uz eksplicitna odstupanja niže; isti redoslijed i hijerarhija glavnih zona |
| Shell / aktivni modul | PASS — stalni plavi sidebar, žuti Učenici, globalni profil i notification prostor; zadržan ranije prihvaćeni shell |
| Naslov / subtitle / akcije | PASS — 2.7 Grupe, referentni podnaslov, PDF i primarna Nova grupa; profil završava na y≈70, akcije počinju na y≈81 |
| Četiri statistike | PASS — jednake kartice, pastelne ikone, dominantan broj, sekundarna napomena; plava/zelena/ljubičasta/žuta obitelj |
| Proporcije / paneli | PASS — lijevi 556 px, desni 707 px, razmak 18 px: 44:56; vrhovi panela poravnati na y≈299 |
| Lista / selected / status | PASS — search i dva filtera, plavi selected border/background, kružni identitet, status uz naziv, kapacitet desno; redak ≈82 px |
| Detalji / tabovi / bilješke | PASS — identitet, četiri metadata zone, aktivni podcrtani tab, tablica i donja zasebna kartica bilješki |
| Typography / borders / shadows | PASS — jasna razlika naslova, tijela i napomena, tanke granice, umjereni radiusi/sjene; postojeći font/brand tokeni |
| Desktop screenshot comparison | PASS — pregledana spremljena snimka, dokument scrollWidth = clientWidth = 1536 |
| Mobile adaptation review | PASS — 2×2 statistike, okomiti paneli, filtri u dvije linije, metadata 2×2; dokument 390/390 |
| Mobilna tablica / akcije | PASS — 544 px tablica unutar 306 px regije, scrollLeft=238 doseže Akcije; fokusabilna imenovana regija i čitljiva potvrda |
| Dodatne širine | PASS — 768, 1024, 1280 i 1402 px bez document overflowa (mjerenje, ne zasebni screenshot acceptance) |
| Tipkovnica / stanja | PASS — ArrowRight, End, Home mijenjaju tab i fokus; otkazivanje potvrde, prazan filter i povratak; 0 pageerror događaja |

### Ispravke nastale stvarnom usporedbom

- Odmaknute header akcije od profilne zone.
- Status premješten uz naziv; smanjena visina desktop retka grupe s oko 112 na 82 px.
- Usklađene obitelji boja statistika, naglašen identitet odabrane grupe i razdjelnici metadata.
- Dodani inicijali članova i čitljivija tipografija tablice; mobilni sadržaj više se ne stišće
  na 306 px, nego ima lokalni scroll s imenovanom fokusabilnom regijom.
- Component test čuva dostupnost regije i dekorativnu semantiku inicijala.

### Namjerna odstupanja i granice dokaza

- Ovo nije pixel-perfect tvrdnja: PNG prikazuje osam grupa i šest članova, a demo jedna/dva.
  Visine lista/kartica ovise o stvarnim podacima; ne dodaju se lažni redci da bi se popunio prostor.
- Zadržan je postojeći 204 px sidebar, tekstualni PLUS 5 wordmark na bijeloj podlozi i
  brand/font tokeni iz prethodno prihvaćenih faza. Ilustrirani logo na plavoj podlozi i
  intenzivnija plava iz PNG-a nisu uvedeni kao paralelni shell/design system.
- Postojeći shell notification prikaz nije ovim gateom potvrđen kao stvaran notification
  podatak; Phase 3.5 provjerava prostor i layout, ne uvodi sustav obavijesti.
- Inicijali umjesto fotografija; status filter je eksplicitan select umjesto skrivenog Filtrar/filter izbornika iz vizuala.
- Termini ovaj tjedan umjesto Sati tjedno; nema izmišljenih sati, procjena razine, prisutnosti ni bilješki.
- Vidljive Ukloni/Premjesti akcije i paginacija povećavaju visinu redaka članova u odnosu
  na mockupov trotočkasti izbornik. Informacije o budućim funkcijama dodaju tekstualne napomene.
- Nova grupa, Uredi i PDF ostaju disabled u skladu s faznim scopeom, zbog čega imaju prigušen izgled.
- Mobile koristi postojeću horizontalnu navigaciju i stacking, ne novu hamburger implementaciju.
- Raspored je vizualno provjeren u praznom stanju. Popunjena pravila/Sessioni provjereni su
  zasebnim SQL/query testovima, ne tvrdi se da ih ove demo snimke prikazuju.
- Last-seat/add/remove writes nisu ponavljani za screenshotove; prethodni automatizirani
  testovi ostaju dokaz poslovnih pravila. Visual gate nije produkcijski security/release gate.

### Ponovljiva provjera

Pokrenuti lokalni Compose, prijaviti se odobrenim demo računom bez spremanja lozinke u repo,
otvoriti Učenici → Grupe. Pregledati viewportove 1536×1024 i 390×844, tabove i filtere;
na mobitelu fokusirati/pomaknuti tablicu do Akcija, otvoriti Ukloni i odabrati Odustani.
Usporediti s canonical PNG-om i navedenim odstupanjima. Ne stvarati dodatne podatke niti
spremati promjenu članstva samo radi reprodukcije screenshotova.
