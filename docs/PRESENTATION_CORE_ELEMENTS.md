# Presentation core elements

## Status

**Phase 7.3 — FINAL LOCKED, odobreno 2026-10-16.**

Ovaj contract primjenjuje zaključani ADR-0026 i Phase 7.1 model na četiri osnovna
prezentacijska elementa. Ne mijenja Phase 7.2 slide lifecycle ni concurrency authority.

## Scope

Teacher može na slajdu Draft `PRESENTATION` MaterialVersiona stvarati, uređivati i brisati:

- `TEXT`;
- `IMAGE`;
- `SHAPE`;
- `TABLE`.

Svaki element ima stabilni GUID, pripada točno jednom slajdu i istoj MaterialVersion granici,
ima `SchemaVersion = 1`, logical-canvas geometry (`X`, `Y`, `Width`, `Height`), contiguous
`ZIndex` i strogo tipizirani bounded JSON payload. Canvas authority ostaje `1600×900`.

## Payload registry v1

Payload parser dopušta samo poznata polja i odbija nepoznati tip, nepoznato polje ili payload
koji ne odgovara tipu.

- `TEXT`: tekst do 4000 znakova, font 12–160, `Regular|Bold`, hex boja i
  `Left|Center|Right` poravnanje;
- `IMAGE`: obavezni version-bound `PresentationAsset`, alt tekst do 500 znakova i
  `Contain|Cover` prikaz;
- `SHAPE`: `Rectangle|Ellipse|Line`, fill/stroke hex boje i stroke 0–20;
- `TABLE`: 1–20 redaka, 1–10 stupaca, do 500 znakova po ćeliji i opcionalni header red.

Raw HTML, CSS, JavaScript, iframe, vanjski URL i proizvoljni nevalidirani payload nisu
dopušteni. Frontend renderira strukturirane vrijednosti kroz React bez `innerHTML`.

## Limits and integrity

- najviše 200 elemenata po slajdu;
- najviše 256 KiB UTF-8 JSON payloada po elementu;
- najviše 8 MiB zbrojenog element payloada po dokumentu;
- geometry mora ostati potpuno unutar logical canvasa;
- z-index je jedinstven i contiguous unutar slajda;
- element, slajd, asset, dokument i MaterialVersion moraju pripadati istom version snapshotu;
- delete ponovno kompaktira z-index bez rupa.

EF i SQL koriste restrictive composite FK-ove, unique z-index i CHECK constrainte. SQL
triggeri `51243` i `51244` dopuštaju asset/element mutacije samo nad Draft PRESENTATION
snapshotom; Active i Superseded graph ostaju immutable.

## Image asset boundary

Slika nije URL spremljen u payloadu. Upload prolazi server-side extension/MIME/signature
provjeru za PNG, JPEG ili WebP, limit 10 MiB, quarantine objekt, fail-closed ClamAV scan,
SHA-256 i tek potom promote u private storage. U bazi se sprema version-bound Clean asset;
storage key, checksum i scan detalji nikad se ne izlažu editor DTO-u.

Read endpoint je owner-only i vraća samo Clean asset iz aktualnog Presentation Drafta.
Anonymous zahtjev dobiva `401`, a missing/foreign/archived/non-Presentation resurs ne otkriva
ownership. Klijent ne bira object key.

## API and concurrency

Owner-only endpointi pod `/api/v1/materials/{materialId}/presentation` proširuju Phase 7.2:

- `POST /slides/{slideId}/elements`;
- `POST /slides/{slideId}/images`;
- `PUT /slides/{slideId}/elements/{elementId}`;
- `DELETE /slides/{slideId}/elements/{elementId}`;
- `GET /assets/{assetId}`.

Svaki write koristi aktualni `expectedDocumentRowVersion`; stale revision vraća kontrolirani
`409`. JSON body je ograničen na 16 KiB, image body na 11 MiB, svi writeovi zahtijevaju CSRF i
postojeći Teacher-scoped Presentation rate limit. Backend je konačni authority za ownership,
Draft status, tip, payload, geometry, z-index i limite.

## Copy semantics

- slide duplicate atomski klonira njegove elemente uz nove element ID-eve i isti same-version
  Clean asset;
- Material restore/new Draft klonira dokument, slajdove i elemente te fizički kopira Clean
  assete u novu MaterialVersion granicu s novim asset ID-em i object keyem;
- neuspjelo storage/DB kopiranje ima kompenzacijsko čišćenje novostvorenih objekata.

Instructional elementi nikad nisu Evidence i nemaju Knowledge mapping.

## Editor UX

Canonical P3 editor ima lijevi slide navigator, centralni responsive 16:9 canvas i desni
property/layer panel. Toolbar stvara četiri podržana tipa, property panel uređuje strukturirani
payload i geometry, a layer navigator omogućuje odabir preklopljenih elemenata. Spremanje je
eksplicitno i persisted kroz API; nema lažnog local-only authoringa.

## Out of scope

- teaching-specific elementi, notes i media handleri iz Phase 7.4;
- Quick Check i assessable Task povezivanje iz Phase 7.5;
- Knowledge Component mapping iz Phase 7.6;
- autosave, recovery i napredni conflict UX iz Phase 7.7;
- preview, publish/package/export iz Phase 7.8;
- AI pomoć i PPTX conversion.

## Evidence

Implementacijski i testni rezultat je u
`summaries/PHASE_7.3_PRESENTATION_CORE_ELEMENTS_SUMMARY.md`, a stvarni canonical
desktop/mobile dokaz u `visual-acceptance/phase-7.3/`.
