# Phase 4.1 — Calendar visual acceptance

## Rezultat

**PASS — 2026-09-12.** Stvarni `/schedule` Screen 3.1 uspoređen je s canonical
`3.1 Početna.png` (SHA-256 `1628085A7F4ED8963BC6EC2A00CF4EF0ADBA1C211E97C4A81D452DE176F3F603`)
iz odobrenog teacher source paketa. Usporedba potvrđuje high visual fidelity, ne tvrdi
matematički pixel-perfect rezultat.

Provjera je izvedena na rebuilt Docker aplikaciji s normalnom lokalnom prijavom i stvarnim
Session/API podacima. Lozinka, cookie i token nisu spremljeni. Headless Edge koristio je
desktop 1536×1024 i mobile 390×844; snimke su full-page, pa mobilna visina prelazi viewport.

## Dokazi

- [Desktop — tjedan](calendar-week-desktop.png)
- [Desktop — dan](calendar-day-desktop.png)
- [Mobile — tjedan](calendar-week-mobile.png)
- [Mobile — dan](calendar-day-mobile.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| Canonical layout i proporcije | PASS — centralni kalendar i desna zona, compact toolbar i donji info strip slijede source |
| Shell i aktivni modul | PASS — stalni PLUS 5 shell, žuti Raspored, profil i notification prostor |
| Naslov i akcije | PASS — `3.1 Raspored`, source podnaslov, Tjedan/Dan, Danas i dominantni Novi termin |
| Calendar navigacija | PASS — prethodni/sljedeći period, Monday-first week, day headers i 08:00+ time grid |
| Stvarni Sessioni | PASS — Group Session kartica koristi canonical vrijeme, kontekst, lokaciju i članstvo; dva termina pronađena su u provjerenom mjesečnom rasponu |
| Desna zona | PASS — mini-kalendar, tri filtera, owner-only signal, sažetak i podsjetnici |
| Metrike | PASS — dvosmisleni source broj razdvojen je na `Jedinstvenih učenika` i `Planiranih dolazaka` uz ostale metrike |
| Week/day flow | PASS — oba prikaza koriste isti owner-scoped API i URL stanje |
| Buduće akcije | PASS — Novi termin, detalj, izvještaj i notification centar stvarno su disabled do svojih faza |
| Desktop overflow | PASS — document/viewport širina 1536/1536 |
| Mobile adaptation | PASS — document/viewport 390/390; tjedni kalendar je lokalna pomična regija 303/768 px |
| Browser greške | PASS — 0 `pageerror` događaja |

## Ispravke nastale provjerom

- Stvarna SQL Server provjera uhvatila je neprevodiv `OrderBy` nad projekcijskim recordom;
  sortiranje je premješteno prije projekcije i završni SQL test prolazi.
- Mobile day assertion više se ne oslanja na CSS-skriven tekst, nego na stvarno aktivni
  `aria-pressed` control i day-grid.
- Tjedni grid ima lokalni horizontalni scroll na mobitelu, dok cijeli dokument ostaje bez
  horizontalnog overflowa.

## Namjerna odstupanja

- Stvarni demo podaci imaju jednu grupu i kasni termin, a canonical PNG prikazuje više
  oglednih grupa i termina. Nisu dodani lažni Sessioni radi popunjavanja slike.
- Grid se po potrebi proširuje nakon 20:00 kako stvarni kasni termin ne bi bio skriven.
- `+ Novi termin` zadržava canonical dominantan vizual, ali je stvarno disabled jer create
  pripada Phase 4.3. Klik na termin je disabled do Phase 4.2 granice.
- `Prikaži samo moje termine` je stalno uključen i zaključan; backend ownership nije opcionalni
  klijentski filter.
- Summary prikazuje precizne jedinstvene učenike i planirane dolaske umjesto jednog
  dvosmislenog source broja.
- Postojeći prihvaćeni DS-001 shell, logo, tokeni i responsive mobilna navigacija imaju
  prednost nad doslovnim kopiranjem pojedinačnih piksela.
- Podsjetnici su dva sljedeća stvarna Sessiona. Otvaranje detalja i notification centar nisu
  simulirani bez pripadajućih contracta.

## Ponovljiva provjera

Pokrenuti lokalni Compose, prijaviti se odobrenim demo računom bez spremanja lozinke i
otvoriti Raspored. Provjeriti tjedan/dan, Danas, prethodni/sljedeći period, mini-kalendar i
filtre. Usporediti 1536×1024 i 390×844 s canonical PNG-om; na mobitelu fokusirati i vodoravno
pomicati samo calendar regiju. Flow je read-only i ne radi business write.
