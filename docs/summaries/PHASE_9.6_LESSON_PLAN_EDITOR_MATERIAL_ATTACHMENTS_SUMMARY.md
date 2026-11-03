# Phase 9.6 — Lesson Plan editor and immutable material-version attachment summary

## Status

**FINAL LOCKED — odobreno 2026-11-04.**

## Implementirano

- `LessonActivityMaterialVersion` exact version-bound junction i EF migracija;
- Draft-only SQL lifecycle/access triggeri `51320` i `51321`;
- owner-scoped editor read model s participant/session/goal/KC/activity/balance kontekstom;
- current Active/Clean owner ili Shared+Use Material candidate query;
- paginirana pretraga, exact KC overlap i already-attached stanje;
- CSRF + optimistic-concurrency attach/detach u serializable transakciji;
- responsive Screen 5.3 i prijelaz iz primijenjene 9.5 strukture;
- canonical desktop/mobile visual acceptance.

## Sačuvane odluke

- canonical attachment je exact `MaterialVersionId`, bez auto-upgradea;
- povijesna attached verzija ostaje čitljiva, Draft je jedini mutable lifecycle;
- nema storage ključeva, signed/public URL-a ni browser authorityja;
- Material relevance je status s exact-overlap objašnjenjem, ne score ili readiness;
- AI/provider/naplata, activity editing, Ready, Attempt i Evidence nisu uvedeni.

## Validation

| Gate | Rezultat |
|---|---|
| Release build | PASS — 0 warninga, 0 grešaka |
| Targeted backend | PASS — 2/2 owner/access/current-version/attach/detach testova |
| SQL migration/runtime | PASS — 1/1; upgrade, idempotent migrate i SQL 51320/51321 |
| EF model drift / idempotent script | PASS |
| Architecture | PASS — 4/4 |
| Frontend targeted | PASS — 2/2 |
| Frontend lint/typecheck/build | PASS |
| Visual acceptance | PASS — 1536×1024 i 390×844; errors 0; overflow 0 |
| Full regression | N/A — targeted phase validation is sufficient |
| Dependency audit | N/A — dependency graph and lockfiles unchanged |
| Docker infrastructure validation | N/A — infrastructure unchanged |

Prvi sandboxed VSTest pokušaji nisu uspjeli uspostaviti testhost IPC vezu; isti unaprijed
određeni targeted filteri pokrenuti su izvan sandboxa i prošli. Prvi SQL setup pokušaji
otkrili su i ispravili isključivo test-fixture lifecycle te SQL alias sintaksu; ponovljen je
samo taj gate. Poznati Vitest temp EPERM riješen je jednim workspace temp runnerom.

## Namjerno nije implementirano

- arbitrary activity/Knowledge Block add, delete, duplicate ili reorder;
- activity detail/edit, bilješke ili regeneration;
- Quick Check authoring i Draft → Ready;
- Board runtime, Attempt, Evidence, Mastery/Readiness mutation;
- AI provider poziv ili bilo kakva dodatna/in-app naplata.

## Sljedeća faza

Phase 9.7 smije dodati eksplicitni activity/Knowledge Block selection nad ovim Draft graphom,
bez preuranjenog activity detail/regeneration ili Ready workflowa.
