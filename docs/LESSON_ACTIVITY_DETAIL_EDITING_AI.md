# Lesson Activity detail/edit and regeneration

## Status

**Phase 9.8 — FINAL LOCKED — 2026-11-07.**

Ovaj dokument zaključava Screen 5.5 i runtime granicu detalja, uređivanja, dupliciranja,
brisanja, redoslijeda i AI prijedloga jedne Draft `LessonActivity`. Vrijedi zajedno s
`LESSON_BUILDER_DOMAIN.md`, `LESSON_BUILDER_AI.md`, Phase 9.5–9.7 contractima i canonical
izvorom `5.5 Detalji - uređivanje aktivnosti`.

## 1. Scope

Phase 9.8 uvodi:

- owner-scoped detail/read i manualno uređivanje jedne aktivnosti;
- `WorkMode`, opis, pedagošku svrhu, Teacher/Student upute, privatne bilješke i Board flags;
- activity-level odabir iz već zaključanih plan Knowledge Componenta;
- dupliciranje, Draft-only brisanje i atomarni contiguous reorder;
- default-disabled AI prijedlog s Preview/Accept/Edit/Reject/Regenerate lifecycleom;
- stvarni responsive Screen 5.5 na
  `/lesson-plans/:lessonPlanId/activities/:activityId`.

Phase ne uvodi Confirm/Ready workflow, Board runtime, Attempt/Evidence, novi task attachment
model, AI billing, provider memory ili automatske AI pozive.

## 2. Canonical manual edit

Manualni Teacher write smije mijenjati:

- naziv, purpose, trajanje i `WorkMode`;
- opis i pedagošku svrhu;
- upute za učitelja i učenike;
- privatne bilješke;
- tri eksplicitna Board visibility flaga;
- activity Knowledge Component subset koji već pripada istoj LessonPlanVersion.

Write je dopušten samo nad owner-scoped ne-arhiviranim Draft planom i traži aktualni parent
`rowversion`. Missing, foreign i archived plan/aktivnost dijele isti owner-safe `404`.
Nevaljan payload vraća `400`, a stale write `409 concurrency_conflict`.

## 3. Duplicate, delete i reorder

Duplicate stvara novi `LessonActivity` odmah nakon izvora i duboko kopira:

- sva activity polja i Board flags;
- exact `KnowledgeBlockVersionId`;
- exact activity Knowledge Component mappinge;
- exact immutable `MaterialVersion` attachmente.

Delete je Draft-only i ponovno zatvara redoslijed bez praznina. Reorder prihvaća cijeli,
jedinstveni popis svih activity ID-eva iste verzije i atomarno postavlja contiguous
`SortOrder`. Sve tri operacije koriste serializable transakciju, CSRF i optimistic
concurrency. Dirty detail forma blokira duplicate/delete/navigation dok se promjene ne
spreme ili eksplicitno odbace.

## 4. AI regeneration authority

AI je pomoćni suggestion workflow, ne canonical write authority. Dopuštena izlazna polja su
isključivo:

```text
Name
PurposeDescription
Description
TeacherInstructions
StudentInstructions
WorkMode
```

AI ne smije promijeniti trajanje, enum `Purpose`, Knowledge Block/versionu, Knowledge
Componente, Material/Task reference, redoslijed, parent, Board flags ili Notes. `Notes` se ne
šalje provideru. Strict schema odbija dodatna polja i output veći od 16 KiB.

AI je defaultno isključen (`Enabled=false`). Manualni workflow radi bez providera i bez
troška. Nema automatskih poziva, PLUS 5 creditsa, in-app naplate ni obaveznog plaćenog
featurea. Ako deployment owner integraciju eksplicitno uključi vlastitim serverskim ključem,
vrijede zaključani OpenAI Responses EU endpoint, `gpt-6.1-sol`, `store=false`, bez tools/web/
file pristupa i timeout do 30 sekundi.

## 5. Suggestion lifecycle

```text
Generate → Pending (24 h)
Pending → Accepted | Edited | Rejected | Replaced | Expired
```

- Preview ne mijenja aktivnost.
- Accept/Edit ponovno provjeravaju ownership, Draft status i exact rowversion iz vremena
  generiranja; stale rezultat vraća `409`.
- Reject ne mijenja activity graph.
- Regenerate označava prethodni Pending zapis kao `Replaced` i stvara novi Pending zapis.
- Resolve/expire briše provider `ContentJson`; audit metapodaci ostaju.
- 10 requestova / 10 minuta i 50 requestova / 24 sata po Teacheru.

Server context builder šalje samo bounded instructional plan/activity/KC kontekst. Teacher i
Student/Group identitet, Notes, materijali, raw Evidence/history, credentials i storage
metapodaci nisu dio provider contexta.

## 6. API

```http
GET    /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}
PUT    /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}
POST   /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/duplicate
DELETE /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}
PUT    /api/v1/lesson-plans/{lessonPlanId}/editor/activities/reorder
PUT    /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/knowledge-components
POST   /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/ai/suggestions
GET    /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/ai/suggestions/{suggestionId}
POST   /api/v1/lesson-plans/{lessonPlanId}/editor/activities/{activityId}/ai/suggestions/{suggestionId}/resolve
```

Svi endpointi zahtijevaju Teacher sesiju. Svi writeovi zahtijevaju CSRF. AI create dodatno
koristi Teacher-scoped rate limit. Business greške koriste stabilne codeove:
`lesson_activity_invalid_request`, `lesson_activity_not_found`, `lesson_editor_not_draft`,
`concurrency_conflict`, `lesson_builder_ai_disabled`, `lesson_activity_ai_invalid_request`,
`lesson_activity_ai_invalid_output`, `lesson_activity_ai_suggestion_expired`,
`lesson_activity_ai_daily_limit` i `lesson_activity_ai_unavailable`.

## 7. Persistence i SQL integrity

Migracija `AddLessonActivityDetailEditingAi`:

- dodaje devet bounded/typed activity detail polja i `CK_LessonActivities_WorkMode`;
- backfilla postojeće retke na `Individual`, oba instruction Board flaga `true` i progress
  flag `false`;
- dodaje `LessonActivityAiSuggestions`, restrictive FK-ove, lifecycle/JSON/expiry CHECK
  constrainte i bounded indekse;
- SQL trigger `TR_LessonActivityAiSuggestions_Validate` (`51340`) odbija suggestion koji ne
  cilja owner-scoped Draft activity istog plana/verzije.

## 8. Presentation i visual acceptance

Screen čuva canonical shell, breadcrumb, plan/activity context, dominantne save/duplicate/
delete akcije, osnovna polja, upute, KC, materijale, Board flags, Notes i AI zonu. Desktop
koristi content + desnu pomoćnu zonu; mobile slaže sadržaj u jedan stupac bez globalnog
horizontalnog overflowa. Kad je AI isključen, UI to jasno kaže i ne prikazuje lažni Generate
CTA.

Minimalni stvarni browser evidence je u `visual-acceptance/phase-9.8/`.

## 9. Namjerno odgođeno

- Confirm/Ready validacija i lifecycle — Phase 9.9;
- novi AssessableTask attachment authoring contract;
- Board/LessonSession runtime, Attempt i Evidence — Phase 10+;
- provider pozivi u acceptanceu, mandatory AI, AI billing/credits ili in-app kupnja;
- cross-plan activity templates i automatsko prepisivanje historyja.
