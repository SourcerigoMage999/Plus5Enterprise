# Phase 6.2 — Material metadata ↔ curriculum/knowledge mapping

## Status

**FINAL LOCKED — odobreno 2026-09-29.**

## Cilj faze

Proširiti zaključani Material snapshot canonical pedagoškim metapodacima i eksplicitnim
Curriculum/Knowledge vezama bez uvođenja Library/detail/import/edit UI-ja ili budućeg Task i
Evidence scopea.

## Implementirano

- nullable version-bound Program, SchoolGrade, ProficiencyLevel i LearningGoal metadata;
- composite same-Teacher Program ownership zaštita;
- version-bound, case-insensitive jedinstveni organizacijski tagovi;
- čisti M:N `MaterialVersionCurriculumOutcome` i `MaterialVersionKnowledgeComponent` mapping;
- zabrana canonical Material mappinga prema Draft KnowledgeModelu uz očuvane Published/Retired
  exact-version reference;
- Draft-only mutation i SQL immutability zaštita svih junctiona;
- migracija bez seeda/backfilla te domain/EF/stvarni SQL regression testovi.

## Namjerno nije implementirano

- javni API, Library/detail/import/edit ekran ili picker;
- obaveznost metadata polja na nepotpunom Draftu;
- production SchoolGrade/Proficiency/Curriculum/Knowledge katalog;
- AI suggestion persistence, automatski mapping ili Teacher confirmation UI;
- Task/TaskVersion metadata, difficulty/evidence/scoring i Evidence emission;
- weights, coverage, readiness ili mastery logika na Material mappingu.

## Promijenjene / dodane datoteke

| Grupa | Datoteke | Razlog |
|---|---|---|
| domain | `MaterialVersion.cs`, `MaterialVersionTag.cs`, `MaterialVersionCurriculumOutcome.cs`, `MaterialVersionKnowledgeComponent.cs` | snapshot metadata i mapping invarijante |
| persistence | `MaterialPersistenceConfigurations.cs`, `Plus5DbContext.cs` | FK/PK/index/trigger model |
| migration | `20260929204328_AddMaterialMetadataMapping*`, model snapshot | aditivna SQL schema evolucija |
| tests | `MaterialMetadataMappingTests.cs`, `MaterialMetadataMappingSqlTests.cs` | domain/model, upgrade, ownership i lifecycle gate |
| docs | `MATERIAL_METADATA_MAPPING.md`, `MATERIAL_FOUNDATION.md`, `ROADMAP.md`, `PERSISTENCE.md`, `DECISION_LOG.md`, ovaj summary | source-of-truth i phase handoff |

## Domain / database promjene

- `MaterialVersions`: `ProgramId`, `SchoolGradeId`, `ProficiencyLevelId`, `LearningGoal`;
- nove tablice: `MaterialVersionTags`, `MaterialVersionCurriculumOutcomes`,
  `MaterialVersionKnowledgeComponents`;
- svi novi FK-ovi su restrictive; Program koristi composite Teacher ownership FK;
- mapping/tag retci mogu se mijenjati samo dok je MaterialVersion Draft;
- Knowledge target ne smije pripadati Draft KnowledgeModelu;
- postojeći 6.1 podaci ne dobivaju fake metadata niti backfill.

## API promjene

Nema javnog API contracta ni endpointa.

## Frontend promjene

Nema frontend promjene; visual acceptance nije primjenjiv.

## Security / authorization

- owner dolazi iz postojećeg Material snapshot scopea;
- composite FK odbija cross-Teacher Program reference čak i pri izravnom SQL upisu;
- restrictive FK-ovi čuvaju povijesne targete;
- nema novog javnog write surfacea ni IDOR površine.

## Testovi

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Phase 6.2 domain/model testovi | PASS — 6/6 |
| Stvarni SQL Server migration/upgrade/lifecycle testovi | PASS — 4/4 |
| Puni backend regression u pinnanom .NET 10 SDK containeru | PASS — 196 prošlo, 44 opt-in SQL testa preskočena, 0 palo |
| Architecture testovi | PASS — 4/4 |
| Frontend regression | PASS — 70/70; lint, typecheck i production build prolaze |
| `.NET format --verify-no-changes` | PASS |
| EF pending-model check | PASS — nema pending model promjena |
| EF idempotent migration script | PASS — generiran i pregledan |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |

Fokusirani SQL testovi stvarno primjenjuju migraciju, provjeravaju upgrade postojećih 6.1
redaka, Draft/Active/Superseded lifecycle za metadata i junctione, KnowledgeModel status te
cross-Teacher Program zaštitu. Visual acceptance nije primjenjiv jer faza nema UI promjenu.

## Self-review

- [x] scope nije proširen na 6.3+ UI/API ili 6.5 Task metadata
- [x] tag nije KnowledgeComponent
- [x] SchoolGrade nije ProficiencyLevel/CEFR
- [x] Material mapping nije Evidence i ne mijenja readiness
- [x] nema seeda, fake kataloga, weighta ili automatskog mappinga
- [x] migracija je aditivna i existing-row safe
- [x] Active/Superseded povijest ostaje immutable
- [x] dokumentacija je usklađena

## Arhitekturne odluke

ADR-0024.

## Poznati rizici / tehnički dug

Production katalog/source provisioning ostaje zaseban odobreni gate. UI command validation i
atomarno kopiranje junctiona pri restoreu implementira 6.6/6.7 nad ovim persistence contractom.

## Otvorena pitanja

Nema otvorenog pitanja koje blokira Phase 6.2 scope.

## Točna početna točka za sljedeću fazu

Phase 6.3 može implementirati Screen 4.1 Material library read model/API/UI nad
`Material.CurrentVersionId` i ovim version-bound Program/Grade/Level/tag/outcome/component
indeksima, uz postojeći owner/share authorization contract i bez fake podataka.
