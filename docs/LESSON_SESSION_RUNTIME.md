# Phase 10.1 — Lesson Session Runtime

## Status

**IMPLEMENTED / REVIEW READY — 2026-11-19.**

Phase 10.1 uvodi Teacher Board i trajni `LessonSession` s pouzdanom recovery granicom.
`LessonPlanVersion` je planirani snapshot, `Session` je kalendarski termin, a
`LessonSession` jedno stvarno izvođenje. Pojmovi nisu međusobno zamjenjivi. Phase 0–9
FINAL LOCKED contracti ostaju nepromijenjeni.

## 1. Poslovne odluke

### Ponovno izvođenje plana

Jedan exact Ready ili povijesni Superseded `LessonPlanVersion` može ostati referenca više
različitih povijesnih izvođenja. Novi start smije koristiti samo verziju koja je u trenutku
starta trenutačni Ready snapshot aktivnog, nearhiviranog, owner-scoped plana. Kasniji
Superseded status ili arhiviranje plana ne mijenja postojeći runtime zapis.

### Više aktivnih sati

Teacher smije imati više `InProgress` LessonSessiona za različite kalendarske termine ili
jasno različite unscheduled kontekste. Board ne bira proizvoljno jedan aktivni sat nego
prikazuje owner-scoped recovery chooser.

- najviše jedan non-voided LessonSession po `CalendarSessionId`, uključujući Completed;
- za unscheduled plan najviše jedan InProgress po `(Teacher, LessonPlan, participant)`;
- ponovni isti start-intent je idempotentan, a drugi različiti intent za isti nedopušteni
  runtime kontekst završava konfliktom;
- različiti Teacheri nikada ne dijele runtime instancu.

### Kalendarska granica

`LessonSession` čuva nullable `CalendarSessionId` i snapshot zakazanog početka, završetka i
vremenske zone. Novi scheduled start zahtijeva isti Teacher i isti Student XOR Group te
kalendarski status `Scheduled`. Start atomarno postavlja `Session` na `InProgress`.
`Cancelled` ili `Held` termin ne može započeti novi runtime.

Započeti runtime ne briše se, ne premješta, ne reparenta i ne završava automatski zbog
naknadne promjene kalendara. Reschedule/cancel započetog termina ostaje blokiran postojećim
Session lifecycleom; reconciliation i završni `Held/Cancelled` prijelaz pripadaju Phase
10.5 ili zasebnom repair contractu.

### Prekid, završetak i void

`InProgress` je trajno stanje. Gubitak veze, zatvaranje taba, sleep ili pad browsera nisu
domenski prijelazi. UI smije prikazati `ConnectionLost/Recovering`, ali server čuva
`InProgress` zapis. Ne uvodi se session-wide `Paused` status.

`Completed` nastaje isključivo eksplicitnim Phase 10.5 `Save and finish` commandom. `Voided`
je iznimna auditirana invalidacija, nikad brisanje. Phase 10.1 ne izlaže Complete ili Void
endpoint i ne uvodi izmišljenu Administrator ulogu. Completed-session void i Evidence
invalidation ostaju Phase 10.5/11 gate.

### Save semantika

`Saved` vrijedi tek nakon server validacije i SQL commita. Odgovor nosi `LessonSessionId`,
monotono rastući `Revision`, server timestamp, canonical latest state i `OperationId`.

- duplicate `(LessonSessionId, OperationId)` s istim canonical fingerprintom vraća originalni
  rezultat bez ponavljanja side effecta;
- isti OperationId s drugim fingerprintom se odbija;
- stale expected revision vraća `409` i safe latest-state reload path;
- offline akcije su `Unsynced`, ne `Saved`;
- automatski retry mora zadržati isti OperationId i fingerprint;
- kasni write nakon terminalnog stanja odbija se bez resurrect/reordering ponašanja.

## 2. Domain i persistence model

### LessonSession

```text
Id
TeacherAccountId
LessonPlanId
LessonPlanVersionId          # exact Ready snapshot
CalendarSessionId?           # postojeći Session
StudentId XOR GroupId
Status: InProgress | Completed | Voided
ScheduledStartsAtUtc?
ScheduledEndsAtUtc?
ScheduledTimeZoneId?
StartedAtUtc
CompletedAtUtc?
VoidedAtUtc?
VoidReason?
LastCommittedAtUtc
Revision BIGINT
RowVersion
```

Ne duplicira se mutable Student/Group PII. Participant i ownership moraju odgovarati Ready
planu, a calendar snapshot služi povijesnom prikazu.

### LessonSessionActivity

```text
Id
LessonSessionId
SourceLessonActivityId?      # null za budući ad-hoc
Origin: Planned | AdHoc
SortOrder
Status: Pending | InProgress | Paused | Completed | Partial | Skipped
exact activity descriptor snapshot
StartedAtUtc?
CompletedAtUtc?
```

Phase 10.1 materializira sve planirane aktivnosti kao `Pending`, prikazuje ih i dopušta
revision-safe organiziranje timelinea. Ad-hoc kreiranje i stvarno izvođenje/status mutacije
aktivnosti pripadaju Phase 10.2. Originalni LessonPlan snapshot nikada se ne mijenja.

### Exact reference junctions

`LessonSessionActivityMaterialVersion` i `LessonSessionActivityAssessableTaskVersion` čuvaju
exact FK reference kopirane iz Ready snapshota. Nema automatskog upgradea. Buduća promjena
materijala tijekom izvođenja mora appendati usage/provenance događaj, ne prepisati povijest.

### LessonSessionOperation

DB-backed idempotency ledger čuva owner/session, client-generated OperationId, operation
type, normalized request hash, expected/applied revision, committed timestamp i bounded
response podatke potrebne za rekonstrukciju rezultata. Unique je `(LessonSessionId,
OperationId)`. Start intent ima Teacher-scoped operation ID i zapisuje se u istoj transakciji.
Vanjski cache nije correctness authority.

### LessonSessionAuditEvent

Append-only audit čuva operation identity, actor, action, dopušteni old/new status, UTC
vrijeme i minimalne bounded metapodatke. Ne sprema Student odgovore, raw free text ni tajne.
Obični feature endpointi ne mijenjaju ni brišu audit povijest.

## 3. Atomarni start i SQL invarianti

Jedna serializable transakcija mora:

1. validirati Teacher ownership, aktivni plan i trenutačni Ready snapshot;
2. validirati participant i optional calendar kontekst;
3. zaključati calendar occurrence i aktivne uniqueness granice;
4. idempotentno evidentirati start intent;
5. stvoriti `LessonSession`;
6. materializirati sve aktivnosti u contiguous redoslijedu;
7. kopirati exact MaterialVersion i AssessableTaskVersion reference;
8. dodati start audit;
9. povezani `Session` atomarno prebaciti `Scheduled → InProgress`;
10. commitati bez djelomično vidljivog Boarda.

Za unscheduled plan ne stvara se lažni kalendarski zapis. Restrictive delete i same-owner FK
pravila ostaju obavezni.

SQL štiti najmanje:

- unique non-voided calendar association, uključujući Completed;
- unique active unscheduled Teacher/plan/participant kontekst;
- unique operation ID;
- unique `(LessonSessionId, SortOrder)`;
- najviše jednu planned kopiju `(LessonSessionId, SourceLessonActivityId)`;
- participant XOR i lifecycle/timestamp konzistenciju;
- exact runtime reference integrity.

Serializable locking ili SQL application lock štiti absent-row race; indeksi su posljednja
obrana. Mehanizam se dokazuje na stvarnom SQL Serveru.

## 4. Lifecycle, autorizacija i recovery

```text
NotCreated → InProgress       # Phase 10.1 StartLesson
InProgress → Completed        # samo Phase 10.5
InProgress → Voided           # deferred policy
```

Completed i Voided su terminalni. Nema reopena, hard deletea, rollbacka u Draft ili auto-end
ponašanja. Refresh/resume su read operacije. Prebacivanje između aktivnih sati nije write.

Board je Teacher-only, cookie-authenticated i owner-scoped. Writeovi koriste postojeći CSRF
policy, owner se nikad ne uzima iz requesta, a missing/foreign IDs dijele privacy-preserving
404. Concurrency vraća stabilni 409 s canonical latest-state read pathom.

Phase 10.1 ne stvara StudentAttempt, EvidenceEvent, Mastery/Readiness promjenu, attendance,
billing ili homework zapis.

## 5. API granica

- `POST /api/v1/lesson-plans/{lessonPlanId}/lesson-sessions/start` — exact current Ready,
  optional calendar Session i `startOperationId`; `201` novi, `200` idempotentni rezultat,
  `409` scheduling/active konflikt; CSRF obavezan.
- `GET /api/v1/lesson-sessions/active` — bounded owner-scoped active session chooser.
- `GET /api/v1/lesson-sessions/{id}` — authoritative Board read model s revisionom,
  statusima, ordered runtime aktivnostima i exact reference podacima.
- `POST /api/v1/lesson-sessions/{id}/mutations` — samo allowlisted Phase 10.1 typed mutation;
  v1 implementira runtime timeline reorder s OperationId i expected revisionom.

Nema generic JSON patcha ni Phase 10.1 endpointa za Complete, Void, Attempt, Evidence,
calendar cancellation ili participant reassign.

## 6. UI granica

- `/board` prikazuje recovery chooser kada postoji više aktivnih sesija i ne bira ih tiho;
- `/board/sessions/:lessonSessionId` prikazuje Teacher Board, plan/participant kontekst,
  vrijeme, ordered Pending timeline i server-backed save state;
- `Saving`, `Saved at <server time>`, `Offline — not saved` i `Conflict — review changes`
  proizlaze iz stvarnih server acknowledgementa;
- Summary/Finish, activity execution, StudentAttempt i live diagnostic kontrole ostaju jasno
  disabled/deferred do svojih faza;
- navigacija ili refresh ne završavaju sat.

## 7. Namjerne odgode

- activity start/pause/complete/partial/skip, ad-hoc runtime activity i Attempt — Phase 10.2;
- live diagnostic — Phase 10.3;
- live plan-vs-executed summary — Phase 10.4;
- explicit closure, Completed void i Evidence finalization/invalidation — Phase 10.5/11;
- audit/idempotency retention duration — budući privacy/retention contract.

## 8. Acceptance

- repeated Ready plan executions stvaraju zasebne runtime zapise bez izmjene plana;
- različiti calendar termini mogu biti aktivni, duplicate occurrence start ne stvara drugi red;
- Cancelled/Held ne mogu započeti novi runtime i scheduled start sync je atomaran;
- sve Pending aktivnosti i exact reference kopije nastaju atomski;
- idempotentni retry, fingerprint mismatch i concurrent duplicate start su dokazani;
- revision-safe mutation prihvaća točno jedan write, stale drugi vraća 409;
- recovery chooser i authoritative read ne mijenjaju stanje;
- owner/foreign/CSRF granice prolaze ciljane testove;
- nema Attempt/Evidence/Mastery/attendance/billing side effecta;
- stvarni SQL concurrency/rollback, desktop/mobile Board i overflow gate prolaze.
