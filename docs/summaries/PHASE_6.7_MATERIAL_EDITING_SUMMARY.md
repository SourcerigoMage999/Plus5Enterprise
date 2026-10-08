# Phase 6.7 — Edit material and version history

## Status

**FINAL LOCKED — odobreno 2026-10-08.**

## Cilj faze

Omogućiti owner Teacheru verzionirano uređivanje Material metapodataka, immutable povijest,
Save Draft, atomsku objavu nove verzije i restore povijesnog snapshota kao novu skicu, bez
uvođenja Presentation Editora iz Phase 7.

## Implementirano

- owner-only `/materials/{materialId}/edit` UI s loading/error/retry i dirty-form zaštitom;
- workspace i read-only version-history queryji;
- CSRF-zaštićeni Save Draft, Publish i Restore endpointi;
- optimistic concurrency preko `Material.RowVersion`;
- Active/Superseded immutable snapshots i jedna otvorena Draft verzija;
- server-side private clean-object copy u vlastiti opaque key nove verzije;
- snapshot kopiranje metadata, tag/outcome/KC mappinga i AssessableTaskVersion metadata;
- transakcijski Active→Superseded + Draft→Active/current publish;
- restore povijesne verzije u novi kasniji Draft;
- stvarni SQL/storage runtime flow i desktop/mobile canonical visual evidence;
- owner-only aktivan `Uredi` link na Material detailu.

## Ključne odluke

- edit nikad ne mutira Active/Superseded redak;
- restore stvara Draft, ne reaktivira povijest i ne mijenja stare reference;
- Clean copy nije novi scan: čuva checksum/size i bilježi `CLEAN_COPY` uz novi object key;
- Visibility ostaje read-only do 6.8;
- binary replacement mora u budućnosti ponovno proći puni import security pipeline;
- Presentation/slide content editor nije simuliran prije Phase 7.

## API promjene

- `GET /api/v1/materials/{materialId}/edit`;
- `GET /api/v1/materials/{materialId}/versions/{versionId}`;
- `PUT /api/v1/materials/{materialId}/draft`;
- `POST /api/v1/materials/{materialId}/draft/publish`;
- `POST /api/v1/materials/{materialId}/versions/{versionId}/restore`.

Nema nove EF migracije ni model drifta. Dodani su domain behaviori za aggregate version touch i
verified clean-copy lifecycle te S3 clean-to-clean copy port/adapter.

## Test evidence

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Ciljani Material domain/metadata/task testovi | PASS — 24/24 |
| Backend regression (.NET 10 Linux SDK) | PASS — 216/216; 47 očekivanih SQL opt-in skipova |
| Architecture (.NET 10 Linux SDK) | PASS — 4/4 |
| Frontend Materials ciljano | PASS — 10/10 |
| Frontend regression | PASS — 80/80 |
| Frontend lint/typecheck/production build | PASS |
| `.NET format --verify-no-changes` | PASS |
| Docker image build/runtime/health | PASS |
| Non-root runtime | PASS — API `1654(app)`, frontend `101(nginx)` |
| Stvarni version lifecycle | PASS — import 201, save/publish/restore 200 |
| Security runtime | PASS — anonymous 401, missing CSRF 400 |
| Canonical visual gate | PASS — 1536×1024 i 390×844, bez overflowa/browser grešaka |

Dependency graph nije promijenjen, pa vulnerability auditi nisu ponovno pokretani. Schema i
SQL triggeri nisu promijenjeni; stvarni SQL publish/restore put provjeren je kroz runtime gate.

## Izmijenjene/dodane cjeline

- Domain/Application: Material version touch, clean copy, editing contracts i storage port;
- Infrastructure: owner-scoped query, write orchestration i S3 clean copy;
- API: edit/version/save/publish/restore endpointi;
- frontend: edit route/page/styles/API i detail edit link;
- testovi: Material lifecycle i Materials UI regression;
- dokumentacija: `MATERIAL_EDITING.md`, ROADMAP, summary i visual evidence.

## Scope namjerno nije implementiran

- Material sharing/visibility/permissions iz 6.8;
- slide CRUD/content/Quick Check/preview/autosave iz Phase 7;
- AI prijedlozi, preview conversion i thumbnailovi;
- binary replacement, duplicate, archive/delete, export i Lesson Builder integracija.

## Točna početna točka za sljedeću fazu

Phase 6.8 smije mijenjati samo `Material.Visibility` i eksplicitne `MaterialShare` grantove
prema zaključanom View/Use contractu. Ne smije dati shared recipientu edit pristup niti
retroaktivno mijenjati version history.
