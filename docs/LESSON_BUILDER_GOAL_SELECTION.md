# Lesson Builder — odabir cilja sata

## Status

**Phase 9.4 — FINAL LOCKED — odobreno 2026-10-31.**

Ovaj contract implementira Screen 5.1 nad zaključanim Phase 9.1 Lesson Plan modelom,
Phase 9.2 AI/privacy granicama i Phase 9.3 Knowledge Block katalogom. Mjerodavni izvori su
`source_specs/5.1_Cilj_sata.md`, canonical PNG i `LESSON_BUILDER_DOMAIN.md`.

## Opseg

Teacher započinje pripremu sata tako da:

1. odabere točno jednog aktivnog vlastitog učenika ili aktivnu vlastitu grupu;
2. opcionalno veže postojeći budući termin istog Teachera i istog participant konteksta;
3. unese glavni cilj sata od 1 do 300 znakova;
4. odabere najmanje jedan Knowledge Component iz aktivne komponente objavljenog
   Knowledge Modela;
5. atomarno stvori `LessonPlan`, njegovu Draft verziju 1 i odabrane KC veze.

Transportni maksimum od 50 Knowledge Componenta ograničava request i nije pedagoško pravilo.
Uspješan create vraća `201` i identifikatore plana i Draft verzije. Phase 9.4 ne mijenja Draft
nakon stvaranja i ne postavlja `CurrentReadyVersionId`.

## Domain i persistence

`LessonPlan` je stabilni owner-scoped root s participant XOR granicom:

```text
StudentId != null XOR GroupId != null
```

`SessionId` je nullable, ali kada postoji mora pripadati istom Teacheru i istom Student/Group
kontekstu. `LessonPlanVersion` ima pozitivan `VersionNumber`, `Draft | Ready | Superseded`
status, obavezni cilj i SQL `rowversion`. Root također ima SQL `rowversion`.

`LessonPlanVersionKnowledgeComponents` čuva exact KC reference. Novi mapping dopušten je samo
Draft verziji i samo na `Active` komponentu `Published` Knowledge Modela. Ready/Superseded
snapshot i njegove veze su immutable. Restrictive FK-ovi, unique version/status indeksi i SQL
triggeri štite ove invariante i izvan aplikacijskog sloja:

| SQL | Invariant |
|---:|---|
| `51301` | Session mora odgovarati Teacheru i participant kontekstu |
| `51302` | current-ready mora biti Ready verzija istog plana |
| `51303` | Ready/Superseded verzija je immutable |
| `51304` | mapping Ready/Superseded verzije je immutable |
| `51305` | novi mapping zahtijeva Draft + Published/Active KC |

Create se izvršava u jednoj serializable transakciji. Missing/foreign/inactive participant ne
otkriva tuđe podatke. Candidate query primjenjuje owner scope prije pretrage i paginacije.

## API

Svi endpointi zahtijevaju aktivnu Teacher sesiju:

| Metoda i ruta | Svrha |
|---|---|
| `GET /api/v1/lesson-plans/participants` | paginirani owner-scoped Student/Group kandidati |
| `GET /api/v1/lesson-plans/sessions` | budući Scheduled termini odabranog participant konteksta |
| `GET /api/v1/lesson-plans/knowledge-components` | paginirani Published/Active KC kandidati |
| `POST /api/v1/lesson-plans/` | CSRF-protected atomarni create Draft plana |

Stabilni business kodovi su `lesson_goal_invalid_request`,
`lesson_goal_participant_unavailable`, `lesson_goal_session_mismatch`,
`lesson_goal_knowledge_unavailable`, `lesson_goal_reference_not_found` i
`concurrency_conflict`.

## Frontend i AI granica

Ruta `/lesson-plans` prikazuje responsive Screen 5.1: Student/Group switch, owner-scoped
odabir, optional Session, 300-character goal, KC search/selection i kontekstni sažetak.
Loading/error/empty ponašanje ne prikazuje izmišljene business podatke.

Phase 9.2 AI je defaultno isključen. Phase 9.4 zato ne poziva providera, ne šalje Student
podatke trećoj strani i ne stvara obaveznu ni in-app naplatu. UI jasno kaže da Teacher ručno
unosi canonical cilj. Planiranje ne stvara `Attempt`, `EvidenceEvent` niti mijenja
Mastery/Readiness.

## Izvan opsega

- AI prijedlog i explainability runtime;
- suggested timeline i balance check iz Phase 9.5;
- activity/material editor iz Phase 9.6–9.8;
- Ready potvrda i publish flow iz Phase 9.9;
- Board/Lesson Session runtime, Attempt i Evidence.
