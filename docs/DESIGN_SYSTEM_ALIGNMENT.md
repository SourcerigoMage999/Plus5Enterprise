# Phase 1.7 — DS-001 alignment audit

## Status i granica

**DONE / odluka ACCEPTED — 2026-09-11, ADR-0014.** Vlasnik je izričito prihvatio DS-001
semantičku osnovu, canonical screen iznimke i očuvanje shella od 204 px. Pitanje 21 u
[OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) je riješeno. Regression postupak niže obavezan je
završni gate; rezultati se bilježe u zasebnom Phase 1.7 visual acceptance zapisu.

Ulazi: [DS-001 snapshot](source_specs/DESIGN_SYSTEM_DS001.md),
[frontend foundation](FRONTEND_FOUNDATION.md),
[raniji visual acceptance](visual-acceptance/README.md), postojeći CSS i status prikazi.
Backend, domena, API, baza, migracije i autentikacija nisu predmet promjene.

## Pregledani vizualni dokazi

- Izvorni `C:/Users/arodr/Downloads/5/Plus 5 aplikacija/Design System/UI kit.png`.
  SHA-256: `9FB00F3781395A7B6A86789248C83C82B7E1C5B74C0D49404FDD6774D3CCBF81`.
- [Canonical Grupe](visual-acceptance/phase-3.5-groups-canonical.png).
- [Prihvaćeni desktop Grupe](visual-acceptance/phase-3.5-groups-desktop-1536x1024.png).
- [Prihvaćeni mobilni Grupe](visual-acceptance/phase-3.5-groups-mobile-390x844.png).

U početnom auditu sve četiri slike otvorene su i vizualno pregledane. Zapis za 3.1–3.4 je pročitan,
ali njihove slike tada nisu ponovno pregledane. Završni regression pregled ispod uključuje
canonical PNG-ove 3.1–3.5 i nove snimke svih ekrana. Povijesne snimke 3.5 prethode novom
ilustriranom logu iz commita `aea7b8e`; nisu dokaz aktualnog loga ni aktualnog runtimea.
Mobilni dokaz je full-page snimka pri viewportu 390×844.

## Nalazi i prihvaćeno semantičko mapiranje

`--ds-*` defaulti dodani su u `frontend/src/styles/tokens.css`. Opt-in action/card/status
stilovi žive u `frontend/src/ui/designSystem.css` i učitavaju se globalno. Postojeći ekrani
zadržavaju screen klase; Student status paleta sada koristi DS tokene istih vrijednosti.
Zajednički `StatusBadge` koristi se na popisu učenika i grupa s izvornim hrvatskim labelima.

| Područje | DS-001 / UI kit | Postojeći izvor ili kod | Predloženi mapping |
|---|---|---|---|
| Primarna akcija | žuta `#f8b91b` | accent `#ffc51b`; canonical Grupe ima plavu Nova grupa; create/edit save imaju zasebne plave | `--ds-action-primary-bg: #f8b91b`, tekst tamnoplavi; screen iznimke eksplicitno evidentirati |
| Sekundarna akcija | bijela, plavi obrub | različiti lokalni obrubi i plave nijanse | `--ds-action-secondary-bg: #fff`, tekst i obrub `#0f4d80` |
| Brand / naslovi | `#0f4d80` | `#064b96`, `#063c78`, `#003773` i hardkodirane screen nijanse | `--ds-color-brand: #0f4d80`; postojeće tokene ne prepisivati bez odluke i pregleda |
| Upozorenje | narančasta `#e84a1c` | lokalne alert/danger palete | `--ds-color-attention: #e84a1c`; upozorenje i destruktivna akcija ostaju odvojene semantike |
| Neutralne boje | `#2c2c2c`, `#6b7280`, `#e5e7eb`, `#f7f8fa`, `#fff` | tekst `#062355` / `#53647e`, obrub `#dce4ee`, bijeli canvas i lokalne podloge | zasebni text/muted/border/canvas/surface tokeni |
| Tipografija | H1 32/700, H2 20/600, H3 16/600, body 14, caption 12 | ista system font obitelj; 32/14/12 tokeni postoje, bold je 750; H2 i lokalne veličine variraju | semantički heading/body/caption tokeni; 20 px i 700 dodati tek uz odobrenu primjenu |
| Kartica | radius 12, padding 20, shadow `0 4px 12px rgba(15,77,128,.08)` | radius tokeni 10/14/20/28 px; lokalni group radius .8rem i druga sjena | `--ds-card-radius`, `--ds-card-padding`, `--ds-card-shadow` |
| Navigacija | 240 px, collapsed 72 px | prihvaćeni shell 204 px; mobilna horizontalna navigacija | zadržati postojeći shell kao evidentiranu iznimku do zasebnog pregleda |
| Ikone | outline 2 px, zaobljeno | postojeći SVG/tekstualni i CSS prikazi | jedan presentation contract, bez nove icon biblioteke |
| Tablice | header `#f1f5f9`, lijevo poravnanje, približno 8 px razmaka | group header `#f7f9fc`, lokalni padding; mobilni lokalni scroll | `--ds-table-header-bg`; očuvati imenovanu scroll regiju i dostupnost Akcija |

Samo zamjena `tokens.css` nije dovoljna: StudentCreatePage.css, StudentEditPage.css,
StudentListPage.css, GroupListPage.css i App.css sadrže lokalne boje, dimenzije i sjene.
Takva zamjena proizvela bi djelomičan restyle s neujednačenim ekranima.

## Nesporni contract komponenti

- Glavna radnja mora imati jasan tekst i vizualni prioritet. Link za navigaciju ostaje
  link, a radnja button. Disabled/pending stanje mora onemogućiti stvarnu akciju;
  boja sama nije kontrola pristupa ni zaštita od dvostrukog slanja.
- Sekundarne radnje imaju niži vizualni prioritet. Destruktivna radnja zadržava zasebnu
  danger semantiku i postojeću eksplicitnu potvrdu; ne uvodi se fizičko brisanje.
- Status prikaz prima domenski status, prikazuje hrvatski tekst i dekorativni indikator.
  Statični badge ne dobiva automatski `role=status`; live obavijest je zasebna potreba.
- Student `active/on_hold/inactive` i Group statusi nisu readiness. Ne smiju se preimenovati
  u Spreman/Potrebna provjera/Potrebna intervencija iz UI kita. Knowledge gate ostaje otvoren.
- Postoje lokalni `StudentStatusBadge` u StudentListPage.tsx
  i lokalni `Status` u GroupListPage.tsx. Dijeljenje presentation komponente
  smije očuvati domenske labele; ne spajati domenske enumove zbog istih boja.
- Kontrast provjeriti za stvarne parove teksta/podloge, focus i enabled kontrole;
  brand swatch sam po sebi nije dokaz pristupačnosti. Boja nije jedini signal.

## Prihvaćeni prioritet i popis iznimaka

Prihvaćeno je **semantičko proširenje uz canonical screen iznimke**: DS-001 daje default
tokene i komponente za buduće ekrane; canonical PNG određuje raspored i može zadržati
plavu glavnu akciju kao dokumentiranu iznimku. Prihvaćeni shell ostaje 204 px i zadržava
mobilnu navigaciju. Postojeći ekrani prelaze na zajedničke tokene samo uz novi pregled.

| Potrošač / CSS | Prihvaćena iznimka |
|---|---|
| App.css / tokens.css | 204 px shell, postojeći brand gradient, horizontalna mobilna navigacija; aktualni ilustrirani logo |
| Auth u App.css | postojeća plava prijava, centrirani logo, širina/radius auth kartice; auth nema canonical screen PNG u ovom gateu |
| StudentListPage.css | žuta akcija `#ffc51b`, postojeća gustoća tablice, kartice i responsive layout; status tokeni identičnih vrijednosti |
| StudentCreatePage.css | canonical plavi save `#075f9f`, sekcionirana forma, desni sažetak i lokalne dimenzije |
| StudentDossierPage.css | prihvaćeni administrativni layout i lokalna paleta; neutralni Knowledge/Evidence blokovi |
| StudentEditPage.css | canonical plavi save `#1768ac`, trostupčani layout; arhiviranje zadržava danger potvrdu |
| GroupListPage.css | plavi primary iz canonical 2.7, paneli 44:56 i gušći status prikaz; lokalna status paleta očuvana |

Nove iznimke moraju navesti canonical referencu i proći desktop/mobile pregled; novi
ekran ne kopira proizvoljne hex vrijednosti iz drugog screena. Globalni DS restyle nije
odabrana strategija. Popis iznimaka ne mijenja ranije dokumentirane funkcionalne granice.

## Primjena komponenti

`StatusBadge` prima `label`, `tone` (`positive`, `warning`, `neutral`) i opcionalni
`className`. Bez klase koristi DS izgled; canonical potrošač izričito predaje screen
klasu. Domenske labele i odabir tona ostaju u featureu. Komponenta nema evente, role,
live region, readiness izračun niti API pristup. Tekst je dovoljan signal bez boje.

Za novu radnju koristiti native `button` s `ds-action ds-action--primary/secondary/danger`;
za navigaciju stvarni link. Disabled je native button atribut, pending ga također postavlja.
Nemoj označavati link disabled samo CSS klasom. Hover podcrtava label bez promjene
kontrasta; focus koristi vidljiv plavi ring. Danger koristi tamni crveni tekst na svijetloj
podlozi; DS narančasta nije automatski tekst na bijelom. Kartice koriste `ds-card`.

## Ponovljivi regression postupak nakon odluke

1. Zapisati odabrani prioritet u Decision Log i povezati pitanje 21 s odlukom.
2. Implementirati odabrane tokene/komponente; pregledati sve potrošače i lokalne overrideove.
3. Pokrenuti frontend testove, lint i build. Testirati korisnički status tekst,
   dostupnost akcija, disabled/pending ponašanje i focus gdje se mijenjaju komponente.
4. U stvarnoj aplikaciji provjeriti login s centriranim novim logom, shell te ekrane
   3.1–3.5 na 1536×1024 i 390×844; svaki promijenjeni ekran usporediti s njegovim
   canonical PNG-om i DS UI kitom. Zabilježiti commit, browser, viewport i stvarno stanje.
5. Provjeriti loading/empty/error, forme, potvrde, tabove, tipkovnicu, lokalni scroll i
   odsutnost document overflowa. Koristiti odobrene demo podatke bez secreta u snimkama.
6. Spremiti nove snimke i tablicu odstupanja, bez prepisivanja povijesnih dokaza.
   Odvojeno navesti canonical comparison, desktop comparison i mobile adaptation rezultat.
7. Tek nakon izvršenih provjera zaključati 1.7 i otvoriti 3.6 Create group.

## Izvršene provjere

Početni docs-only audit proširen je implementacijom i obaveznim regression gateom nakon
vlasnikove odluke. 29 frontend testova, lint/typecheck/build, Docker Compose i stvarna
prijava prolaze. Snimljeno je 25 desktop/mobile prikaza uz 8 interaction zapisa i 0 browser
pageerror događaja. Mobilni overflow popisa učenika ispravljen je vezanjem skrivenog
naslova stupca uz lokalnu scroll regiju. Rezultati, dokazi i odstupanja su u
[Phase 1.7 visual acceptanceu](visual-acceptance/phase-1.7/README.md).

Izračun kontrasta novih default parova (sRGB relativna luminancija): primary 4.99:1,
secondary 8.77:1, danger 7.32:1, positive status 6.65:1, warning 6.16:1, neutral 5.49:1.
To provjerava navedene parove teksta/podloge, ne tvrdi potpunu accessibility certifikaciju
svih naslijeđenih screen paleta ili disabled kontrola.
