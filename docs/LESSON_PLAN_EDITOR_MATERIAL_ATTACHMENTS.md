# Lesson Plan editor and immutable material-version attachment

## Status

**Phase 9.6 — FINAL LOCKED — odobreno 2026-11-04.**

Ovaj dokument zaključava runtime granicu Screena 5.3 nad Phase 9.1–9.5 Lesson Plan
agregatom. `docs/` ostaje source of truth; izvorni `5.3 Plan sata` i canonical PNG određuju
namjeru ekrana, a zaključani domain/security contracti imaju prednost nad još
neimplementiranim elementima mockupa.

## 1. Scope

Phase 9.6 uvodi:

- Teacher-only read model cijelog postojećeg Draft plana;
- prikaz participant/session/goal/KC/timeline/balance konteksta;
- odabir dostupne exact `MaterialVersion` iz biblioteke;
- attach i detach exact verzije na postojeći `LessonActivity`;
- stvarni Screen 5.3 na `/lesson-plans/:lessonPlanId/editor`.

Phase ne dodaje novu aktivnost, Knowledge Block picker, activity detail/edit, bilješke,
regeneration, Quick Check, Ready potvrdu, Board runtime, Attempt ili Evidence.

## 2. Canonical persistence

`LessonActivityMaterialVersion` je čisti version-bound junction:

```text
LessonActivityId      PK/FK → LessonActivity
MaterialVersionId     PK/FK → MaterialVersion
```

Composite ključ sprječava duplikat. Oba FK-a koriste `Restrict`. Attachment ne kopira
storage key, signed/public URL, binary ni browser filename i ne prati stabilni `Material`.
Canonical referenca je uvijek exact `MaterialVersionId`.

Novi attachment dopušten je samo kada vrijedi sve:

- plan pripada autentificiranom Teacheru i nije arhiviran;
- `LessonPlanVersion.Status == Draft`;
- Material je Active, nije arhiviran i odabrana verzija je njegov current Active snapshot;
- verzija ima najmanje jednu `Clean` datoteku;
- Teacher je owner ili Material ima `Shared` visibility i eksplicitni `Use` grant.

Kasniji Material versioning ne mijenja postojeći attachment. Exact povijesna verzija ostaje
prikazana i uklonjiva dok je plan Draft; Ready/Superseded graph je immutable.

SQL trigger `TR_LessonActivityMaterialVersions_Validate` provodi lifecycle i access
integrity neovisno o application sloju:

- `51320` — mutation nije nad Draft planom;
- `51321` — nova exact verzija nije current/Active/Clean ili Teacher nema owner/Use pristup.

## 3. API

```http
GET    /api/v1/lesson-plans/{lessonPlanId}/editor/
GET    /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/material-candidates
POST   /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/materials/{materialVersionId}
DELETE /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/materials/{materialVersionId}
```

Svi endpointi zahtijevaju aktivnu Teacher sesiju. Writeovi zahtijevaju CSRF i parent
`LessonPlanVersion.RowVersion`; stale write vraća `409 concurrency_conflict`. Missing,
foreign i archived plan imaju isti `404 lesson_editor_not_found` rezultat.

Candidate query primjenjuje access/lifecycle scope prije searcha, sortiranja i paginacije.
Search je bounded na 120 znakova, page size na 1–50. Exact-KC overlap je informativan broj,
ne readiness, preporuka ili automatski izbor.

## 4. Atomicity i concurrency

Attach/detach se izvodi u serializable transakciji:

```text
owner + Draft + rowversion check
→ exact MaterialVersion availability check
→ junction INSERT/DELETE
→ LessonPlanVersion authoring timestamp/rowversion update
→ COMMIT
```

Ne postoji partial attachment. Duplicate attach i missing detach vraćaju stabilni business
conflict bez skrivenog overwritea.

## 5. Presentation i balance semantika

UI prikazuje stvarne aktivnosti i njihove exact Material verzije (`vN`). Candidate picker
pokazuje owner/shared dostupne current verzije i exact broj KC podudaranja. Povijesna
attached verzija jasno je označena i ne auto-upgradea se.

`MaterialRelevance` ostaje:

- `NotEvaluated` kada nema attachmenta;
- `Warning` ako aktivnost s KC ciljem ima materijal bez exact KC overlap-a;
- `Satisfied` kada svi takvi attachmenti imaju barem jedno exact podudaranje.

To nije overall score, readiness, predicted grade ni AI preporuka. PLUS 5 ne radi provider
poziv i ne uvodi dodatnu ili in-app naplatu.

## 6. Security i privacy

- Teacher identity dolazi samo iz autentificirane sesije;
- owner scope se provodi server-side prije dohvata aggregatea;
- recipient/user directory ne postoji u ovom flowu;
- storage ključevi, scan detalji i signed/public URL-ovi nisu dio responsea;
- browser ne šalje TeacherId, Material ownera ni canonical pedagogical rezultat;
- planiranje ne stvara `StudentAttempt`, `EvidenceEvent`, Mastery ili Readiness mutation.

## 7. Visual acceptance

Minimalni stvarni browser gate koristi canonical `5.3 plan sata.png` i dokazuje:

- desktop 1536×1024;
- mobile 390×844;
- stvarni login, API i exact attachment state;
- bez document horizontal overflowa i browser/page grešaka;
- shell/profile i akcije se ne preklapaju;
- budući 9.7–9.9 controlsi jasno su disabled, a ne lažno funkcionalni.

Evidence: `visual-acceptance/phase-9.6/`.

## 8. Namjerno odgođeno

- add/duplicate/delete/reorder arbitrary aktivnosti i KB picker — Phase 9.7;
- activity detail/edit, notes i regeneration — Phase 9.8;
- Draft → Ready validacija i potvrđeni plan — Phase 9.9;
- Board/LessonSession runtime, Attempt i Evidence — Phase 10+;
- AI personalization/provider runtime — ostaje default-off i izvan ovog scopea.
