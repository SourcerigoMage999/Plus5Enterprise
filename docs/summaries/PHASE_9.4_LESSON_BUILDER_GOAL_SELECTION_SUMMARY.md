# Phase 9.4 — Lesson Builder goal selection summary

## Status

**FINAL LOCKED — odobreno 2026-10-31.**

## Implementirano

- version-bound `LessonPlan`, Draft `LessonPlanVersion` i exact KC mapping persistence;
- Student XOR Group root, optional same-context Session i optimistic concurrency;
- Teacher-only participant/session/KC read API i CSRF-protected atomic create API;
- owner scope prije search/paginationa te Published/Active KC candidate scope;
- responsive `/lesson-plans` Screen 5.1 sa stvarnim Student/Group, Session, goal i KC flowom;
- honest AI-disabled/no-extra-cost stanje bez provider poziva;
- SQL triggeri za context, current-ready, snapshot i KC mapping integritet;
- canonical desktop/mobile visual acceptance nad stvarnim loginom, API-jem i SQL-om.

Završni architecture gate otkrio je staru izravnu API metadata referencu na Domain
`EvidenceType` kroz Presentation Quick Check contract. Granica je minimalno ispravljena
Application-owned enumom i Infrastructure mapiranjem, bez promjene zaključane Quick Check
semantike ili javnog numeric JSON contracta.

## Sačuvane odluke

- plan ima točno jednog Studenta ili Grupu i ostaje owner-scoped;
- Session je opcionalan, ali kada postoji mora odgovarati istom kontekstu;
- goal je obavezan i ograničen na 300 znakova;
- create stvara Draft v1; ne proglašava plan Ready;
- novi KC izbor koristi samo Active komponente Published modela;
- Ready/Superseded snapshotovi i njihovi mappingi ostaju immutable;
- planiranje ne stvara Attempt/Evidence niti browser-side readiness;
- AI ostaje default-disabled, provider-neutralan i bez obavezne/in-app naplate.

## Validation

| Gate | Rezultat |
|---|---|
| Release build | PASS — 0 warninga, 0 grešaka |
| Targeted backend | PASS — 4/4 ukupno; domain/query/create 3/3 i stvarni SQL 1/1 |
| SQL migration/runtime | PASS — migration apply, idempotent migrate i novi trigger/index invarijanti |
| Architecture | PASS — 4/4 |
| Presentation boundary regression | PASS — 7/7 targeted API testova |
| Frontend targeted | PASS — 2/2 |
| Frontend lint/typecheck/build | PASS |
| Visual acceptance | PASS — 1536×1024 i 390×844; errors 0; writes 0; overflow 0 |
| Full regression | N/A — targeted phase validation is sufficient |
| Dependency audit | N/A — dependency graph and lockfiles unchanged |
| Docker infrastructure validation | N/A — infrastructure unchanged |

Tijekom restorea prijavljeni su novi upstream advisories za postojeće transitive
`System.Security.Cryptography.Pkcs` i `System.Security.Cryptography.Xml` verzije. Trenutni
diff ih nije uveo, dependency graph nije mijenjan i prema STRICT VALIDATION BUDGET-u ostaju
zaseban maintenance/security finding za Solution Architecta.

## Dokumentacija

- `LESSON_BUILDER_GOAL_SELECTION.md`;
- `PERSISTENCE.md`, `API_CONVENTIONS.md` i `FRONTEND_FOUNDATION.md`;
- `visual-acceptance/phase-9.4/README.md`;
- `ROADMAP.md`.

## Namjerno nije implementirano

- AI suggestion runtime;
- timeline/balance, activity editor i Knowledge Block selection;
- Material attachment, Ready/publish i Board runtime;
- Attempt, EvidenceEvent ili Mastery/Readiness write.

## Sljedeća faza

Phase 9.5 smije implementirati suggested lesson structure timeline i balance check nad
zaključanim Phase 9.4 participant/session/goal/KC kontekstom, bez preuranjenog stvaranja
Attempta ili EvidenceEventa.
