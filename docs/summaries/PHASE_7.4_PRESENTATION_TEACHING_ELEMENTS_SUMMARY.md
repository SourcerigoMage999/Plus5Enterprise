# Phase 7.4 — Presentation teaching-specific elements

## Status

**FINAL LOCKED — odobreno 2026-10-18.**

## Implementirano

- zatvoreni v1 payloadi za `RULE`, `EXAMPLE`, `VOCABULARY` i instructional `QUESTION`;
- private, version-bound `VIDEO` MP4 i `AUDIO` MP3 asseti;
- AUDIO MP3-only contract: `.mp3`, `audio/mpeg`, 25 MiB, optional ID3v2 + valjani MPEG
  frameovi, obavezni controls i zabranjeni autoplay;
- extension/MIME/size/signature/structure/SHA-256 validacija prije private quarantinea;
- fail-closed ClamAV scan, auditabilni nereferencirani `Quarantined` malware i Clean promote
  bez izlaganja storage internals;
- `AssetKindCode` i SQL `51245` same-version/Clean/kind integrity;
- owner-only upload/read, CSRF, bounded multipart body, rate limit i document rowversion;
- responsive toolbar, property forme i siguran React prikaz svih šest nastavnih tipova.

## Sačuvane odluke

- `QUESTION` nije AssessableTask, StudentAttempt ni EvidenceEvent;
- nema external/public URL-a, base64 medija ni raw binary payloada;
- AUDIO se nikad ne pokreće automatski;
- VIDEO ostaje na postojećem MP4 contractu;
- duplicate unutar verzije reusea asset, a restore kopira Clean bytes u novi version-bound
  asset i opaque object key;
- Quick Check, Knowledge mapping, autosave, publish/export i AI nisu uvedeni.

## Validacija

- backend Release build: PASS — 0 warninga i 0 grešaka;
- targeted Phase 7.4 domain/API/SQL suite: PASS — 15/15 na stvarnom SQL Serveru;
- backend full regression: N/A — targeted Phase 7.4 validacija je dovoljna;
- frontend targeted Presentation Editor: PASS — 6/6, lint, TypeScript i production build;
- format: PASS — `dotnet format --verify-no-changes`;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni;
- Docker infrastructure validation: N/A — Dockerfile, Compose, startup i runtime identity nisu
  promijenjeni;
- visual acceptance: PASS — jedan stvarni desktop 1536×1024 i jedan mobile 390×844 feature
  state, bez globalnog overflowa, browser grešaka, auth bypassa, API interceptiona ili DOM
  mutationa; renderirani AUDIO nema `autoplay` atribut.

## Maintenance/security finding izvan Phase 7.4 scopea

Tijekom ranijeg, nepotrebno širokog audita u postojećem NuGet grafu prijavljeni su transitive
advisoryji za `System.Security.Cryptography.Pkcs` i `System.Security.Cryptography.Xml`.
Phase 7.4 ne mijenja dependency graph i finding nije uveden ovim featureom; zato ovdje nema
dependency upgradea. Solution Architect treba ga voditi kao zaseban security-maintenance rad.

## Dokumentacija

- `PRESENTATION_TEACHING_ELEMENTS.md`
- `PRESENTATION_SLIDE_DOCUMENT_MODEL.md`
- `PRESENTATION_CORE_ELEMENTS.md`
- `PERSISTENCE.md`
- `DECISION_LOG.md` / ADR-0026 Phase 7.4 primjena
- `ROADMAP.md`
- `visual-acceptance/phase-7.4/README.md`

## Scope namjerno nije implementiran

- Phase 7.5 Quick Check / AssessableTaskVersion;
- Phase 7.6 Knowledge Component mapping;
- Phase 7.7 autosave i recovery;
- Phase 7.8 preview, publish i export;
- Phase 7.9 AI assistance.
