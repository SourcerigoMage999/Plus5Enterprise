# Phase 4.3 — Create session visual acceptance

## Rezultat

**PASS — 2026-09-12.** Stvarni `/schedule/new` Screen 3.3 uspoređen je s canonical
`3.3 Novi termin.png` (SHA-256
`A56898B37D134E22A53F1BF3A3573CC93ECAB7BC8111FC2C09464C5A8167B175`) iz odobrenog
teacher source paketa. Usporedba potvrđuje high visual fidelity, ne matematički pixel-perfect
rezultat.

Provjera je izvedena na rebuilt Docker aplikaciji normalnom lokalnom prijavom i stvarnim
owner-scoped Group/API podacima. Lozinka, cookie i token nisu spremljeni. Headless Edge koristio
je desktop 1536×1024 i mobile 390×844; snimke su full-page, a provjera nije izvršila write.

## Dokazi

- [Desktop](create-session-desktop.png)
- [Mobile](create-session-mobile.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| Canonical layout i hijerarhija | PASS — desktop čuva tri gornja i tri donja područja; mobile ih slaže u jedan stupac |
| Shell i aktivni modul | PASS — stalni PLUS 5 shell, žuti Raspored, profil i notification prostor |
| Osnovni podaci | PASS — group/individual izbor, stvarni searchable context, naziv i napomena |
| Datum, vrijeme i recurrence | PASS — lokalni datum/vrijeme, trajanje, jednokratni izbor i pošteno zaključana grupna recurrence |
| Lokacija | PASS — bez lokacije, owner-scoped fizička lokacija i HTTPS online izbor |
| Sažetak i akcije | PASS — stvarni unos se odmah odražava; Odustani i Spremi termin imaju canonical prioritet |
| Budući contracti | PASS — boje, podsjetnici i obavijesti su vidljivi, ali stvarno disabled |
| Desktop overflow | PASS — document/viewport 1536/1536 |
| Mobile adaptation | PASS — document/viewport 390/390 |
| Browser greške | PASS — 0 `pageerror` događaja |

## Namjerna odstupanja

- PNG prikazuje tjedno ponavljanje grupnog sata. Tekstualni source i zaključani scheduling
  contract određuju da Screen 3.3 za grupu stvara dodatni konkretni Session, dok se redoviti
  raspored grupe uređuje kroz 2.8/2.9; zato je grupna recurrence disabled s objašnjenjem.
- Demo koristi jednu stvarnu grupu i inicijale jer se nisu dodavali lažni zapisi ni avatar
  storage samo radi slike.
- Boja, reminders i notification slanje nisu perzistirani jer njihovi contracti nisu zaključani.
- Postojeći prihvaćeni DS-001 shell, logo, tokeni i responsive navigacija imaju prednost nad
  doslovnim kopiranjem pojedinačnih piksela.

## Ponovljiva provjera

Pokrenuti lokalni Compose, prijaviti se odobrenim demo računom bez spremanja lozinke, otvoriti
Raspored i odabrati `+ Novi termin`. Odabrati stvarnu grupu, unijeti budući datum i valjan
interval, provjeriti live sažetak, disabled grupnu recurrence i buduće kontrole. Usporediti
1536×1024 i 390×844 s canonical PNG-om te potvrditi da nema document overflowa. Funkcionalni
write zasebno pokrivaju API/component i stvarni SQL testovi.
