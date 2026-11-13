# Phase 9.9 — Confirmed Lesson Plan / Ready state

## Status

**Phase 9.9 — FINAL LOCKED — 2026-11-13.**

## Izvori

- `source_specs/5.6_Potvrdena_priprema_Sat_je_spreman.md`
- canonical `5.6 Potvrđena priprema.png`
- `LESSON_BUILDER_DOMAIN.md`
- `LESSON_PLAN_EDITOR_MATERIAL_ATTACHMENTS.md`
- `LESSON_ACTIVITY_DETAIL_EDITING_AI.md`

## Zaključani contract

`LessonPlanVersion` prelazi samo `Draft → Ready → Superseded`. Potvrda validira cijeli
verzionirani graph i u jednoj serializable transakciji postavlja novi Ready snapshot te
`LessonPlan.CurrentReadyVersionId`. Ako već postoji Ready verzija, ona u istoj transakciji
postaje Superseded. Stale plan ili version rowversion vraća `409` bez djelomičnog stanja.

Ready zahtijeva:

- owner-scoped, Active i non-archived `LessonPlan`;
- valjan Student XOR Group context i opcionalni isti-context `Session`;
- neprazan cilj;
- najmanje jednu aktivnost, contiguous `SortOrder` od nule i pozitivno trajanje;
- postojeće exact Knowledge Component/Knowledge Block reference;
- exact `MaterialVersion` reference, pri čemu svaka povezana verzija ima `Clean` datoteku;
- bez dangling child referenci.

Application provjeru ponavlja SQL trigger `TR_LessonPlanVersions_ProtectSnapshots`:

- `51350` — Draft graph nije Ready-validan;
- `51351` — nedopušten lifecycle prijelaz;
- `51303` — pokušaj promjene ili brisanja Ready/Superseded snapshota.

## Edit nakon potvrde

`Uredi pripremu` ne mijenja Ready snapshot. Server deep-clonea trenutni Ready graph u novu
Draft verziju: cilj, target trajanje, plan KC mappings, aktivnosti sa svim content/Board
poljima, activity KC mappings i exact MaterialVersion attachments. AI suggestion retci se
ne kopiraju. Stari Ready ostaje `CurrentReadyVersionId` dok nova Draft verzija nije uspješno
potvrđena.

## API

- `GET /api/v1/lesson-plans/{lessonPlanId}/confirmation/`
- `POST /api/v1/lesson-plans/{lessonPlanId}/confirmation/confirm`
- `POST /api/v1/lesson-plans/{lessonPlanId}/confirmation/edit`

Svi endpointi su Teacher-only. Missing, foreign i archived planovi dijele `404` bez
ownership oraclea. Writeovi zahtijevaju CSRF i optimistic concurrency. Validacijski problem
vraća `422`; stale ili lifecycle konflikt `409`.

## UI contract

Ruta `/lesson-plans/:lessonPlanId/confirmation` prikazuje sudionika, opcionalni termin,
cilj, verzionirane Knowledge Components, aktivnosti, trajanje, work mode, exact materijale,
balans i blokirajuće poruke. No-data nije prikazan kao izmišljeni postotak spremnosti.

Nakon potvrde prikazuje se `Sat je spreman`. `Pokreni sat na PLUS 5 Ploči` ostaje vidljiv,
ali disabled uz Phase 10 objašnjenje. Akcije koriste globalni
`ds-page-actions--below-global-profile` razmak i ne ulaze u profile/notification zonu.

## Izričito izvan scopea

- stvaranje `LessonSession` ili Board runtime;
- `Attempt`, `EvidenceEvent`, Mastery/Readiness mutacija ili Knowledge Model promjena;
- automatsko prihvaćanje AI prijedloga;
- overall readiness, predviđena ocjena ili drugi nepostojeći izračun;
- novi AssessableTask authoring/mapping model.
