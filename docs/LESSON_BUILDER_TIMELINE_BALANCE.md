# Phase 9.5 — Suggested lesson structure timeline and balance check

## Status

**Phase 9.5 — FINAL LOCKED.**

Ovaj contract formalizira Screen 5.2 nad zaključanim Phase 9.1–9.4 modelom. `docs/` ostaje
source of truth. Phase 9.5 ne uvodi AI provider call, plaćeni servis, Student inference,
`Attempt`, `EvidenceEvent`, Mastery ili Readiness write.

## Authority i no-cost pravilo

Kada je `LessonBuilderAi:Enabled=false`, početni prijedlog stvara isključivo server policy
`SYSTEM_BASELINE_V1`. Policy ne poziva OpenAI niti drugi provider, ne troši tokene i ne šalje
Student/Guardian podatke izvan aplikacije. PLUS 5 ne uvodi AI billing, credits ni in-app
naplatu. UI mora prijedlog nazvati **Početni prijedlog**, a AI prikazati kao isključen.

Preview nije canonical sadržaj. Učitavanje ekrana ne piše aktivnosti. Teacher može prijedlog
odbiti i ostaviti Draft s nula aktivnosti. Tek eksplicitni `Primijeni prijedlog` atomarno
stvara canonical `LessonActivity` retke.

## Target duration

`LessonPlanVersion.TargetDurationMinutes` je canonical snapshot i vrijedi `5..480`:

- plan povezan sa Sessionom inicijalno kopira točno `EndsAtUtc - StartsAtUtc`;
- plan bez Sessiona inicijalno koristi 60 minuta;
- kasnija promjena Sessiona ne mijenja plan;
- Teacherova ručna promjena targeta ostaje canonical i ne auto-revertira se.

Promjena targeta prije Apply ponovno izračunava samo preview. Nakon Apply promjena targeta ne
mijenja aktivnosti, nego može dati Time warning.

## Deterministički baseline

Za 60 minuta policy vraća točno:

| Sort | Aktivnost | Purpose | KB | Minute |
|---:|---|---|---|---:|
| 0 | Small Talk | `Engagement` | KB-001 Published v1 | 5 |
| 1 | Aktivacija predznanja | `ActivatePriorKnowledge` | KB-002 Published v1 | 10 |
| 2 | Objašnjenje novog gradiva | `Instruction` | KB-004 Published v1 | 15 |
| 3 | Interaktivni zadaci | `Practice` | KB-005 Published v1 | 20 |
| 4 | Provjera razumijevanja | `CheckForUnderstanding` | bez KB-a | 10 |

KB-006 ostaje Draft i ne smije biti referenciran. Provjera razumijevanja nije Quick Check,
`AssessableTask`, `Attempt` ni `EvidenceEvent`. Small Talk nema KC mapping; ostale četiri
aktivnosti nakon Apply kopiraju sve plan-level exact Knowledge Component mappinge.

Za druge targete koristi se weight vektor `1/2/3/4/2`, najmanje jedna minuta po aktivnosti i
deterministički largest-remainder raspored. Tie se rješava uzlaznim `SortOrderom`, a zbroj mora
točno odgovarati targetu.

## Knowledge Block runtime katalog

Migration-deployed katalog stvara System roots KB-001–KB-025 i v1 snapshote. KB-001–KB-005
su Published, a KB-006–KB-025 Draft. Novi activity reference smije pokazivati samo na exact
current Published verziju Active root-a. Published/Retired snapshot je SQL-zaštićen od in-place
semantičke mutacije.

Published duration envelopes za baseline su KB-001 `3/5/10`, KB-002 `5/10/15`, KB-003
`5/10/20`, KB-004 `8/15/25` i KB-005 `10/20/30`. Trajanje izvan raspona daje
`KB_DURATION_BELOW_RECOMMENDED` ili `KB_DURATION_ABOVE_RECOMMENDED`; nije hard reject.
Participant je `Both`. Homework mode je `RequiresHomeworkContext` samo za KB-003, `Optional`
za KB-005, a za KB-001/002/004 `NotRelevant`. Assessment capability je
`MayContainAssessment` za KB-003/005 i `NotAssessment` za KB-001/002/004.

SYSTEM_BASELINE_V1 ne koristi KB-003 jer Phase 9.5 nema homework runtime/context. Redoslijed
čuva preporuke: KB-001 blizu početka, KB-002 prije KB-004/005 i KB-004 prije KB-005. To su
warning/preporuke, ne hard reorder authority.

## LessonActivity i Apply transakcija

`LessonActivity` ima stabilni ID, `LessonPlanVersionId`, optional exact
`KnowledgeBlockVersionId`, contiguous `SortOrder`, bounded Name, `Purpose` i `DurationMinutes`.
`LessonActivityPurpose` je zatvoren skup `Engagement | ActivatePriorKnowledge | Instruction |
Practice | CheckForUnderstanding | Other`.

Apply je dopušten samo owneru nad praznim Draftom. U jednoj serializable transakciji ponovno
se provjeravaju ownership, Draft, rowversion i exact current Published KB verzije; zatim se
stvara pet aktivnosti, na aktivnosti 1–4 kopiraju plan-level KC mappingi, parent version dobiva
authoring update i sve se zajedno commita. Stale write vraća `409 concurrency_conflict`.
Ponovni Apply vraća `lesson_structure_already_applied`; nema partial apply. Regeneration je
Phase 9.8.

## Balance read model v1

Balance nije score niti predikcija. Svako područje vraća
`Satisfied | Warning | NotEvaluated`:

- `Goal`: Satisfied kada postoji valjan plan goal;
- `KnowledgeCoverage`: svaki plan-level KC mora biti na barem jednoj aktivnosti;
- `Time`: zbroj top-level aktivnosti uspoređuje se s targetom, bez auto-resizea;
- `Variety`: najmanje tri aktivnosti i dva Published KB codea ili jedan KB + custom purpose;
- `UnderstandingCheck`: postoji `CheckForUnderstanding` purpose ili budući valjan TaskVersion;
- `MaterialRelevance`: u Phase 9.5 uvijek `NotEvaluated`.

Broj Material kandidata prikazuje se odvojeno. Kandidat je ownerov ili podijeljen s `Use`
permissionom, Active/current verzija s Clean fileom i najmanje jednim exact KC overlapom.
Broj ne znači attachment, izbor ni procjenu relevantnosti. Baseline ne attach-a MaterialVersion
i ne stvara AssessableTaskVersion.

## API i sigurnost

Svi endpointi su Teacher-only i owner-scoped:

- `GET /api/v1/lesson-plans/{lessonPlanId}/structure/` — preview ili canonical struktura;
- `PUT /api/v1/lesson-plans/{lessonPlanId}/structure/target-duration` — CSRF + rowversion;
- `POST /api/v1/lesson-plans/{lessonPlanId}/structure/apply` — CSRF + rowversion + atomic Apply.

Missing, foreign i archived plan vraćaju isti `404 lesson_structure_not_found`. Klijent ne
šalje Teacher ID. Write requestovi ne prihvaćaju activity/KB/KC graph iz browsera; server ga
izvodi iz zaključane politike i canonical plana.

## UI acceptance i granice

Responsive `/lesson-plans/:lessonPlanId/structure` prikazuje goal/participant kontekst,
target duration, timeline, exact KC kontekst, šest balance područja, Material candidate
availability i Apply. Minimalni visual gate koristi stvarni login/API/SQL, desktop 1536×1024,
mobile 390×844, relevantni preview state, bez overflowa i browser/page errors.

Izvan scopea ostaju AI runtime/personalizacija, full Material selection/attachment, KB picker,
arbitrary activity editor/regeneration, Ready, Board/LessonSession runtime, Attempt,
EvidenceEvent, Mastery/Readiness i predicted grade.
