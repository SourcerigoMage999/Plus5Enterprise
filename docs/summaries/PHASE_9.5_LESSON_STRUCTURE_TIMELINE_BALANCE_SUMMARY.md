# Phase 9.5 — Suggested lesson structure timeline and balance check summary

## Status

**FINAL LOCKED — odobreno 2026-11-03.**

## Implementirano

- target duration snapshot: Session duration ili default 60;
- migration-deployed KB-001–KB-025 katalog, s 001–005 Published i 006–025 Draft;
- no-cost `SYSTEM_BASELINE_V1` preview s točnim 60-min rasporedom 5/10/15/20/10;
- deterministic largest-remainder raspodjela za druge targete;
- owner-scoped structure read, target-duration write i atomic Apply API;
- pet canonical aktivnosti, contiguous 0–4 order i explicit plan-KC mapping copy;
- šest-area balance read model bez overall scorea/predikcije;
- Material candidate count bez attachmenta ili relevance tvrdnje;
- responsive Screen 5.2 s iskrenim AI-disabled/no-extra-cost stanjem.

## Sačuvane odluke

- preview nije canonical i učitavanje nema business write;
- Apply vrijedi samo za prazan Draft i koristi optimistic rowversion;
- KB-006 ostaje Draft; understanding check nije assessment/evidence;
- published KB duration envelope daje warning, ne hard reject;
- ručno target trajanje je canonical; aktivnosti se ne auto-resizeaju nakon Apply;
- AI provider nije pozvan i PLUS 5 nema AI billing/in-app naplatu;
- Material attachment, arbitrary KB/activity editing, Ready i Evidence nisu uvedeni.

## Validation

| Gate | Rezultat |
|---|---|
| Release build | PASS — 0 warninga, 0 grešaka |
| Targeted backend | PASS — 7/7 domain/query/create/preview/apply testova |
| SQL migration/runtime | PASS — 1/1; upgrade/backfill, idempotent migrate, katalog, Apply i SQL 51310–51312 |
| Architecture | PASS — 4/4 |
| Frontend targeted | PASS — 4/4 Phase 9.4/9.5 |
| Frontend lint/typecheck/build | PASS |
| Visual acceptance | PASS — 1536×1024 i 390×844; errors 0; capture writes 0; overflow 0 |
| Full regression | N/A — targeted phase validation is sufficient |
| Dependency audit | N/A — dependency graph and lockfiles unchanged |
| Docker infrastructure validation | N/A — infrastructure unchanged |

Windows sandbox je pri prvom Vitest pokretanju vratio poznati EPERM, a paralelni VSTest
testhost nije uspio uspostaviti vezu. Isti unaprijed određeni targeted filteri jednom su
pokrenuti izvan sandboxa i prošli, bez širenja scopea.

Docker image restore tijekom pripreme browser runtimea zaustavili su ranije poznati upstream
advisoryji za postojeće transitive `System.Security.Cryptography.Pkcs` i
`System.Security.Cryptography.Xml`. Phase 9.5 nije promijenio dependency graph; prema STRICT
VALIDATION BUDGET-u nije napravljen opportunistic upgrade. Stvarni visual gate izveden je
lokalnim već izgrađenim API/frontend runtimeom nad istim SQL-om i ostaje PASS. Advisoryji su
zaseban maintenance/security finding za Solution Architecta.

## Namjerno nije implementirano

- OpenAI/drugi provider runtime i personalization;
- Material attachment/relevance evaluation;
- Knowledge Block picker i arbitrary activity editor/regeneration;
- Ready confirmation, Board/LessonSession runtime;
- Attempt, EvidenceEvent, Mastery/Readiness ili predicted grade.

## Sljedeća faza

Phase 9.6 smije dodati full Lesson Plan editor i immutable exact MaterialVersion attachment
nad ovim Draft activity graphom, bez proširenja u KB picker ili Evidence runtime.
