# Phase 6.8 — Material sharing, visibility and permissions

## Status

**FINAL LOCKED — odobreno 2026-10-09.**

## Cilj faze

Implementirati zaključani Phase 6.1 Private/Shared i eksplicitni Teacher View/Use permission
contract bez proširenja na edit, re-share, javne linkove ili buduće Lesson flowove.

## Implementirano

- owner-only sharing workspace i Material detail `Dijeli` akcija;
- owner-scoped Active/current/Clean GET query;
- CSRF-zaštićeni PUT s exact-email active Teacher resolutionom;
- enumeration-safe generički recipient rezultat i zaseban sharing PUT rate limit;
- Private/Shared vidljivost i potpuni desired-state View/Use grant skup;
- self-share, duplicate, unknown/deactivated recipient i Private-with-grants validacija, bez
  otkrivanja statusa recipient računa;
- atomski add/change/remove grant flow te Private transition koji uklanja grantove;
- optimistic concurrency preko `Material.RowVersion`;
- shared recipient nema edit/re-share/sharing workspace;
- loading, error/retry, private/shared empty, saving, success i dirty-navigation UI stanja;
- stvarni SQL runtime, security i desktop/mobile visual evidence;
- milestone security patch `source-map-js 1.2.1 → 1.2.2` samo u npm lockfileu.

## Ključne odluke

- owner identitet dolazi samo iz autentificirane serverske sesije;
- recipient se navodi točnom e-mail adresom; unknown/deactivated/self/nedopušten recipient vraća
  isti neutralni rezultat pa user directory/enumeration nije uveden;
- sharing PUT je zasebno ograničen na 10 pokušaja u minuti po izvornoj IP adresi;
- grant je samo `View` ili `Use`; nema Edit, re-share, owner transfer, public/link/org scopea;
- Material share nema implicitni Student/Group/Evidence/readiness/Lesson pristup;
- povratak na Private i uklanjanje grantova jedna su transakcija;
- postojeći Phase 6.1 SQL triggeri dovoljni su; nema redundantne nove migracije;
- zaseban sharing canonical PNG ne postoji, pa Screen 4.1 služi samo kao Materials/shell baseline.

## API promjene

- `GET /api/v1/materials/{materialId}/sharing`;
- `PUT /api/v1/materials/{materialId}/sharing`.

Stabilni kodovi su `material_share_invalid_request`, `material_share_invalid_recipient`,
`material_not_found`, `concurrency_conflict` i `invalid_csrf_token`. Contract je aditivan.

## Domain / database

Nema novog domenskog entiteta ni schema promjene. Implementacija koristi postojeće
`Material.Visibility`, `MaterialShare`, View/Use enumove, `Material.RowVersion`, unique/composite
ključeve i Phase 6.1 triggere. EF model drift provjera je PASS; migrations runtime javlja da je
baza već ažurna.

## Test evidence

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Ciljani MaterialSharing application/query/API security testovi | PASS — 4/4 |
| Stvarni SQL sharing/concurrency/integrity testovi | PASS — 2/2, bez skipa; unknown/deactivated/self daju isti generic outcome |
| Backend regression (.NET 10 Linux SDK) | Pre-M-01 baseline PASS — 218/218; nije ponavljan prema uskom review zahtjevu |
| Architecture | PASS — 4/4 |
| Frontend Materials ciljano | PASS — 13/13; neutralni recipient error potvrđen |
| Frontend regression | Pre-M-01 baseline PASS — 82/82; nije ponavljan prema uskom review zahtjevu |
| Frontend lint/typecheck/production build | PASS |
| `.NET format --verify-no-changes` | PASS |
| EF model drift / migration runtime | PASS — nema model promjene; baza ažurna |
| Docker image/runtime/health | PASS |
| Non-root runtime | PASS — API `1654(app)`, frontend `101(nginx)` |
| Security runtime | PASS — anonymous `401`, missing CSRF `400`, owner-only workspace, generic recipient `400`, sharing PUT `429` nakon 10 pokušaja/min |
| Visual gate | PASS — 1536×1024 i 390×844, bez overflowa/browser grešaka |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm vulnerability audit | PASS — 0 ranjivosti nakon kompatibilnog transitive patcha |

## Izmijenjene/dodane cjeline

- Application: sharing workspace/command/result contracti;
- Infrastructure: owner query i transakcijski sharing service;
- API: sharing GET/PUT endpointi i DI/route registracija;
- frontend: sharing route/page/styles/API, detail akcija i error mapping;
- testovi: application/query, stvarni SQL i Materials UI;
- dependency lock: patchani transitive `source-map-js`;
- dokumentacija: `MATERIAL_SHARING.md`, ROADMAP, summary i visual evidence.

## Scope namjerno nije implementiran

- recipient edit/re-share i ownership transfer;
- public link, marketplace, organization/team permissions i user directory;
- notification/audit history permission promjena;
- binary read/preview, duplicate i Lesson Builder/Board integracija;
- Phase 7 slide model, content editor, Quick Check, autosave, preview/export i AI assistance.

## Poznati rizici / otvorena pitanja

Nema blockera za Phase 6.8 review. Buduća uporaba `Use` granta mora se autorizirati u vlastitom
Lesson/duplicate use caseu; ova faza ne implementira taj budući write.

## Točna početna točka za sljedeću fazu

Nakon Phase 6.8 FINAL LOCK-a, prva nezavršena stavka je Phase 7.1 — tehnički dizajn
verzioniranog slide document modela. Ne uvoditi slide persistence ili editor prije tog contracta.
