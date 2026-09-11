# Phase 1.7 — Visual regression acceptance

**2026-09-11 — PASS uz prihvaćene canonical iznimke (ADR-0014).**

## Okruženje i ponavljanje

Stvarna aplikacija na `http://localhost:8081`, Docker Compose API/SQL Server/frontend.
Headless Microsoft Edge 152.0.4191.66, Windows, deviceScaleFactor 1, locale hr-HR;
desktop viewport 1536×1024 i mobilni viewport 390×844. PNG-ovi su full-page snimke,
pa visina može biti veća od viewporta. Mobilni pregled nije test fizičkog telefona.

Baza: postojeći odobreni preview račun, jedan učenik, Grupa Orion (1/8), Matematika 7,
bez redovitog rasporeda i termina. Podaci nisu mijenjani. Potvrde arhiviranja i uklanjanja
članstva otvorene su i otkazane; prazan create obrazac zaustavila je native validacija.
Lozinka i session podaci nisu u dokazima. Login je snimljen prije unosa podataka.

Polazišni commit `aea7b8e649754c67433185efa414a80a0c3d260a` plus Phase 1.7 working-tree
promjene; renderirani CSS build naveden je za svaku snimku u [mjerenjima](measurements.json).
Povijesni dokazi 3.1–3.5 nisu prepisani. Nepouzdane probne snimke ugrađenog browsera
zamijenjene su stvarnim završnim Edge snimkama, bez obrade piksela, AI generiranja,
zamjene DOM sadržaja ili lažnih API odgovora.

Ponovljivi test: `frontend/tests/visual/phase17.mjs` iz root direktorija. Zahtijeva
pokrenuti Compose, lokalni Microsoft Edge i postojeći instalirani Playwright.
Postaviti `PLUS5_REVIEW_PLAYWRIGHT` na njegov `index.mjs` te `PLUS5_REVIEW_EMAIL` i
`PLUS5_REVIEW_PASSWORD` u procesnom environmentu; pokrenuti `node frontend/tests/visual/phase17.mjs`.
Nema novog npm dependencyja, hardkodirane lozinke ni spremljenog browser profila.
Račun mora imati postojećeg učenika, aktivnu grupu s članom i bez neaktivnih grupa;
test jasno pada ako taj fixture/preduvjet više ne vrijedi. Ne stvarati podatke automatski.

## Glavni dokazi

| Ekran | Desktop | Mobile | Rezultat |
|---|---|---|---|
| Login | [PNG](login-desktop.png) | [PNG](login-mobile.png) | centrirani logo, čitljiva forma, bez overflowa |
| 3.1 Popis učenika | [PNG](students-desktop.png) | [PNG](students-mobile.png) | canonical sidebar, žute akcije, filtri, tablica i summary |
| 3.1 Kartice | [PNG](student-cards-desktop.png) | [PNG](student-cards-mobile.png) | stvarni isti učenik, status i neutralni progress |
| 3.2 Novi učenik | [PNG](create-desktop.png) | [PNG](create-mobile.png) | sekcionirana forma, sažetak, plavi save, mobile stacking |
| 3.3 Dosje | [PNG](dossier-desktop.png) | [PNG](dossier-mobile.png) | administrativna hijerarhija i neutralne buduće zone |
| 3.4 Uredi učenika | [PNG](edit-desktop.png) | [PNG](edit-mobile.png) | trostupčani desktop, okomiti mobile, archive potvrda |
| 3.5 Grupe | [PNG](groups-desktop.png) | [PNG](groups-mobile.png) | dva panela, statusi, metadata, tabovi i članovi |

Canonical izvori su iz `Za programera - novo/UČITELJ/2.0 Učenici/`:
`2.1 Popis učenika/2.1. Popis učenika.png`, `2.3. Novi učenik/2.3 Novi učenik.png`,
`2.2 Digitalni dosje učenika/2.2 Digitalni dosje učenika.png`,
`2.6 Uredi učenika/2.6 Uredi učenika.png` i ranije spremljeni
[2.7 Grupe canonical](../phase-3.5-groups-canonical.png). Svi su vizualno pregledani uz
DS UI kit i aktualne screen snimke. Login nema zaseban canonical PNG u ovom skupu;
ocijenjen je prema prihvaćenom auth foundationu i korisnikovoj uputi za centrirani logo.

## Rezultati i granice dokaza

| Gate | Rezultat |
|---|---|
| Canonical PNG visual comparison | PASS uz ADR-0014 i postojeće fazne iznimke niže |
| Desktop screenshot comparison | PASS — layout, proporcije, hijerarhija, paleta, kartice i akcije očuvani |
| Mobile adaptation review | PASS — svih 390 px; lokalno pomicanje tablica i dossier akcija |
| Document overflow | PASS — test odbija document width veći od viewporta za svaku snimku |
| Tipkovnica | PASS — ArrowRight/Home mijenjaju group tab; edit linkovi učenika/dosjea dosegljivi na mobitelu |
| Potvrde i forme | PASS — archive/member potvrde otkazane; obavezni create unos validiran bez savea |
| Empty | PASS — stvarni prazan raspored i filter neaktivnih grupa |
| Loading/error/retry/pending | component suite PASS; ne tvrdi se da full-page PNG-ovi prikazuju simulirane network kvarove |
| Browser errors | 0 pageerror događaja u završnom testu |

Dodatni PNG-ovi: `archive-confirmation-*`, `membership-confirmation-*`, `schedule-empty-*`,
`groups-empty-*`, `students-actions-mobile.png`, `dossier-actions-mobile.png` i
`groups-actions-mobile.png`. Mjerenja obuhvaćaju 25 snimki i 8 eksplicitnih interaction zapisa;
provjere dostupnosti mobilnih edit linkova dodatno se izvršavaju kao assertions.

## Ispravka pronađena ovim gateom

Popis učenika imao je document width 1015 px na mobilnom viewportu 390 px.
Skriveni apsolutno pozicionirani naslov stupca Akcije izlazio je iz scroll containera.
`position: relative` na `.student-table-wrap` veže taj element uz lokalnu scroll regiju.
Završna širina je 390 px; tablica ostaje pomična i edit link dosegljiv tipkovnicom.
Browser regression assertion čuva ovo ponašanje. Nije dodan globalni overflow hidden.

## Namjerna odstupanja

- Zadržan 204 px shell, screen palete, postojeće dimenzije/radiusi i ilustrirani logo.
  DS defaulti su opt-in, nisu globalno prebojavanje. Popis svih iznimaka je u
  [alignment contractu](../../DESIGN_SYSTEM_ALIGNMENT.md).
- Dodatni žuti gumb Grupe iznad Dodaj učenika ostaje prema korisnikovoj ranijoj uputi;
  zato je header viši od izvornog 2.1 PNG-a. Broj redaka određuje stvarna baza.
- Canonical demo fotografije, postotci, ocjene, raspored i bilješke nisu fake podaci.
  Inicijali, neutralne zone, odsutnost exporta i disabled buduće akcije ostaju prema
  ranijim faznim contractima. Notification broj nije potvrđen kao stvaran notification sustav.
- Dosje čuva prihvaćeni administrativni raspored umjesto source demo sadržaja; create/edit
  forme imaju ranije prihvaćena odvojena Guardian polja i eksplicitni DeliveryMode.
- Mobile koristi horizontalnu navigaciju, lokalno pomične tablice i dossier akcije.
  Prihvaćeni sticky create footer i modalni overlay na full-page snimci zauzimaju samo
  područje viewporta; to nisu skrivena ili izrezana polja u stvarnom scroll flowu.
- Ovo je acceptance odabranog DS proširenja i regresije postojećih ekrana, nije globalni
  pixel-perfect DS restyle, nova readiness funkcija ni produkcijski accessibility/security certifikat.
