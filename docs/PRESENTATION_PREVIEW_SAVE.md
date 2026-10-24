# Presentation preview and save flow

## Status

**Phase 7.8 — IMPLEMENTED / REVIEW READY — 2026-10-24.**

Ovaj contract dovršava zaključani ADR-0026 publish smjer: Teacher prije objave čita stvarni
Draft graph, pregledava njegove metadata i slajdove te ga jednom atomskom naredbom pretvara u
nepromjenjivu Active MaterialVersion. Relacijski Presentation graph ostaje canonical source;
server-generated ZIP je izvedeni private artefakt za zaključani `MaterialFile` lifecycle.

## Review workspace

```http
GET /api/v1/materials/{materialId}/presentation/review
```

Review je dostupan samo autentificiranom owner Teacheru za Active/non-archived Material koji
ima Draft `PRESENTATION` verziju i inicijalizirani `PresentationDocument`. Missing, foreign,
archived, objavljeni ili neinicijalizirani resource vraća isti neutralni `404`.

Response vraća samo podatke potrebne za četvrti korak:

- Material i Document rowversion;
- version-bound naziv, opis, predmet, jezik, visibility, Program, SchoolGrade i
  ProficiencyLevel;
- cilj učenja, tagove, exact Knowledge Component i CurriculumOutcome reference;
- spremljene slajdove, typed elemente i dopušteni private asset read endpoint;
- broj slajdova i Quick Check elemenata;
- server-derived publish blockers.

Storage key, bucket, checksum i javni/signed URL ne izlažu se review payloadom. Različite
KnowledgeModel verzije ostaju eksplicitne; browser ne izvodi nove pedagoške agregate.

## Publish naredba

```http
POST /api/v1/materials/{materialId}/presentation/publish
```

Request nosi exact `MaterialVersionId`, `expectedMaterialRowVersion` i
`expectedDocumentRowVersion`. Endpoint je Teacher-only i ponovno primjenjuje owner scope,
CSRF, bounded body i Teacher-scoped write rate limit.

Server prije objave provjerava:

- 1..200 slajdova i contiguous `SortOrder`;
- podržani typed element i njegov aktualni schema version;
- valjan JSON payload;
- svaki media element ima same-version, Clean asset odgovarajućeg kinda;
- svaki Quick Check ima exact AssessableTaskVersion s najmanje jednim Knowledge mappingom;
- obje očekivane rowversion vrijednosti odgovaraju aktualnom stanju.

Validation greška ostavlja Draft nepromijenjen. Stale Material ili Document vraća `409`;
nema last-write-wins objave.

## Deterministički package

Nakon validacije server iz canonical grapha generira `plus5-presentation.zip` s media typeom:

```text
application/vnd.plus5.presentation+zip
```

V1 paket sadrži `manifest.json` s document/canvas verzijom, poredanim slajdovima, typed
elementima, geometryjem, payloadom, asset i TaskVersion referencama te asset metadata
referencama. ZIP entry timestamp je fiksiran, redoslijed je determinističan, a SHA-256 se
računa nad konačnim byteovima. Paket ne sadrži javne URL-ove, storage ključeve ni proizvoljne
remote reference.

Paket je server-generated artefakt: upisuje se izravno u private Clean storage pod novim
opaque keyjem i dobiva `SERVER_GENERATED` scan kategoriju. Ne prolazi user-upload quarantine
jer nije nepouzdani user binary; svi user asseti koje manifest referencira već moraju biti
Clean kroz zaključani asset pipeline.

## Atomska aktivacija

Nakon storage upisa server otvara serializable SQL transakciju i ponovno učitava owner,
Draft, Document, aktualni Active snapshot i obje rowversion vrijednosti. Uspjeh atomski:

```text
zamjenjuje privremeni Draft MaterialFile izvedenim packageom
→ supersedea prethodnu Active MaterialVersion
→ aktivira Draft uz Clean package
→ postavlja Draft kao Material.CurrentVersion
```

Concurrency ili SQL failure rollbacka bazu i best-effort briše novi package. Nakon commita
stari Draft upload object briše se best-effort; orphan lifecycle cleanup ostaje zaštitna
mreža. Active/Superseded PresentationDocument graph ostaje immutable kroz postojeće SQL
guardove.

## UI

Ruta `/materials/{materialId}/presentation/review` prikazuje:

- četvrti canonical korak `Pregled i spremanje`;
- stvarne osnovne i pedagoške version-bound podatke;
- stvarni spremljeni slide preview, thumbnails i full-preview modal;
- broj slajdova, Knowledge Componenta, ishoda i Quick Checkova;
- blocker listu ili `Spremno za biblioteku` status;
- povratak u editor i jednu eksplicitnu `Spremi materijal` akciju.

Editor omogućuje nastavak tek kada je autosave stanje `saved`. Preview je read-only: ne
stvara Attempt, EvidenceEvent, mastery/readiness promjenu ni Student podatke.

## Izvan scopea

- public share/presentation URL i anonymous playback;
- PPTX/PDF/video export i client-side ZIP generation;
- offline package sync, collaborative publish i approval workflow;
- publish rollback/reopen Active snapshot in-place;
- Student presentation runtime, Attempt, Evidence i grading;
- AI assistance (Phase 7.9, i dalje BLOCKED).

## Validation budget

Klasifikacija: `READ_API + WRITE_API + SECURITY + UI + DOCS_ONLY`.

- backend Release build i targeted review/publish/storage/API testovi;
- targeted frontend review/publish testovi, lint, TypeScript i production build;
- jedan stvarni desktop i jedan mobile screenshot, document overflow i browser/page errors;
- jedan stvarni review → package → atomic publish runtime flow;
- full regression: N/A — shared auth/infrastruktura/domain baseline nisu promijenjeni;
- dependency audit: N/A — dependency graph i lockfile nisu promijenjeni;
- Docker infrastructure validation: N/A — Dockerfile, Compose, startup i runtime identity
  nisu promijenjeni.
