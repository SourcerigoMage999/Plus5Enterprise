# Phase 6.4 — Screen 4.2 Material detail

## Status

**FINAL LOCKED — odobreno 2026-10-03.**

## Cilj faze

Implementirati Teacher-only Material detail nad zaključanim current-version, Clean-file i
owner/share contractom te canonical Screen 4.2 layoutom, bez preuranjenog Task/Evidence,
storage deployment, edit ili Lesson scopea.

## Implementirano

- `IMaterialDetailQuery` application read contract i `EfMaterialDetailQuery`;
- owner ili eksplicitni Shared recipient scope prije projekcije;
- samo Active/non-archived Material, Active current version i Clean file;
- `GET /api/v1/materials/{materialId}` s jednakim `404` za missing/foreign/archived;
- točni version-bound tag, KnowledgeComponent/KnowledgeModel i CurriculumOutcome podaci;
- `/materials/:materialId` s canonical headerom, metadata karticom, centralnim preview prostorom,
  learning mappingom, curriculumom, details i honest future stanjima;
- library title linkovi na stvarni detalj;
- loading, retry i safe not-found stanje;
- desktop/mobile canonical visual evidence.

## Security

- anonymous API je `401`;
- Teacher ID dolazi samo iz cookie session claima;
- shared read traži eksplicitni grant i `Shared` visibility;
- object key, bucket/container, checksum, scan detalji i binary nisu dio responsea;
- `Otvori`/`Preuzmi` nisu aktivirani bez production storage-read adaptera;
- browser capture nije napravio business write.

## Test evidence

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Phase 6.4 query testovi | PASS — 3/3 |
| Stvarni SQL owner/share/foreign + EF translation | PASS — 1/1 |
| Backend regression | PASS — 202 prošlo, 45 opt-in SQL skipova, 0 palo |
| Architecture | PASS — 4/4 |
| Phase 6.3/6.4 frontend ciljano | PASS — 7/7 |
| Frontend regression | PASS — 77/77 |
| lint/typecheck/production build | PASS |
| `.NET format --verify-no-changes` | PASS |
| NuGet / npm vulnerability audit | PASS — 0 poznatih ranjivosti |
| Docker build/runtime/health | PASS — API live/ready i frontend HTTP 200 |
| Non-root runtime | PASS — API `uid=1654(app)`, frontend `uid=101(nginx)` |
| Canonical visual gate | PASS — 1536×1024 i 390×844, bez overflowa/grešaka/writeova |

## Scope discipline

- nema migracije ili persistence promjene;
- nema lažnog previewa ili javnog storage URL-a;
- nema Task/TaskVersion, Attempt/Evidence, readiness, usage ili recommendation formule;
- nema import/create iz 6.6, edit/version history iz 6.7, permission mutation iz 6.8 ili
  Lesson Builder integracije;
- obični tagovi ostaju odvojeni od kontroliranih Knowledge Components.

## Dokumentacija i evidence

- `MATERIAL_DETAIL.md`;
- `visual-acceptance/phase-6.4/README.md`;
- `visual-acceptance/phase-6.4/measurements.json`;
- `ROADMAP.md` i `FRONTEND_FOUNDATION.md`.

## Točna početna točka za sljedeću fazu

Nakon SA reviewa i FINAL LOCK-a za 6.4, Phase 6.5 može definirati Evidence-capable
`Task`/`TaskVersion` metadata unutar Materiala. Ne smije retroaktivno pretvoriti samo gledanje
instructional Materiala u dokaz znanja.
