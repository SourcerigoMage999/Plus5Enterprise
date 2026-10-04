# Phase 6.5 — Evidence-capable task metadata within Material

## Status

**FINAL LOCKED — odobreno 2026-10-04.**

## Cilj faze

Uvesti minimalni version-bound metadata procjenjivog zadatka unutar Material agregata tako da
budući valjani StudentAttempt može referencirati reproducibilni pedagoški snapshot, bez
preuranjenog Attempt runtimea, gradinga ili automatskog Evidence emissiona.

## Implementirano

- stable `AssessableTask` identitet unutar jednog Materiala;
- `AssessableTaskVersion` vezan uz točnu Draft/Active/Superseded `MaterialVersion` granicu;
- prompt, canonical task type code, redoslijed, Difficulty 1..5, EvidenceType, pozitivan
  `MaxPoints` i answer/evaluation-criterion semantika;
- leaf-only M:N prema Knowledge Components iz Published ili Retired KnowledgeModel verzije;
- composite same-Material FK-ovi, restrictive deleteovi, unique i CHECK constrainti;
- SQL triggeri za Draft-only mutacije, immutable objavljeni snapshot i obavezni Knowledge
  target prije aktivacije MaterialVersiona;
- aditivna `AddAssessableTaskMetadata` EF migracija bez seeda ili backfilla;
- postojeći owner/share-scoped Material detail query/API proširen task snapshotovima;
- Screen 4.2 `Zadaci i procjena` kartice te pošteno prazno stanje;
- reproducibilni real-login/real-API desktop/mobile visual gate.

## Sigurnost i integritet

- Teacher identitet i owner/share autorizacija ostaju isključivo server-side;
- Task, TaskVersion i MaterialVersion ne mogu prijeći Material granicu;
- direct evidence target ne može biti Draft-model ili parent komponenta;
- Active/Superseded snapshot i njegovi mapping retci ne mogu se retroaktivno mijenjati;
- storage key, checksum i scan detalji nisu dodani API responseu;
- pregled Materiala ili Task metadataka ne stvara EvidenceEvent.

## Test evidence

| Gate | Rezultat |
|---|---|
| Backend Release build | PASS — 0 warninga, 0 grešaka |
| Phase query/domain/API-mapping testovi | PASS — 8/8 |
| Fokusirani stvarni SQL migration/integrity testovi | PASS — 2/2 |
| EF pending-model check | PASS — nema model drifta |
| Backend regression | PASS — 207 prošlo, 47 očekivanih SQL opt-in skipova, 0 palo |
| Architecture | PASS — 4/4 |
| Frontend Materials ciljano | PASS — 7/7 |
| Frontend regression | PASS — 77/77 |
| lint/typecheck/production build | PASS |
| `.NET format --verify-no-changes` | PASS |
| Dependency audit | N/A — dependency graph nije mijenjan |
| Docker migration/runtime/health | PASS — migration exit 0; API live/ready i frontend HTTP 200 |
| Non-root runtime | PASS — API `uid=1654(app)`, frontend `uid=101(nginx)` |
| Canonical visual gate | PASS — 1536×1024 i 390×844, bez overflowa/grešaka/writeova |

## Scope discipline

- nema Task authoring/import UI-ja ili write endpointa;
- nema answer-option/response modela, StudentAttempta, gradinga ili generic Evidence API-ja;
- nema Evidence emittera ni promjene mastery/readiness projekcija;
- nema storage adaptera, Lesson/Homework/Board integracije, usage analitike ili AI authoringa;
- nije uveden izmišljeni zatvoreni production katalog TaskType vrijednosti.

## Dokumentacija i evidence

- `ASSESSABLE_TASK_METADATA.md`;
- ADR-0025 u `DECISION_LOG.md`;
- `MATERIAL_DETAIL.md` Phase 6.5 read extension;
- `visual-acceptance/phase-6.5/README.md` i `measurements.json`;
- `ROADMAP.md`, `PERSISTENCE.md` i `DOMAIN_GLOSSARY.md`.

## Točna početna točka za sljedeću fazu

Phase 6.6 ostaje zaseban import/upload workflow. Ne smije zaobići private object-storage,
validation/scanning i Clean-only activation contract iz 6.1 niti automatski stvarati Task,
Knowledge mapping ili Evidence bez zasebno odobrenog authoring contracta.
