# Phase 3.7 — Edit group visual acceptance

## Rezultat

**PASS — 2026-09-11.** Stvarni `/students/groups/:groupId/edit` uspoređen je s canonical
`2.9 Uredi grupu.png` (SHA-256 `2B52DCA2752BE26FEC974E625A4E0C2BC130AA5626B13833EFF704155C728E7F`)
iz odobrenog teacher source paketa. Usporedba je high-fidelity, ne pixel-perfect tvrdnja.

Provjera je izvedena u stvarnoj lokalnoj aplikaciji s normalnom prijavom i API podacima.
Lozinka, cookie i token nisu spremljeni. Headless Edge koristio je desktop 1536×1024 i
mobile 390×844; spremljene slike su full-page, pa su više od viewporta.

## Dokazi

- [Desktop edit](edit-desktop.png)
- [Mobile edit](edit-mobile.png)
- [Desktop zaštita nespremljenih promjena](unsaved-desktop.png)
- [Mobile zaštita nespremljenih promjena](unsaved-mobile.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| Canonical layout i hijerarhija | PASS — četiri gornja panela, donje zone i završna napomena slijede source |
| Shell i aktivni modul | PASS — PLUS 5 logo, plavi shell, žuti Učenici, profil i notifications prostor |
| Naslov i akcije | PASS — `2.9 Uredi grupu`, Odustani, archive granica i dominantni Spremi promjene |
| Osnovni podaci | PASS — naziv, Program, objašnjeni Program lock, razred i opis |
| Način rada i capacity | PASS — Group prikaz, stvarni member count, slobodna mjesta, lokacija i status |
| Raspored | PASS — stvarno pravilo, opcionalni završetak, budući datum i jasna kalendarska posljedica |
| Sažetak | PASS — identitet, status, Program, razred, capacity, termini i lokacija |
| Članovi i buduće zone | PASS — stvarni članovi; materijali/ciljevi/bilješke ostaju neutralni |
| Program business pravilo | PASS — kontrola je disabled uz aktivnog člana i vidljivo objašnjenje |
| Dirty navigation | PASS — fokusirana potvrda s ostankom/napuštanjem na desktopu i mobitelu |
| Desktop overflow | PASS — document/client širina 1536/1536 |
| Mobile adaptation | PASS — okomiti paneli i document/client širina 390/390 |
| Browser greške | PASS — 0 pageerror događaja |

## Stvarna funkcionalna provjera

Na zasebnoj praznoj demo grupi spremljen je samo opis `Phase 3.7 funkcionalni pregled.`.
Save je vratio korisnika na odabranu grupu i prikazao potvrdu. Screenshot flow nije radio
business write: mijenjao je samo vrijednost naziva u browseru, otvorio dirty-navigation
potvrdu i ostao na obrascu.

## Namjerna odstupanja

- Archive je prigušena akcija umjesto canonical hard-delete akcije. Hard delete je
  zabranjen foundationom, a posljedice arhiviranja grupe s rasporedom još nisu zaključane.
- Program je disabled kada postoje aktivni članovi prema ADR-0016; canonical PNG ne nosi
  taj naknadno zaključani business state.
- Stvarni podaci, broj članova i jedan termin zamjenjuju primjer iz mockupa; nisu dodani
  lažni redci radi popunjavanja prostora.
- Materijali, ciljevi i bilješke su informativne neutralne zone bez preuranjenog modela.
- Postojeći prihvaćeni shell, font/tokeni i responsive mobilna navigacija imaju prednost
  nad doslovnim kopiranjem svakog piksela izvornog PNG-a.
- Native date/time kontrole u automatiziranom Edge profilu mogu prikazati 12-satni locale;
  aplikacijski sažetak i backend contract ostaju Europe/Zagreb i 24-satni.

## Ponovljiva provjera

Pokrenuti lokalni Compose, prijaviti se odobrenim demo računom bez spremanja lozinke,
otvoriti Učenici → Grupe → Uredi. Provjeriti Program lock na grupi s članom, urediti polje
bez spremanja i odabrati Odustani. Pregledati viewportove 1536×1024 i 390×844 te potvrditi
da nema document overflowa. Za funkcionalni save koristiti praznu demo grupu; ne mijenjati
članstva ili raspored samo radi visual gatea.
