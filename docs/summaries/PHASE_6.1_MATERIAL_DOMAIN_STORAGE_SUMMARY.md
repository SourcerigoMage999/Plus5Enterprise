# Phase 6.1 — Material domain model & storage strategy summary

## Status

**FINAL LOCKED — odobreno 2026-09-29.** SA review odobrio je domain/persistence
implementaciju, storage/security contract i stvarni SQL migration/lifecycle evidence bez
dodatnih promjena.

## Implementirano

- `Material` sa single-Teacher ownershipom, Private/Shared visibilityjem, arhiviranjem i
  SQL Server `rowversion` concurrencyjem;
- immutable `MaterialVersion` Draft → Active → Superseded lifecycle i restore-as-new-version;
- version-bound `MaterialFile` bez binary bloba, s opaque server keyem, SHA-256 i fail-closed
  upload/scan lifecycleom;
- zaključani PDF/DOCX/PPTX/MP4/ZIP size/MIME policy i ZIP security limits;
- `MaterialShare` View/Use grantovi bez ownership transfera, self-sharea ili Edit permissiona;
- application portovi za private object storage, file validation, malware scan i opcionalnu AI
  analizu;
- EF konfiguracije, migracija, DB CHECK/index/FK zaštita i SQL triggeri za lifecycle,
  current/active/clean konzistentnost i immutable history;
- stvarni SQL upgrade i integrity regression testovi.

## Očuvane odluke

- Cloudflare R2/S3-compatible private object storage je initial production topology, ali
  business sloj ostaje provider-neutralan;
- binary nije u SQL-u ni na persistentnom API disku;
- samo `Clean` file može postati usable; quarantine nema download/share/preview/AI pristup;
- Material share ne daje Student/Group/Evidence/readiness pristup;
- AI je optional suggestion source nakon clean scana i nikad canonical authority;
- historical MaterialVersion ostaje vezan uz exact file; budući Task history mora koristiti
  exact TaskVersion.

## Validation

- backend Release build: PASS, 0 warninga i 0 grešaka;
- ciljani domain/model testovi: PASS, 13/13;
- fokusirani stvarni SQL suite: PASS, 16/16 ukupno bez skipova;
- migration upgrade, repeated apply i empty-catalog provjera: PASS;
- puni backend/API regression: PASS, 190 passed / 40 opt-in SQL skipped;
- architecture testovi: PASS, 4/4;
- `dotnet format --verify-no-changes`: PASS;
- EF model drift: PASS, nema pending model changes;
- idempotent migration script generation: PASS;
- NuGet audit: PASS, 0 poznatih ranjivosti;
- transitive `undici` podignut je na sigurnu `8.11.2`; `npm ci` i npm audit: PASS,
  0 ranjivosti;
- frontend regression: PASS, 70/70; lint, typecheck i production build: PASS;
- aktualni Docker API/migrations/frontend image build: PASS; migration container exited 0;
- Docker health: API live 200, ready 200, frontend 200;
- non-root runtime: API UID 1654, frontend `nginx`;
- visual acceptance: N/A — faza nema UI.

## Self-review

- [x] nema binary sadržaja u EF modelu
- [x] owner nije request-controlled polje nijednog API-ja; API još nije uveden
- [x] object key ne koristi originalni filename
- [x] format i scan lifecycle imaju domain i DB zaštitu
- [x] Active/Superseded metadata/file history ne može se mijenjati in-place
- [x] activation zahtijeva Clean file i sinkronizira current pointer
- [x] share je eksplicitan, bounded i ne prenosi ownership
- [x] nema Task, AI provider, upload endpoint ili UI scope creepa
- [x] migracija nema seed ni backfill

## Ključne datoteke

- `docs/MATERIAL_FOUNDATION.md`
- `backend/src/Plus5.Domain/Materials/*`
- `backend/src/Plus5.Application/Materials/MaterialStorageContracts.cs`
- `backend/src/Plus5.Infrastructure/Persistence/MaterialPersistenceConfigurations.cs`
- `backend/src/Plus5.Infrastructure/Persistence/Migrations/20260929143813_AddMaterialStorageFoundation.cs`
- `backend/tests/Plus5.Api.Tests/Materials/*`
- `frontend/package-lock.json`

## Scope koji nije implementiran

Nema 6.2 mappinga, Library/detail/import/edit UI-ja, upload/download endpointa, R2 ili ClamAV
deployment adaptera, AI suggestion persistence/UI-ja, Task domene, preview conversiona,
transcodinga, quotas/billinga, retention cleanupa ni Lesson Builder integracije.

## Blockeri

Nema otvorenog blockera unutar Phase 6.1 foundation scopea. Deployment adapteri i operativni
provider/secrets dolaze uz upload/deployment fazu, a nisu uvjet za provider-neutralni domain i
persistence foundation.
