# Material import

## Status

**Phase 6.6 — IMPLEMENTED / REVIEW READY (2026-10-05).**

Ovaj dokument je source of truth za Screen 4.4 i siguran Teacher-only uvoz vlastitog
materijala. Primjenjuje zaključane `MATERIAL_FOUNDATION.md` i
`MATERIAL_METADATA_MAPPING.md` contracte bez uvođenja edita/version historyja iz 6.7,
permission managementa iz 6.8 ili AI authoringa.

## Route i API

- frontend: `/materials/import`;
- opcije: `GET /api/v1/materials/import/options`;
- spremanje: `POST /api/v1/materials/import` kao `multipart/form-data` s jednim `file`
  dijelom i jednim JSON `metadata` dijelom;
- pristup: isključivo autentificirani Teacher, čiji ID dolazi iz server-side sesije;
- write zahtijeva valjan CSRF token, ima request/form limit 251 MB i zaseban rate limit
  od pet pokušaja u deset minuta po Teacheru/IP-u.

Options endpoint vraća samo owner-scoped Programe te globalne Grade/CEFR kataloge,
Published/Retired KnowledgeModel komponente i CurriculumOutcome retke. Klijent ne šalje
Teacher ID, storage lokaciju, checksum ni scan odluku.

## Četiri koraka

1. `Datoteka` — PDF, DOCX, PPTX, MP4 ili ZIP uz format-specifični limit;
2. `Osnovni podaci` — naziv, vrsta, opis, predmet, jezik, Program, razred/razina,
   vidljivost i slobodne oznake;
3. `Cilj i mapiranje` — opcionalni cilj učenja te version-bound KnowledgeComponent i
   CurriculumOutcome odabir;
4. `Pregled` — eksplicitna potvrda svih podataka prije jedinog write zahtjeva.

React Hook Form i Zod provode korisničku validaciju, ali server je konačni authority.
Dirty-form guard štiti napuštanje, a prijelaz na review nikada ne smije automatski poslati
formu. AI prijedlozi nisu preduvjet i nisu implementirani.

## Datoteka i sigurnosna validacija

| Format | Maksimalna veličina | Obavezna provjera |
|---|---:|---|
| PDF | 50 MB | ekstenzija, dopušteni MIME i `%PDF-` signature |
| DOCX | 25 MB | ZIP struktura, `[Content_Types].xml` i `word/document.xml` |
| PPTX | 100 MB | ZIP struktura, `[Content_Types].xml` i `ppt/presentation.xml` |
| MP4 | 250 MB | dopušteni MIME i `ftyp` box signature |
| ZIP | 100 MB | sigurna arhiva prema pravilima ispod |

ZIP odbija absolute/traversal/duplicate putanje, executable ili nested ZIP sadržaj,
symlinkove, nečitljive/encrypted entryje, više od 500 entryja, više od 500 MB expanded
sadržaja i entry compression ratio iznad 100:1. Server ponovno mjeri stvarnu veličinu i
računa SHA-256; filename se svodi na sigurno bazno ime, a object key je nepredvidiv i
isključivo server-generated.

## Storage, scan i activation lifecycle

Binary nikada ne ide u SQL ni na persistentni API disk:

```text
validate extension/MIME/signature/structure + SHA-256
    → private quarantine bucket
    → ClamAV-compatible INSTREAM scan
    → malware: Quarantined + HTTP 422
    → scanner outage/error: Failed + HTTP 503
    → clean: copy to private clean bucket + remove quarantine object
    → atomic SQL insert snapshot/mappings + Draft → Active
    → SQL trigger postavlja Material.CurrentVersionId
    → HTTP 201 + owner-scoped detail route
```

SQL stvaranje i aktivacija izvode se u jednoj transakciji. Object storage ne sudjeluje u
distribuiranoj transakciji: ako SQL spremanje nakon promocije ne uspije, servis kompenzacijski
briše i clean i quarantine objekt te vraća izvornu grešku. Malware i scanner-failure zapisi
ostaju non-current i nisu vidljivi u Library/detail queryjima.

`IMaterialObjectStorage` koristi privatni S3-compatible adapter. Lokalni Compose koristi
Adobe S3Mock s odvojenim persistentnim quarantine/clean bucketima; production početni provider
ostaje Cloudflare R2 i credentiali dolaze isključivo iz deployment secrets sloja.

## Metadata i ownership integritet

- Program referenca mora pripadati prijavljenom Teacheru;
- Grade, CEFR, Knowledge Component i Curriculum Outcome moraju postojati;
- Knowledge Component mora biti Active u Published ili Retired KnowledgeModel verziji;
- tagovi su trimani, case-insensitive deduplicirani i najviše 64 znaka;
- metadata/tag/outcome/component veze nastaju dok je verzija Draft i nakon aktivacije
  postaju immutable prema postojećim SQL triggerima;
- uspješan rezultat je isključivo `Active` Material s `Active` current verzijom i točno
  jednom `Clean` datotekom.

## Error contract

- `401` anonymous;
- `400` CSRF, multipart/metadata/input ili file validation problem;
- `404` nepostojeća ili nedopuštena referenca, bez ownership enumeracije;
- `422` detektiran malware;
- `429` upload rate limit;
- `503` scanner nije dostupan ili nije dao čist rezultat;
- neočekivane greške prolaze kroz postojeći sigurni globalni ProblemDetails boundary.

## Izvan Phase 6.6

Nema AI metadata analize, OCR/transkripcije, PPTX konverzije, thumbnail/preview pipelinea,
Task extractiona, AssessableTask authoringa, edita ili nove MaterialVersion povijesti,
share-grant managementa, archive/deletea, Lesson Builder handoffa ni automatskog
Evidence/Readiness učinka. Signed-read capability postoji iza storage adaptera, ali binary
download/inline-preview endpoint i UI nisu dio ovog import ekrana.
