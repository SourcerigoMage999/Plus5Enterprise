# Phase 9.7 visual acceptance

## Status

**PASS — 2026-11-06.**

Canonical source: `5.4 Odabir aktivnosti/5.4 odabir aktivnosti  knowledge blocka.png`.

- canonical SHA-256: `BBFB0857F98FB0797B4516CC8242CE693A82736D078D092D58CD3D6225CE3976`;
- copied unchanged as `canonical.png`;
- desktop evidence: `activity-selection-desktop-1536x1024.png`;
- mobile evidence: `activity-selection-mobile-390x844.png`.

## Runtime flow

Evidence je nastao kroz stvarni lokalni browser/runtime:

1. stvarna Teacher cookie prijava bez auth bypassa;
2. stvarni Phase 9.4 Draft create;
3. stvarni Phase 9.5 baseline Apply;
4. stvarni Phase 9.7 add istog KB-005 radi duplicate-warning statea;
5. stvarni SQL-backed candidate GET u desktop i mobile captureu.

Nije korišten API interception, mock DOM, hardkodirani screenshot state ili browser-side
pedagoška logika. Setup je napravio tri eksplicitna business writea; screenshot capture nakon
konačnog statea nije napravio dodatni write.

## Vizualna usporedba

PASS:

- trajni PLUS 5 shell i aktivna `Priprema sata` navigacija;
- breadcrumb, Screen 5.4 naslov i participant/goal/KC/time context;
- canonical filter + candidate list + preview raspored na desktopu;
- Published version/duration/applicability metapodaci i jasan selected state;
- vidljivo upozorenje `Već u planu` bez blokiranja dopuštenog ponavljanja;
- mobile single-column prikaz relevantne candidate i preview zone;
- nema globalnog horizontalnog overflowa ili preklapanja kontrola.

Namjerna odstupanja od canonicala:

- `Aktivnosti` tab prikazuje no-catalog stanje jer ActivityTemplate domain nije zaključan;
- nema proizvoljnih demo activity redaka, readiness scorea ni AI preporuke;
- exact Published version i duplicate warning prikazani su eksplicitnije radi verzijske i
  authoring sigurnosti.

## Mjerenja

- desktop: `scrollWidth 1536 <= clientWidth 1536`;
- mobile: `scrollWidth 390 <= clientWidth 390`;
- browser/page errors tijekom capturea: `0`;
- business writeovi tijekom capturea: `0`;
- candidate API: HTTP `200` na oba viewporta.

Strojno čitljivi detalji su u `measurements.json`.
