# Phase 6.3 — Material library visual acceptance

## Rezultat

**PASS — 2026-10-01.** Stvarna `/materials` ruta uspoređena je s canonical
`4.1 Biblioteka materijala.png` (SHA-256
`07677BA32CE7BCBBD7148B02BF903EB7D21667C295F5C2249CEAC33B99813D05`).

Gate je izveden normalnom lokalnom Teacher prijavom i stvarnim
`GET /api/v1/materials` + `GET /api/v1/materials/overview` odgovorima nad SQL Serverom.
Nisu korišteni auth bypass, API interception, DOM mutation ni hardkodirani browser
response. Tijekom capturea nije napravljen business write; lokalna review lozinka i session
podatci nisu spremljeni u Git.

## Dokazi

- [Canonical Screen 4.1](canonical.png)
- [Desktop viewport 1536×1024](material-library-desktop-1536x1024.png)
- [Mobile viewport 390×844](material-library-mobile-390x844.png)
- [Dijeljeni sa mnom — desktop](material-library-shared-desktop.png)
- [No-results state — desktop](material-library-no-results-desktop.png)
- [Mjerenja](measurements.json)

## Usporedba

| Područje | Rezultat |
|---|---|
| PLUS 5 shell i aktivni Materijali modul | PASS |
| Naslov, opis, search i primarna akcija | PASS |
| Mine/shared tabovi | PASS — stvarni odvojeni owner/share API scopeovi |
| Grid/list i sort | PASS — interaktivni, URL-owned presentation state |
| Filter sidebar | PASS — Subject, Program, Grade, Type i Tag iz stvarnog overviewa |
| Type counts i recently added | PASS — server-side autorizirani scope |
| Empty/no-results ponašanje | PASS |
| Desktop overflow | PASS — `scrollWidth/clientWidth = 1536/1536` |
| Mobile adaptation | PASS — `scrollWidth/clientWidth = 390/390` |
| Browser greške / business writeovi | PASS — 0 / 0 |

## Namjerna odstupanja od sourcea

- Canonical koristi ilustrirane thumbnaile. Phase 6.1–6.3 nema zaključan thumbnail/preview
  conversion pipeline, pa UI koristi format previewe i stvarne metadata podatke umjesto
  lažnih slika.
- Canonical prikazuje 48 produkcijskih materijala. Evidence koristi mali lokalni, legalni
  lifecycle fixture; broj nije ugrađen u production kod.
- Filter je stalno vidljiv na desktopu umjesto zasebnog modal/popover gumba; sva tražena
  filter polja i semantika ostaju očuvani.
- `Novi materijal` i item menu ostaju disabled jer import/detail/edit/share write workflowi
  pripadaju fazama 6.4 i 6.6–6.8.
- Profil avatar, notification count i future message podaci nisu izmišljeni; koristi se
  postojeći globalni shell.

## Ponovljiva provjera

Pokrenuti Compose i pripremiti odobreni lokalni Teacher review račun s Clean/Active own i
shared materijalima. Postaviti lokalne `PLUS5_REVIEW_PLAYWRIGHT`, `PLUS5_REVIEW_EMAIL`,
`PLUS5_REVIEW_PASSWORD` i `PLUS5_MATERIAL_LIBRARY_CANONICAL` varijable te pokrenuti
`frontend/tests/visual/phase63.mjs`. Skripta provjerava stvarni login/API, oba ownership
scopea, grid/list kontrole, disabled buduće akcije, no-results, desktop/mobile overflow,
browser greške i odsutnost business writeova.
