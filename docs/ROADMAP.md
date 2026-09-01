# ROADMAP

## Kako čitati ovaj ROADMAP

ROADMAP je **izvršni redoslijed razvoja**, a ne samo popis featurea. Svaka podfaza završava zasebnim summaryjem. Statusi: `TODO`, `READY`, `BLOCKED`, `DONE`.

---

# PHASE 0 — Documentation & Architecture Lock

## 0.1 Normalize source documentation — READY
**Cilj:** pretvoriti postojeće specifikacije u AI-čitljiv source-of-truth paket i evidentirati rupe.

**Scope:** dokumentacijski indeks, screen status, open questions, pravila i summary format.

**Acceptance:** svi postojeći čitljivi DOCX-ovi imaju Markdown snapshot; prazni/oštećeni izvori su označeni; nijedan nedokumentirani ekran nije proglašen spremnim.

## 0.2 Product/domain glossary — DONE
**Cilj:** zaključati značenje ključnih pojmova: Teacher, Student, Guardian, Group, Program, Grade/Level, Session, Material, Activity, Knowledge Component, Evidence Event, Readiness itd.

**Acceptance:** jedan canonical naziv i definicija po pojmu; uklonjene kontradikcije među specifikacijama.

**Dovršeno 2026-08-23:** canonical nazivi, hrvatski/UI nazivi, definicije i zaključane terminološke razlike zapisani su u `DOMAIN_GLOSSARY.md`. Nedokumentirane kardinalnosti, permissions i algoritmi ostavljeni su svojim ROADMAP gateovima.

## 0.3 Technology architecture decision — DONE
**Preduvjet:** **ZADOVOLJEN 2026-08-23** — vlasnik projekta zaključao React + TypeScript frontend, C# / ASP.NET Core .NET backend, SQL Server persistence i Docker baseline.

**Deliverable:** ažuriran `ARCHITECTURE_BASELINE.md`, obavezni engineering standardi + ADR zapisi u `DECISION_LOG.md`.

**Mora zaključati:** backend, frontend, DB, API, testing, Docker/deployment baseline, configuration/secrets i security baseline. Detaljni business auth contract i file policy ostaju eksplicitni gateovi za njihove ROADMAP faze, bez blokiranja bootstrap arhitekture.

**Dovršeno 2026-08-23:** baseline je zaključan u `ARCHITECTURE_BASELINE.md`, obavezni standardi u zasebnim engineering dokumentima, a trajne odluke u ADR-0001–ADR-0005. U fazi 0.3 validirana je njihova međusobna usklađenost i lokalna dostupnost .NET 10, Node/npm i Docker toolchaina.

## 0.4 Repository/bootstrap — DONE
**Cilj:** stvoriti minimalan buildable/testable repo prema zaključanoj arhitekturi.

**Out of scope:** feature business logika.

**Acceptance:** clean checkout → build + test prolaze; osnovni README/dev setup postoji.

**Dovršeno 2026-08-23:** dodani su .NET 10 modularni backend, React + TypeScript + Vite frontend, architecture testovi, reproducibilni dependency/toolchain lockovi, non-root Docker imageovi, lokalni Compose i root development README. Release buildovi, testovi, health smoke testovi, dependency auditi i stvarni container build/runtime provjere prolaze.

---

# PHASE 1 — Cross-cutting Application Foundation

## 1.1 Configuration, environments & secrets — DONE
**Dovršeno 2026-08-23:** zaključan je environment/configuration contract za Development, Staging i Production; API koristi strongly typed startup validation za javni frontend origin, eksplicitni host allowlist i odbija nepodržane environmente, frontend ima centralni validirani public-config boundary bez secreta, a Development/Compose primjeri i user-secrets pravila dokumentirani su u `CONFIGURATION.md`. Backend/frontend testovi, buildovi, auditi i Docker runtime smoke test prolaze.

## 1.2 Database/persistence foundation — DONE
**Dovršeno 2026-08-24:** uvedeni su SQL Server 2025 i EF Core 10 persistence temelj, prazna početna migracija bez business sheme, odvojeni bootstrap/migration/runtime identiteti, non-root one-shot migration image, kontrolirani Compose dependency redoslijed i database-aware readiness. Release buildovi, 33 backend/frontend testa, format/lint/typecheck, clean i ponovljeni migration apply, migration history, least-privilege ovlasti, persistent-volume restart, health endpointi i non-root runtime provjere prolaze.
## 1.3 API conventions, validation & error contract — DONE
**Dovršeno 2026-08-24:** zaključani su `/api/v1` Minimal API route group, built-in .NET 10 server validation, sigurni RFC `ProblemDetails` odgovori sa stabilnim `code` i `traceId` poljima, globalni exception boundary bez curenja internih detalja te bounded pagination contract. Release build, 36 backend i 4 frontend testa, format/lint/typecheck, dependency auditi i izolirani Docker runtime contract test prolaze.
## 1.4 Logging/telemetry foundation — DONE
**Dovršeno 2026-08-24:** API zapisuje strukturirane JSON stdout logove, vraća i propagira W3C `X-Trace-Id`, bilježi sigurne route-template completion događaje te registrira OpenTelemetry ASP.NET Core traces/metrics i runtime metrike s opcionalnim validiranim OTLP exportom. Osjetljivi URL/query/user-agent tagovi uklanjaju se prije exporta, live health buka je potisnuta, a buildovi, 54 backend i 4 frontend testa, auditi i izolirani Docker runtime/log contract prolaze.
## 1.5 Frontend app shell, routing & design tokens — DONE
**Dovršeno 2026-08-24:** uvedeni su responsive učiteljski app shell, centralni registry svih 11 dokumentiranih glavnih ruta, aktivna SPA navigacija, eksplicitni 404, skip-link/landmark/focus accessibility temelj i centralni CSS design tokeni. Svi moduli ostaju jasno označeni neutralni placeholderi bez fake podataka, autha ili feature logike. Vitest + Testing Library component-test temelj, 9 frontend i 54 backend testa, lint/typecheck/build, dependency auditi, browser desktop/mobile review te non-root frontend Docker runtime prolaze.
## 1.6 Authentication & authorization — DONE
**Preduvjet:** **ZADOVOLJEN 2026-08-24** — business auth contract zaključan je u `AUTHENTICATION_REQUIREMENTS.md`, a tehnički baseline u `AUTHENTICATION_ARCHITECTURE.md`.

**Cilj:** implementirati samo Teacher account/authentication/authorization foundation bez uvođenja budućih korisničkih uloga ili modula.

**Scope:** javna Teacher registracija, potvrda e-maila, login e-mailom i lozinkom, sigurna revocable cookie sesija, logout, forgot/reset/change password, session invalidation, deny-by-default API authorization, Teacher ownership boundary, rate limiting i obavezna frontend auth stanja.

**Out of scope:** Student/Guardian/Admin accounti, admin-created ili invitation onboarding, JWT bearer auth u browser storageu, social login, MFA, Remember me i permissions budućih modula.

**Acceptance:** svi kriteriji iz `AUTHENTICATION_REQUIREMENTS.md` i test obligations iz `AUTHENTICATION_ARCHITECTURE.md` prolaze; protected API je zatvoren po defaultu; auth secrets se ne logiraju niti spremaju u browser storage; phase summary je obavezan.

**Dovršeno 2026-08-24:** implementiran je isključivo Teacher account lifecycle: javna registracija, obavezna potvrda e-maila, login/logout, forgot/reset/change password, revocable server-side cookie sesije, CSRF, rate limiting i deny-by-default authorization. Dodani su potpuni auth UI flowovi bez browser token storagea, SQL migracije za identity/session/token podatke i trajni shared Data Protection key ring koji izvan Developmenta zahtijeva zaštitu certifikatom. Release build, 86 backend i 13 frontend testova, format/lint/typecheck, dependency auditi, EF migration provjere, desktop/mobile browser review te čisti non-root Docker runtime prolaze.

---

# PHASE 2 — Core Teaching Domain

## 2.1 Program, grade/level and curriculum foundation — DONE
**Cilj:** definirati osnovne entitete koje kasnije koriste učenici, grupe, materijali i Knowledge Model.

**Scope:** Teacher-owned `Program` te odvojeni globalni `SchoolGrade`, `ProficiencyLevel` i verzionirani `Curriculum` referentni korijeni, domenske invariante, 3NF persistence i migracija.

**Out of scope:** Program CRUD/UI/lifecycle, seed/import katalozi, Student/Group/Material veze, CurriculumOutcome hijerarhija, Knowledge Model i readiness/evidence logika.

**Acceptance:** canonical razlike iz glossaryja ostaju očuvane; ownership, unique i check constrainti postoje u bazi; clean/idempotent migration i regression suite prolaze; contract i odgođene odluke su dokumentirani.

**Dovršeno 2026-08-25:** zaključan je `CORE_TEACHING_FOUNDATION.md` i ADR-0010. Dodani su odvojeni `Program`, `SchoolGrade`, `ProficiencyLevel` i `Curriculum` domain/persistence modeli bez seed pretpostavki ili preuranjenih veza. Migracija `AddCoreTeachingFoundation` na stvarnom SQL Serveru stvara četiri tablice, restriktivni Teacher ownership FK, četiri natural-key unique indeksa i dva sort-order CHECK constrainta. Release build, 92 backend i 13 frontend testova, format/lint/typecheck, dependency auditi, EF model/idempotent SQL te čisti non-root Docker runtime prolaze.

## 2.2 Student aggregate / profile foundation — DONE

**Cilj:** zaključati najmanji siguran Student/Guardian profil koji koriste ekrani 2.1, 2.2, 2.3 i 2.6 bez uvođenja feature API-ja ili knowledge/readiness podataka.

**Scope:** Teacher-owned Student bez accounta, obavezni SchoolGrade, opcionalni same-Teacher Program + eksplicitni DeliveryMode, statusi Active/OnHold/Inactive, osnovni opcionalni kontakt/profile podaci, Student-owned Guardian kontakti, arhiviranje, 3NF persistence i migracija.

**Out of scope:** Student CRUD/API/UI, Group/GroupMembership, fotografije/file storage, bilješke, communication, privacy toggles, ProficiencyLevel procjene/ciljevi, Knowledge Model, mastery/readiness/progress/evidence i production legal-erasure workflow.

**Acceptance:** Teacher/object ownership i cross-Teacher Program zaštita postoje u bazi; status/organization/archive/primary-Guardian invariante imaju DB constraint; clean, upgrade i idempotent migration prolaze; nema Student/Guardian accounta, seeda ni readiness polja; contract, ADR i odgođene odluke su dokumentirani.

**Dovršeno 2026-08-25:** zaključani su `STUDENT_FOUNDATION.md` i ADR-0011. Dodani su Student, DeliveryMode, StudentStatus i Student-owned Guardian modeli, restriktivni Teacher/SchoolGrade/Program/Student FK-ovi, composite same-Teacher Program zaštita, četiri Student CHECK constrainta i filtered unique primarni Guardian indeks. Migracija `AddStudentProfileFoundation` prolazi clean i upgrade putanju na SQL Serveru, očuvava postojeći Program i odbija cross-owner, incomplete organization, invalid status, active archive i duplicate-primary zapise. Release build, 99 backend i 13 frontend testova, format/lint/typecheck, dependency auditi, EF model/idempotent SQL te čisti non-root Docker runtime prolaze.

## 2.3 Group foundation — DONE

**Cilj:** zaključati najmanji siguran Group i GroupMembership temelj potreban za buduće ekrane 2.7, 2.8 i 2.9 bez rasporeda, termina ili feature API/UI sloja.

**Scope:** Teacher-owned Group s obaveznim same-Teacher Programom i SchoolGradeom, statusima Active/OnHold/Inactive, pozitivnim kapacitetom, arhiviranjem, optimistic concurrency zaštitom te vremenskim članstvom koje dopušta najviše jednu aktivnu grupu po Studentu.

**Out of scope:** Group CRUD/API/UI, raspored, lokacija, trajanje i Session, materijali, ciljevi, bilješke, progress/readiness/Knowledge Model, minimalan broj članova, fizičko brisanje i promjena Programa grupe s aktivnim članovima.

**Acceptance:** ownership i cross-Teacher zaštita postoje u bazi; status/capacity/archive/membership invariante imaju domain ili DB zaštitu; upgrade, clean i idempotent migration prolaze; concurrent capacity contract je eksplicitan; nema preuranjenih Session ili knowledge polja; contract, ADR i odgođene odluke su dokumentirani.

**Dovršeno 2026-08-25:** zaključani su `GROUP_FOUNDATION.md` i ADR-0012. Dodani su Group, GroupStatus i vremenski GroupMembership modeli, restriktivni composite ownership FK-ovi, filtered unique one-active-group indeks, status/capacity/archive/interval CHECK constrainti i `rowversion` za transakcijsku zaštitu kapaciteta. Student dobiva eksplicitne domain prijelaze za ulazak u grupni Program i izlazak u individualni način uz očuvanje Programa. Migracija `AddGroupFoundation` prolazi clean i upgrade putanju na SQL Serveru, očuvava postojeće Studente te odbija cross-owner, drugo aktivno članstvo, nevaljan kapacitet/status/archive/interval i duplicate-name zapise. Release build, 107 backend i 13 frontend testova, format/lint/typecheck, dependency auditi, EF model/idempotent SQL te čisti non-root Docker runtime prolaze.

## 2.4 Schedule/session foundation — DONE

**Cilj:** zaključati najmanji siguran model konkretnog termina i verzioniranog tjednog rasporeda potreban za buduće ekrane 3.1–3.4, bez feature API/UI sloja ili lesson-delivery evidencije.

**Scope:** Teacher-owned Location, konkretni UTC Session s individualnim ili grupnim kontekstom, eksplicitni statusi, tjedna RecurringSessionSeries za redoviti Group raspored i individualnu recurrence, occurrence identity, “samo ovaj termin” iznimka, “svi budući” versioning, optimistic concurrency i conflict transaction contract.

**Out of scope:** Schedule CRUD/API/UI, arbitrary RRULE, overnight, generation horizon/background replenishment, conflict override, shared-room permissions, reminders/notifications, attendance, stvarni held-lesson zapis, plan sata, materijali, domaća zadaća i Knowledge Model.

**Acceptance:** recurrence i series-change pravila iz 3.3/3.4 su formalizirana; ownership/context/time/status invariante imaju domain ili DB zaštitu; clean, upgrade i idempotent migration prolaze na SQL Serveru; “samo ovaj” i “svi budući” contracti su testirani; nema preuranjenog API/UI ili delivery scopea.

**Dovršeno 2026-08-28:** zaključani su `SCHEDULING_FOUNDATION.md` i ADR-0013. Dodani su Location, RecurringSessionSeries i Session modeli, restriktivni same-Teacher composite FK-ovi, CHECK constrainti, Teacher-first kalendarski indeksi, unique occurrence zaštita i `rowversion`. Upgrade 6→7 i clean SQL Server migracije prolaze uz očuvan sentinel i 0 seed redaka; devet negativnih constraint scenarija je odbijeno, a rowversion promjena potvrđena. Release build, 118 backend i 13 frontend testova, format/lint/typecheck, dependency auditi, EF model/idempotent SQL te čisti non-root Docker runtime i health provjere prolaze.

---

# PHASE 3 — Teacher UI: Students & Groups

## 3.1 Screen 2.1 Student list — DONE

**Dovršeno 2026-08-28:** implementirani su Teacher-authorized `GET /api/v1/students` i `/overview`, owner-scoped pretraga, filteri i bounded pagination, tablični/kartični responsive ekran, URL stanje, loading/empty/error/retry stanja te pregled statusa i programa. Zadnji sat koristi samo postojeći `Held` Session; napredak ostaje neutralno “Nije dostupno” do Knowledge/Evidence faze. Create/dossier/edit akcije su eksplicitno onemogućene do svojih podfaza. Release build, 116 API/domain/persistence i 4 architecture testa, 16 frontend testova, format/lint/typecheck/build, stvarna SQL Server translacija, Docker health i non-root runtime prolaze.

**Visual gate 2026-08-29:** PASS. Stvarni ekran uspoređen je s canonical PNG-om iz izvornog paketa `Za programera - novo.zip`. Desktop 1536×1024 i mobilna prilagodba 390×844 vizualno su pregledani; dokazi i namjerna odstupanja dokumentirani su u `docs/visual-acceptance/README.md`.
## 3.2 Screen 2.3 Create student — TODO
## 3.3 Screen 2.2 Student digital dossier (administrative/core view) — TODO
## 3.4 Screen 2.6 Edit student — TODO
## 3.5 Screen 2.7 Groups — TODO
## 3.6 Screen 2.8 Create group — TODO
## 3.7 Screen 2.9 Edit group — TODO

**Out of scope za ovu fazu:** izračun readinessa i detaljni Knowledge Model ako PHASE 5 još nije dovršena; UI mora koristiti neutralne placeholder/hidden states definirane prije implementacije, ne lažne postotke.

---

# PHASE 4 — Schedule

## 4.1 Screen 3.1 Calendar — TODO
## 4.2 Screen 3.2 Session detail — TODO
## 4.3 Screen 3.3 Create session — TODO
## 4.4 Screen 3.4 Edit session — TODO
## 4.5 Recurrence/series consistency tests — TODO

**Acceptance cijele faze:** promjena termina ne smije nekonzistentno mijenjati trajni raspored grupe; ponašanje “samo ovaj termin” i buduća serija mora biti eksplicitno testirano.

---

# PHASE 5 — Knowledge Model & Evidence Engine

> Ova faza je temelj za readiness, detalje znanja, inteligentne preporuke, materijale, zadatke, domaće zadaće i Ploču. Ne smije se svesti na UI postotke.

## 5.1 Curriculum hierarchy — TODO
## 5.2 Knowledge Component model — TODO
## 5.3 Evidence Event model — TODO
## 5.4 Evidence metadata: difficulty, help, evidence type — TODO
## 5.5 Readiness calculation rules — BLOCKED
**Gate:** matematička/poslovna pravila agregacije moraju biti eksplicitno definirana; specifikacija trenutno definira koncept, ali ne puni algoritam.

## 5.6 Screen 2.4 Readiness assessment — BLOCKED until 5.5
## 5.7 Screen 2.5 Knowledge detail overview — BLOCKED until 5.5
## 5.8 Grammar detail — BLOCKED until 5.5
## 5.9 Vocabulary detail — BLOCKED until 5.5
## 5.10 Reading detail — BLOCKED until 5.5
## 5.11 Listening detail — BLOCKED until 5.5
## 5.12 Speaking detail — BLOCKED until 5.5
## 5.13 Writing detail — BLOCKED until 5.5

---

# PHASE 6 — Materials Foundation

## 6.1 Material domain model & storage strategy — TODO
## 6.2 Material metadata ↔ curriculum/knowledge mapping — TODO
## 6.3 Screen 4.1 Material library — TODO
## 6.4 Screen 4.2 Material detail — TODO
## 6.5 Evidence-capable task metadata within material — TODO
## 6.6 Screen 4.4 Import own material — TODO
## 6.7 Screen 4.5 Edit material and version history — TODO
## 6.8 Material sharing, visibility and permissions contract — BLOCKED

**Source status:** detaljni source sada postoji za 4.1 i 4.4–4.5. Stari 0 B blocker za 4.1 više nije aktivan.

**Gate:** prije implementacije zaključati storage adapter, dopuštene formate i veličine, upload validation/scanning, ownership/sharing permissions, `MaterialVersion`/`TaskVersion` povijesnu konzistentnost te AI analysis confirmation/privacy boundary. AI metadata ostaje prijedlog dok ga Teacher ne potvrdi.

---

# PHASE 7 — PLUS 5 Presentation Editor

## 7.1 Technical design for slide document model — TODO
## 7.2 Slide CRUD/reorder/duplicate — TODO
## 7.3 Core content elements — TODO
## 7.4 Teaching-specific elements — TODO
## 7.5 Interactive question / Quick Check — TODO
## 7.6 Knowledge Component mapping per question — TODO
## 7.7 Autosave/version safety — TODO
## 7.8 Preview & save flow — TODO
## 7.9 AI assistance — BLOCKED
**Gate:** definirati AI provider, privacy, prompt/data boundary, confirmation behavior i failure fallback prije implementacije.

---

# PHASE 8 — Teacher Dashboard

## 8.1 Dashboard read model/API — TODO
## 8.2 Screen 1.1 Teacher dashboard — TODO

**Preduvjeti:** studenti, raspored i materijali moraju postojati. Sekcije koje ovise o budućim modulima (poruke, financije, lesson plan) implementirati tek kad njihovi izvori postoje; bez fake produkcijskih podataka.

---

# PHASE 9 — Lesson Builder

## 9.0 Detailed specification 5.1–5.6 — DOCUMENTED
Source: `source_specs/5.0_Priprema_sata_Lesson_Builder.md` i 5.1–5.6 screen specovi.

## 9.1 Goal selection — TODO
## 9.2 Suggested lesson structure and balance check — TODO
## 9.3 Lesson Plan editor and material-version attachment — TODO
## 9.4 Activity / Knowledge Block selection — TODO
## 9.5 Lesson Activity detail/edit — TODO
## 9.6 Confirmed Lesson Plan / ready state — TODO

**Gate:** prije implementacije zaključati formalne `LessonPlan`, `ActivityTemplate` i `LessonActivity` contracte te dependencyje na Knowledge/Material modele. Planiranje ne stvara `Attempt`, `EvidenceEvent` niti mijenja Knowledge Model.

---

# PHASE 10 — PLUS 5 Board / Live Lesson

## 10.0 Detailed specification 6.1–6.5 — DOCUMENTED
Source: `source_specs/6.0_PLUS5_Ploca.md` i 6.1–6.5 runtime specovi.

## 10.1 Teacher board and Lesson Session lifecycle — TODO
## 10.2 Universal activity runtime and Attempt capture — TODO
## 10.3 Live student diagnostic — TODO
## 10.4 Live lesson summary and plan-vs-executed view — TODO
## 10.5 Lesson closure, Evidence finalization and follow-up — TODO

**Gate:** formalizirati `LessonSession` persistence, runtime recovery/autosave, `TaskVersion`/`MaterialVersion` references, teacher-assessed Evidence i invalidation/audit pravila. `Completion`, `Accuracy` i `Mastery` ostaju odvojeni koncepti; `Attempt` nije automatski Evidence.

---

# PHASE 11 — Session History

## 11.0 Detailed specification 7.1–7.2 — DOCUMENTED
## 11.1 Completed Lesson Session history list — TODO
## 11.2 Completed lesson historical detail — TODO

**Gate:** povijesni read model mora čuvati stvarno korištene Task/Material verzije, planirano naspram izvedenog, void/invalidation audit i isti Session context prema dossieru, homeworku, reportsima i financijama.

---

# PHASE 12 — Homework

## 12.0 Detailed specification 8.1–8.3 — DOCUMENTED
## 12.1 Homework overview and operational queues — TODO
## 12.2 Create/duplicate Homework Assignment — TODO
## 12.3 Homework detail, Submission review and lifecycle — TODO

**Gate:** formalizirati `HomeworkAssignment`/`HomeworkSubmission`, participant access, Task versioning, reminder confirmation i Evidence emission/assistance pravila. Closing ili cancelling ne briše povijesne Submissione, Attemptse ili Evidence.

---

# PHASE 13 — Messaging

## 13.0 Detailed specification 9.1–9.2 — DOCUMENTED
## 13.1 Inbox, sent, archived and draft conversations — TODO
## 13.2 Conversation detail, composer, attachments and Context Links — TODO

**Gate:** zaključati participants/permissions, private replies na broadcast, attachments, retention, delivery, read state i abuse/privacy contract. AI smije predložiti draft, ali ne smije automatski poslati poruku.

---

# PHASE 14 — Reports

## 14.0 Detailed specification 10.1–10.9 — DOCUMENTED
## 14.1 Reports overview and Student/Group selection — TODO
## 14.2 Student report overview with Period Context — TODO
## 14.3 Knowledge analysis — TODO
## 14.4 Activity timeline analysis — TODO
## 14.5 Task, Attempt and result analysis — TODO
## 14.6 Completed-session analysis — TODO
## 14.7 Engagement and behavior analysis — TODO
## 14.8 Parent-report draft, section selection and export preview — TODO
## 14.9 Parent-report review, immutable Report Snapshot and messaging handoff — TODO

**Gate:** zaključati izvore podataka, metric definitions, confidence/insufficient-data behavior, privacy, export/PDF i immutable `ReportSnapshot` contract. Knowledge, Engagement i Assessment Readiness ne smiju se svesti na jednu metriku.

---

# PHASE 15 — Finance

## 15.0 Detailed specification 11.1–11.3 — DOCUMENTED
## 15.1 Finance overview, trends and opportunity suggestions — TODO
## 15.2 Completed sessions, financial entries and payment status — TODO
## 15.3 Financial detail, adjustment, void and audit trail — TODO

**Gate:** formalno zaključati internal-ledger naspram invoice/payment/tax/fiscalization scopea, valutu i money precision, group per-Student payment semantics te permissions/audit. Održani sat može stvoriti stavku, ali se financijska povijest ne briše tiho; poslovne prijedloge potvrđuje Teacher.

---

# PHASE 16 — Settings, Notifications & Profile

## 16.0 Detailed specification 12.x–14.x — DOCUMENTED
## 16.1 General / Teacher settings — TODO
## 16.2 Teaching and Group preferences — TODO
## 16.3 Assessment, attendance and homework rules — TODO
## 16.4 Pricing and business rules — TODO
## 16.5 Notification preferences and channel policy — TODO
## 16.6 Privacy, security and data controls — TODO
## 16.7 Settings single-source-of-truth audit and MVP boundary — BLOCKED
## 16.8 Notification center and event/read-resolved model — TODO
## 16.9 User menu and Teacher profile — TODO
## 16.10 Account/security UI — TODO

**Gate:** završiti audit postavki 12.1–12.7 tako da svaka odluka ima jedan source of truth; zaključati notification event/delivery/retention contract. `Message` nije `Notification`, a read notification ne rješava povezani poslovni događaj. Auth funkcije ostaju podređene zaključanim Phase 1.6 contractima; budući MFA/2FA ili novi account tipovi zahtijevaju zasebnu odluku.

---

# PHASE 17 — Hardening & Release

## 17.1 End-to-end critical journeys — TODO
## 17.2 Security review — TODO
## 17.3 Performance review — TODO
## 17.4 Accessibility review — TODO
## 17.5 Backup/restore and operational runbook — TODO
## 17.6 Production deployment validation — TODO
## 17.7 Release summary / known limitations — TODO

---

# Obavezni completion zapis po podfazi

Za svaku dovršenu stavku kreira se:

`summaries/PHASE_<id>_<slug>_SUMMARY.md`

po predlošku `PHASE_SUMMARY_TEMPLATE.md`.
