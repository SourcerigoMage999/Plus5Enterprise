# Phase 9.2 — Lesson Builder AI privacy, explainability, confirmation and audit contract

## Status

**FINAL LOCKED — odobreno 2026-10-30.**

## Cilj faze

Zaključati provider, trošak, Student/Group privacy, fairness, explainability, Teacher
confirmation, retention, moderation, local feedback-learning i audit granice Lesson Builder
AI-ja prije bilo kakve runtime implementacije.

## Implementirano

- definiran je provider-neutral `ILessonBuilderAiAssistant` s OpenAI Responses API / EU v1
  adapter boundaryjem;
- zaključan je default-off, explicit-action-only i no-in-app-payment cost model;
- definiran je server-side context builder s PII/sensitive-data strippingom;
- bounded su readiness, Evidence, previous lesson, homework, interest i support inputi;
- zaključani su individual/group scope, ephemeral pseudonimi i cross-student leakage zaštita;
- definirani su strict structured outputs, explainability, provenance i missing-data pravila;
- zaključani su moderation, 24-satni suggestion retention i Accept/Edit/Reject/Regenerate/Undo
  lifecycle;
- Teacher preference learning je lokalni opt-in/resettable profil bez Student memoryja;
- definirani su audit, rate limit, payload, timeout, retry i manual fallback;
- ADR-0029 bilježi trajnu security/architecture odluku.

## Namjerno nije implementirano

- provider port/adapter, configuration, SQL entity/migracija ili API;
- frontend AI panel, runtime request ili plaćeni provider poziv;
- Student-specific memory/profiling, fine-tuning ili provider training feedback;
- web/tool/file/computer pristup i provider persistence;
- binary generation/storage ili automated grading;
- autonomous plan mutation, Ready transition, LessonSession, Attempt ili Evidence.

## Promijenjene / dodane datoteke

| Datoteka | Vrsta promjene | Razlog |
|---|---|---|
| `docs/LESSON_BUILDER_AI.md` | added | canonical Phase 9.2 contract |
| `docs/DECISION_LOG.md` | changed | ADR-0029 |
| `docs/OPEN_QUESTIONS.md` | changed | zatvoreni Lesson Builder AI/privacy i feedback-learning gateovi |
| `docs/ROADMAP.md` | changed | Phase 9.2 status i acceptance |
| `docs/summaries/PHASE_9.2_LESSON_BUILDER_AI_SUMMARY.md` | added | phase evidence i handoff |

## Domain / database promjene

- Novi runtime entiteti/value objecti: nema; budući suggestion/preference model je dokumentiran.
- Promijenjena runtime pravila: nema.
- Migracije i backfill: nema.

## API promjene

Nema. Budući API mora biti Teacher-only, owner-scoped, CSRF/bounded/rate-limited i koristiti
aktualni LessonPlanVersion rowversion za canonical write.

## Frontend promjene

Nema. Phase 9.2 nema ekran ni visual gate.

## Security / authorization

Contract zabranjuje direct PII/sensitive/free-form input, raw Evidence/history, cross-student
leakage, hidden mutation, provider persistence/tools i Student-specific learning. Manualni
Lesson Builder ostaje potpuno funkcionalan kad je AI isključen ili nedostupan.

## Testovi

| Naredba / suite | Rezultat |
|---|---|
| `git diff --check` | PASS |
| Build/test/SQL/frontend/visual | N/A — DOCS_ONLY faza |
| Full regression | N/A — nema runtime ni shared-contract diffa |
| Dependency audit | N/A — dependency graph nije promijenjen |
| Docker infrastructure validation | N/A — infrastructure unchanged |

## Self-review

- [x] scope nije proširen izvan Phase 9.2
- [x] SA odluka prenesena je bez novih business pretpostavki
- [x] Phase 9.1 FINAL LOCKED contract nije mijenjan
- [x] nema provider poziva, troška ni runtime/schema promjene
- [x] Phase 9.3 handoff je precizan

## Arhitekturne odluke

- ADR-0029 — privacy-minimized default-disabled Lesson Builder AI suggestions.

## Poznati rizici / tehnički dug

- Buduća implementacija mora testirati actual serialized provider payload, ne samo mapping
  DTO, kako bi dokazala PII i cross-student leakage granice.
- Availability ZDR-a ovisi o produkcijskom OpenAI accountu, ali nije funkcionalni preduvjet.

## Otvorena pitanja

Nema blockera za Phase 9.3. Provider key provisioning i konkretni runtime deployment ostaju
operativni preduvjeti samo kada kasnija faza stvarno implementira/uključi AI.

## Točna početna točka za sljedeću fazu

Phase 9.3 smije formalizirati versioned KB-001–KB-025 katalog i applicability pravila nad
zaključanim LessonPlan/Activity modelom i ovim bounded suggestion/privacy contractom. Ne
implementirati provider adapter, AI endpoint ili UI prije podfaze koja ih eksplicitno traži.
