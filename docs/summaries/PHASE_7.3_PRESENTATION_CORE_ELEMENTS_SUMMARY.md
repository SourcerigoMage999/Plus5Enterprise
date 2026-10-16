# Phase 7.3 — Presentation core content elements

## Status

**FINAL LOCKED — odobreno 2026-10-16.**

## Cilj faze

Dodati četiri zaključana osnovna elementa u stvarni Phase 7.2 Presentation Editor, uz typed
payload registry, private Clean image asset lifecycle, Draft-only SQL zaštitu, owner-scoped
API, optimistic concurrency i canonical desktop/mobile dokaz.

## Implementirano

- version-bound `PresentationElement` i `PresentationAsset` domain/persistence modeli;
- closed v1 payload registry za `TEXT`, `IMAGE`, `SHAPE` i `TABLE` bez raw HTML/CSS/scripta;
- geometry, contiguous z-index te per-element/per-slide/per-document limiti;
- owner-only create/update/delete/image upload i Clean asset read endpointi;
- PNG/JPEG/WebP signature provjera, quarantine, stvarni fail-closed ClamAV scan, SHA-256 i
  private object-storage promote;
- Draft-only SQL triggeri i same-version restrictive FK-ovi;
- slide duplicate s elementima te Material restore s kopiranim elementima i assetima;
- responsive editor s persisted core toolbarom, sigurnim renderiranjem, geometry/payload
  property formama i eksplicitnim layer navigatorom;
- canonical desktop/mobile visual runner nad stvarnim loginom, API-jem, SQL-om, S3 storageom i
  malware scannerom.

## Namjerno nije implementirano

- teaching-specific elementi iz 7.4;
- Quick Check/Task i Knowledge mapping iz 7.5–7.6;
- autosave/recovery iz 7.7;
- preview, publish, package i export iz 7.8;
- AI, collaboration i PPTX conversion.

## Domain / database promjene

Migracija `20261011131859_AddPresentationCoreElements` dodaje `PresentationAssets` i
`PresentationElements`, composite same-version FK-ove, unique z-index, bounded CHECK
constrainte i Draft/PRESENTATION triggere. SQL error `51243` štiti asset snapshot, a `51244`
element snapshot. Nema seeda ni business backfilla.

## API / frontend promjene

Presentation route sada učitava i mijenja stvarne elemente i assete uz isti document
rowversion authority. Svi writeovi imaju CSRF, bounded body i Teacher-scoped rate limit.
Frontend ne računa niti duplicira domain pravila, ne koristi `innerHTML`, ne prikazuje storage
interne podatke i ne simulira buduće kontrole.

Tijekom stvarnog browser flowa otkriven je i ispravljen race: mutation UI više ne oslobađa
sljedeći write prije nego što preuzme novi server rowversion, pa uzastopne legalne promjene ne
završavaju lažnim stale `409` odgovorom.

## Visual acceptance

Canonical `4.3 P3.png` uspoređen je sa stvarnim editorom na 1536×1024 i 390×844. Capture koristi
stvarni login i persistence, četiri stvarno spremljena tipa, Clean image stream i property/layer
interaction. Document overflow, browser error, API interception, auth bypass, DOM mutation i
business write tijekom samog capturea nisu zabilježeni.

Canonical teaching toolbar, Quick Check, tema/layout, autosave, preview i publish kontrole nisu
lažno aktivirane jer pripadaju budućim fazama. Responsive mobile prikaz namjerno slaže navigator,
canvas i property panel vertikalno.

## Validacija

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Targeted domain/API/security tests | PASS — 9/9 |
| Targeted stvarni SQL tests | PASS — 2/2 |
| Full backend regression | PASS — 233 PASS, 54 očekivana SQL opt-in skipa |
| Architecture tests | PASS — 4/4 |
| Frontend targeted tests | PASS — 5/5 |
| Full frontend regression | PASS — 88/88 |
| Frontend lint/typecheck/production build | PASS |
| .NET format | PASS |
| EF pending model changes | PASS — nema drifta |
| Idempotent migration script | PASS — migration guard i oba triggera prisutni |
| Migration/SQL/runtime | PASS — migracija primijenjena, live/ready/frontend HTTP 200 |
| Docker non-root | PASS — API `1654(app)`, frontend `101(nginx)` |
| Visual acceptance | PASS — 1536×1024 i 390×844, bez overflowa/errora/capture writeova |
| Dependency vulnerability audit | N/A — dependency graph nije promijenjen |

Stvarna SQL 2/2 provjera izvršena je kroz Compose mrežu na SQL Serveru i obuhvaća upgrade,
ponovljivi migration apply, same-version/geometry/asset constrainte te Active snapshot SQL
greške `51243` i `51244`. Full regression namjerno zadržava ostale SQL klase kao opt-in jer
njihovi ranije FINAL LOCKED invarianti nisu promijenjeni.

Frontend production build prolazi; postojeći Vite advisory za glavni bundle veći od 500 kB
nije build warning/error gate ni nova Phase 7.3 regresija.

## Self-review

- [x] implementirana je samo Phase 7.3 granica
- [x] closed registry i server-side validacija sprječavaju executable payload
- [x] image asset ostaje private, version-bound i Clean-only
- [x] ownership, CSRF, rate limit, concurrency i SQL snapshot zaštita pokriveni su
- [x] duplicate/restore čuvaju version boundary
- [x] canonical desktop/mobile usporedba i opravdana odstupanja dokumentirani su
- [x] credentiali, storage ključevi i checksumovi nisu spremljeni u Git

## Arhitekturne odluke

Nema novog ADR-a. Faza primjenjuje ADR-0026 i ne mijenja njegov typed-element, asset,
instructional/Evidence ni publish-package contract.

## Poznati rizici / otvorena pitanja

Nema otvorenog blockera za Phase 7.3. Napredni element tipovi, assessable runtime, autosave i
publish ostaju eksplicitno odgođeni na svoje ROADMAP faze.
