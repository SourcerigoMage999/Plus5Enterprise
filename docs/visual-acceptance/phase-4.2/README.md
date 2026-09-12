# Phase 4.2 — Session detail visual acceptance

## Rezultat

**PASS — 2026-09-12.** Stvarni `/schedule/:sessionId` Screen 3.2 uspoređen je s canonical
`3.2 Detalji termina.png` (SHA-256
`043B1B05F6E01DA05AB37340A1539B01035F452A98B81A4164E31115585B021C`) iz odobrenog
teacher source paketa. Usporedba potvrđuje high visual fidelity, ne matematički pixel-perfect
rezultat.

Provjera je izvedena na rebuilt Docker aplikaciji normalnom lokalnom prijavom i stvarnim
Session/API podacima. Lozinka, cookie i token nisu spremljeni. Headless Edge koristio je
desktop 1536×1024 i mobile 390×844; snimke su full-page.

## Dokazi

- [Desktop](session-detail-desktop.png)
- [Mobile](session-detail-mobile.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| Canonical layout i hijerarhija | PASS — breadcrumb/header, session sažetak, učenici, tri sadržajne zone, povezano/povijest i info strip slijede source |
| Shell i aktivni modul | PASS — stalni PLUS 5 shell, žuti Raspored, profil i notification prostor |
| Stvarni Session | PASS — grupni context, Program, SchoolGrade, vrijeme, trajanje, lokacija i status dolaze iz owner-scoped API-ja |
| Učenici | PASS — roster koristi stvarno temporalno članstvo; dosje link je aktivan, attendance je pošteno zaključan |
| Budući sadržaji | PASS — plan/Knowledge, materijali, homework i notification ne prikazuju izmišljene podatke |
| Session akcije | PASS — edit/start/cancel/duplicate ostaju disabled do pripadajućih contracta |
| Desktop overflow | PASS — document/viewport 1536/1536 |
| Mobile adaptation | PASS — document/viewport 390/390; roster je lokalna regija 311/496 px |
| Browser greške | PASS — 0 `pageerror` događaja |

## Ispravka nastala provjerom

Prvi mobile run otkrio je da vizualno skriveni heading unutar široke roster tablice doprinosi
document-level scroll širini. Uklonjen je apsolutno pozicionirani element, zadržan pristupačan
header i učvršćen lokalni containment. Završni run je 390/390 bez globalnog overflowa.

## Namjerna odstupanja

- Demo sadrži jednu stvarnu grupu i jednog učenika, dok canonical PNG prikazuje šest. Nisu
  dodani lažni zapisi radi popunjavanja slike.
- Fotografije zamjenjuju inicijali jer storage/avatar contract nije zaključan.
- Tema, cilj, Knowledge Components, materijali i homework prikazuju neutralna prazna stanja
  jer njihovi canonical modeli pripadaju kasnijim fazama.
- Attendance i rezultati održanog sata nisu izvedeni iz planiranog Sessiona; čekaju zaseban
  delivery/evidence model.
- Online meeting URL nije izložen. Edit/start/cancel/duplicate/reminder akcije vizualno su
  prisutne, ali stvarno disabled.
- Povijest koristi stvarne `CreatedAtUtc`, series i cancellation signale; ne izmišlja osobu ni
  audit event koji nije spremljen.
- Postojeći prihvaćeni DS-001 shell, logo, tokeni i responsive navigacija imaju prednost nad
  doslovnim kopiranjem pojedinačnih piksela.

## Ponovljiva provjera

Pokrenuti lokalni Compose, prijaviti se odobrenim demo računom bez spremanja lozinke, otvoriti
Raspored za tjedan s terminom i kliknuti njegovu karticu. Provjeriti povratak na Raspored,
Group/Student linkove, roster, status i disabled granice. Usporediti 1536×1024 i 390×844 s
canonical PNG-om; mobile tablicu pomicati samo unutar imenovane regije. Flow je read-only.
