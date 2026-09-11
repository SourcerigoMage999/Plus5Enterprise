# Phase 1.7 — DS-001 token and component alignment

## Status i datum

**DONE — 2026-09-11.** Vlasnik izričito prihvatio ADR-0014 i obavezni regression gate.

## Implementirano

- DS-001 semantički tokeni i opt-in primary/secondary/danger, card i status stilovi.
- Zajednički StatusBadge na popisu učenika i grupa; feature čuva domensku labelu i ton.
- Dokumentirane canonical iznimke, postojeći 204 px shell i mobilna navigacija.
- Ispravljen mobilni overflow: skriveni naslov Akcije vezan je uz lokalni table scroll container;
  document width na 390 px više nije 1015 px. Browser assertion čuva ispravku.
- Ponovljivi browser regression test i 25 stvarnih PNG dokaza s mjerenjima.

## Namjerno nije implementirano

Globalni restyle, nova UI biblioteka, ekran 3.6 i Knowledge/readiness statusi.
Demo učenik i grupa nisu mijenjani; potvrde su otkazane, prazan create nije spremljen.

## Promijenjene / dodane datoteke

| Datoteka | Vrsta | Razlog |
|---|---|---|
| docs/DESIGN_SYSTEM_ALIGNMENT.md | added | mapping, iznimke, component contract i gate |
| docs/DECISION_LOG.md | changed | Accepted ADR-0014 |
| docs/OPEN_QUESTIONS.md | changed | riješeno pitanje 21 |
| docs/ROADMAP.md | changed | 1.7 DONE uz izvršene dokaze |
| docs/FRONTEND_FOUNDATION.md | changed | pravila DS primjene |
| docs/source_specs/DESIGN_SYSTEM_DS001.md | changed | projektni resolution odvojen od izvornog snapshot sadržaja |
| docs/DOCUMENTATION_MANIFEST.md | changed | indeks contracta |
| docs/visual-acceptance/README.md | changed | novi gate |
| docs/visual-acceptance/phase-1.7/ | added | README, measurements.json i 25 PNG-ova |
| docs/summaries/PHASE_1.7_DESIGN_SYSTEM_ALIGNMENT_SUMMARY.md | added | handoff |
| frontend/src/styles/tokens.css | changed | DS defaulti |
| frontend/src/ui/designSystem.css | added | opt-in stilovi |
| frontend/src/ui/StatusBadge.tsx | added | zajednički statični status |
| frontend/src/index.css | changed | učitavanje DS stilova |
| frontend/src/students/StudentListPage.tsx | changed | status komponenta |
| frontend/src/students/StudentListPage.css | changed | status mapping i overflow fix |
| frontend/src/groups/GroupListPage.tsx | changed | status komponenta |
| frontend/tests/StatusBadge.test.tsx | added | labele bez readiness/live-region pretpostavki |
| frontend/tests/visual/phase17.mjs | added | stvarni browser regression |

Radni screenshot helper u ignoriranom TestResults nije deliverable; ponovljiva verzija
je u frontend/tests/visual. Nema novog package dependencyja.

## Domain / database / API / security

Nema novih entiteta, migracija, backfilla, endpointa ili breaking API promjena.
Status komponenta je presentation-only. Postojeći auth/validation boundary ostaje očuvan.
Preview lozinka korištena je samo za lokalnu prijavu; nije spremljena u nove datoteke.

## Testovi

| Provjera | Rezultat |
|---|---|
| npm test | PASS — 29 testova / 9 datoteka |
| npm run lint | PASS |
| npm run build (TypeScript + Vite) | PASS |
| Docker Compose build/up i završni frontend rebuild | PASS |
| node frontend/tests/visual/phase17.mjs | PASS — 25 snimki, 8 interaction zapisa, 0 pageerror |
| Canonical PNG comparison / desktop / mobile | PASS uz ADR-0014 i dokumentirane iznimke |
| Mobile overflow i dosegljivost akcija | PASS; regresija reproducirana prije CSS ispravke |
| Loading/error/retry/pending | frontend suite PASS; ne simulira se u prezentacijskim slikama |
| DS default text/background kontrast | svi provjereni parovi iznad 4.5:1 |
| Backend testovi / migracije | nisu ponavljani; nema promjene backend/schema koda |

Početni browser test pao je na pogrešnom selectoru testnog alata, zatim otkrio stvarni
mobilni overflow. Ispravljeni su selector i CSS. Završni test čeka renderirane kartice/tabove,
ne samo URL; nema proizvoljnih sleep poziva. Dokazi i ograničenja su u
[visual acceptance zapisu](../visual-acceptance/phase-1.7/README.md).

## Self-review

- [x] Scope je jedna podfaza i prihvaćena odluka je zapisana.
- [x] Build, relevantni testovi i stvarni visual regression prolaze.
- [x] Administrativni statusi nisu zamijenjeni readiness signalima.
- [x] Native disabled/focus i postojeće sigurnosne granice očuvani su.
- [x] Migracije/API promjene nisu primjenjive.
- [x] Dokumentacija razlikuje povijesne dokaze i aktualni pregled s novim logom.

## Arhitekturne odluke i otvorena pitanja

ADR-0014 Accepted. Pitanje 21 riješeno; ostali product/security/Knowledge gateovi ostaju
svojim fazama. Nema otvorenog blokera za završetak 1.7.

## Rizici i granice

Opt-in defaulti ne prebojavaju sve postojeće ekrane; canonical iznimke su namjerne.
Browser test zahtijeva postojeći lokalni demo fixture i instalirani Edge/Playwright.
PNG-ovi su full-page, ne fizički mobilni uređaj. Raspored je provjeren u praznom stanju.
Ovo nije novi production security/performance/accessibility lock.

## Točna početna točka

Sljedeća je **3.6 — Screen 2.8 Create group**: pročitati Group foundation, source 2.8 i
canonical PNG te primijeniti DS default uz dokumentirane iznimke. Phase 3.6 nije započeta.
Commit/push čekaju zasebnu korisnikovu uputu.
