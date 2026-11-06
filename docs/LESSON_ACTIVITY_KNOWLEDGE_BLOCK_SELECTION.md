# Lesson Activity / Knowledge Block selection

## Status

**Phase 9.7 — FINAL LOCKED — odobreno 2026-11-06.**

Ovaj dokument zaključava runtime granicu pomoćnog Screena 5.4 nad Phase 9.1–9.6 Lesson
Plan agregatom. `docs/` ostaje source of truth; izvorni `5.4 Odabir aktivnosti` i canonical
PNG određuju namjeru ekrana, a zaključani Knowledge Block i Lesson Builder contracti imaju
prednost nad elementima mockupa za koje ne postoji odobren persistence model.

## 1. Scope

Phase 9.7 uvodi:

- owner-scoped katalog stvarno dostupnih `KnowledgeBlockVersion` kandidata;
- bounded search i filter maksimalnog trajanja;
- participant, goal, Knowledge Component i raspoloživo-vrijeme kontekst;
- eksplicitno dodavanje odabranog bloka kao nove Draft `LessonActivity`;
- stvarni Screen 5.4 na `/lesson-plans/:lessonPlanId/activity-selection`.

Phase ne uvodi novi ActivityTemplate katalog, activity detail/edit, regeneration, Ready
potvrdu, Board runtime, Attempt, Evidence, AI provider poziv ili naplatu.

## 2. Candidate eligibility

Candidate query vraća samo exact current Published verziju Active `KnowledgeBlocka` kada:

- plan pripada autentificiranom Teacheru, nije arhiviran i njegova verzija je Draft;
- `ParticipantApplicability` odgovara Student/Group kontekstu ili je `Both`;
- `HomeworkMode` nije `RequiresHomeworkContext`, jer LessonPlan trenutno nema zaključan
  homework context;
- minimalno trajanje stane u opcionalni bounded filter;
- naziv ili stabilni KB code odgovara bounded searchu.

Draft KB-006–KB-025 nisu izloženi niti ih browser može poslati kao legalan izbor. UI prikazuje
broj postojećih aktivnosti iste exact verzije kao upozorenje; ponavljanje nije hard reject jer
zaključani katalog tretira ponovljeni tip kao balance warning.

Tab `Aktivnosti` ne simulira katalog koji još nema persistence/source contract. Prikazuje
pošteno no-catalog stanje i upućuje na odobrene Knowledge Blockove.

## 3. API

```http
GET  /api/v1/lesson-plans/{lessonPlanId}/editor/activity-candidates
POST /api/v1/lesson-plans/{lessonPlanId}/editor/activities
```

GET prihvaća opcionalni `search` do 120 znakova i `maximumDurationMinutes` od 1 do 480.
POST body sadrži samo `knowledgeBlockVersionId` i parent `rowVersion`; zahtijeva Teacher
sesiju i CSRF. Missing, foreign i archived plan dijele owner-safe `404
lesson_editor_not_found`. Nedostupan ili lifecycle-nevaljan blok vraća `409
lesson_knowledge_block_unavailable`, a stale parent vraća `409 concurrency_conflict`.

## 4. Canonical write

Eksplicitni Add izvodi se u serializable transakciji:

```text
owner + Draft + rowversion check
→ ponovna provjera current Published/Active i participant/homework eligibilityja
→ INSERT LessonActivity na max SortOrder + 1
→ kopiranje trenutno odabranih plan Knowledge Components na novu aktivnost
→ LessonPlanVersion authoring timestamp/rowversion update
→ COMMIT
```

Naziv i trajanje dolaze iz exact `KnowledgeBlockVersion` snapshot semantike. Purpose mapping
za Published v1 katalog ostaje isti kao u Phase 9.5 baselineu. Browser ne šalje naziv,
trajanje, purpose, TeacherId, SortOrder ni Knowledge Component identifikatore i nije authority
za canonical activity graph.

Kopiranje plan KC mappinga rezultat je eksplicitne Teacher akcije dodavanja, a ne AI
inferencija. Ne nastaju `StudentAttempt`, `EvidenceEvent`, Mastery ili Readiness promjene.

## 5. Security i concurrency

- Teacher identity dolazi isključivo iz aktivne autentificirane sesije;
- owner/lifecycle scope primjenjuje se prije čitanja kandidata i prije writea;
- write zahtijeva CSRF i optimistic concurrency parenta;
- kandidat se ponovno dohvaća i validira unutar transakcije;
- nije moguće dodati Draft, retired, foreign-context ili homework-required blok;
- response ne sadrži provider secret, storage podatke ni student directory podatke.

## 6. Presentation

Ekran čuva canonical tri zone: filtere, listu kandidata i pregled odabira. Prikazuje stvarni
participant/goal/KC/time kontekst, Published verziju, trajanje, participant applicability,
assessment capability i upozorenje kada je exact blok već u planu. Dominantni CTA dodaje
samo trenutačno odabrani kandidat; povratak ne mijenja Draft.

## 7. Visual acceptance

Minimalni stvarni browser gate koristi canonical `5.4 odabir aktivnosti  knowledge
blocka.png` i dokazuje:

- desktop 1536×1024 i mobile 390×844;
- stvarni login, API, SQL i plan s postojećim ponovljenim blokom;
- stvarni candidate/detail state bez mock DOM-a ili API interceptiona;
- bez document horizontal overflowa, capture business writea i browser/page grešaka.

Evidence: `visual-acceptance/phase-9.7/`.

## 8. Namjerno odgođeno

- zaseban ActivityTemplate katalog i persistence;
- activity detail/edit, delete/reorder i regeneration — Phase 9.8;
- Draft → Ready validacija i potvrđeni plan — Phase 9.9;
- Board/LessonSession runtime, Attempt i Evidence — Phase 10+;
- AI/provider runtime, dodatna ili in-app naplata — nije dio Phase 9.7.
