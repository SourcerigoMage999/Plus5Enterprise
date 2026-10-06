# Phase 6.6 — Import own material

## Status

**FINAL LOCKED — odobreno 2026-10-06.**

## Cilj faze

Omogućiti Teacheru ručni četverokoračni uvoz vlastite datoteke u Material biblioteku kroz
zaključani private-storage, fail-closed validation/scanning i Clean-only activation contract.

## Implementirano

- Teacher-only `/materials/import` wizard: datoteka, osnovni podaci, cilj/mapiranje i review;
- `GET /api/v1/materials/import/options` s owner-scoped Programima i verzioniranim
  Knowledge/Curriculum referencama;
- CSRF-zaštićen i upload-rate-limited `POST /api/v1/materials/import` multipart endpoint;
- PDF/DOCX/PPTX/MP4/ZIP size, MIME, signature i strukturalna validacija te SHA-256;
- ZIP traversal/executable/nested/symlink/count/expanded-size/compression-ratio zaštite;
- private S3-compatible quarantine/clean adapter, persistent local S3Mock i ClamAV INSTREAM scanner;
- fail-closed Quarantined/Failed stanja te kompenzacijsko brisanje storage objekta na SQL
  grešci;
- atomsko spremanje Draft snapshot-a, metadata mappinga i aktivacije preko postojećih SQL
  lifecycle triggera;
- owner-scoped detail read-back nakon HTTP 201;
- stvarni desktop/mobile canonical visual evidence i jedan namjerni clean runtime import;
- regression zaštita od preuranjenog submita pri prijelazu na review.

## Sigurnost i integritet

- Teacher identitet nikad ne dolazi iz request bodyja;
- anonymous import surface vraća `401`, a authenticated write bez CSRF-a `400`;
- object key, bucket, checksum i scanner detalji nisu dio API responsea;
- quarantine i clean bucketi su privatni i odvojeni;
- scanner outage ne aktivira materijal;
- server ponovno provjerava ownership i sve reference prije uploada;
- samo Active/current/Clean rezultat ulazi u postojeću Library/detail projekciju;
- API→Domain dependency nije uveden: transportni enum contract ostaje u Application sloju.

## Test evidence

| Gate | Rezultat |
|---|---|
| Backend/container Release build | PASS — 0 warninga, 0 grešaka |
| Ciljani import/validator/security testovi | PASS — 14/14 |
| Backend regression | PASS — 215 prošlo, 47 očekivanih SQL opt-in skipova, 0 palo |
| Architecture | PASS — 4/4 |
| Frontend Materials ciljano | PASS — 8/8 |
| Frontend regression | PASS — 78/78 |
| lint/typecheck/production build | PASS |
| `.NET format --verify-no-changes` | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Compose build/runtime/health | PASS — SQL, persistent S3Mock, ClamAV, API i frontend zdravi |
| Object-storage restart persistence | PASS — testni objekt preživljava restart i zatim je uklonjen |
| Stvarni import | PASS — HTTP 201, detail HTTP 200, clean bucket objekt, quarantine prazan |
| Canonical visual gate | PASS — 1536×1024 i 390×844, bez overflowa/browser grešaka |

Nema nove EF migracije ni model drifta: Phase 6.6 koristi zaključanu 6.1/6.2 shemu i
integrity triggere. SQL opt-in suite nije ponovno pokrenut jer nema schema/invariant promjene;
stvarni SQL transakcijski activation put dokazan je runtime importom.

## Scope discipline

- nije uveden AI/OCR/transcription/provider contract;
- nije implementiran Material edit/version-history flow iz 6.7;
- nije implementiran share/permission mutation iz 6.8;
- nije uveden Task extraction, grading, Attempt/Evidence emission ili readiness promjena;
- nije uveden public object URL, thumbnail/preview conversion ni Lesson Builder integracija.

## Dokumentacija i evidence

- `MATERIAL_IMPORT.md`;
- `visual-acceptance/phase-6.6/README.md` i `measurements.json`;
- `CONFIGURATION.md`;
- `ROADMAP.md`.

## Točna početna točka za sljedeću fazu

Phase 6.7 smije uređivati metadata samo stvaranjem nove Draft `MaterialVersion` i očuvanjem
immutable Active povijesti. Ne smije mijenjati postojeći Active snapshot in-place niti
zaobići isti validation/quarantine/scan/Clean activation put kada se zamjenjuje datoteka.
