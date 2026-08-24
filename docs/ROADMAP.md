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
## 1.6 Authentication & authorization — BLOCKED
**Gate:** posebni auth/business zahtjevi još nisu detaljno definirani u dostavljenoj dokumentaciji.

---

# PHASE 2 — Core Teaching Domain

## 2.1 Program, grade/level and curriculum foundation — TODO
Definirati osnovne entitete koje kasnije koriste učenici, grupe, materijali i Knowledge Model.

## 2.2 Student aggregate / profile foundation — TODO
Podaci potrebni za 2.1, 2.2, 2.3 i 2.6 bez readiness logike.

## 2.3 Group foundation — TODO
Podaci i pravila potrebni za 2.7, 2.8 i 2.9.

## 2.4 Schedule/session foundation — TODO
Model konkretnog termina, statusa termina, individualnog/grupnog konteksta i veze s redovitim rasporedom grupe.

**Gate:** pravila ponavljajućih termina i promjene serije moraju se formalizirati iz 3.3/3.4 specifikacije prije DB locka.

---

# PHASE 3 — Teacher UI: Students & Groups

## 3.1 Screen 2.1 Student list — TODO
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
## 6.3 Screen 4.1 Material library — BLOCKED
**Razlog:** DOCX izvor je prazan (0 B). PNG postoji, ali bez detaljnog business opisa ne zaključavati ponašanje filtera, akcija i permissionsa.

## 6.4 Screen 4.2 Material detail — TODO
## 6.5 Evidence-capable task metadata within material — TODO

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

# PHASE 9 — Lesson Builder — DOCUMENTATION GATE

## 9.0 Detailed specification 5.1–5.7 — BLOCKED
Tek nakon odobrene detaljne specifikacije:
- 9.1 Goal selection
- 9.2 Suggested lesson structure
- 9.3 Lesson plan editor
- 9.4 Activity/Knowledge Block selection
- 9.5 Activity detail/edit
- 9.6 Materials attachment
- 9.7 Confirmed lesson

---

# PHASE 10 — PLUS 5 Board / Live Lesson — DOCUMENTATION GATE

## 10.0 Detailed specification 6.1–6.5 — BLOCKED
Nakon toga: teacher board, activity delivery, student work review, live summary, lesson completion and evidence emission.

---

# PHASE 11 — Session History — DOCUMENTATION GATE

## 11.0 Detailed specification 7.1–7.2 — BLOCKED

---

# PHASE 12 — Homework — DOCUMENTATION GATE

## 12.0 Detailed specification 8.1–8.3 — BLOCKED
Posebno zaključati kako homework rezultat emitira Evidence Event i kako se evidentira pomoć učeniku.

---

# PHASE 13 — Messaging — DOCUMENTATION GATE

## 13.0 Detailed specification 9.1–9.2 — BLOCKED
Zaključati korisnike razgovora, permissions, unread/read state, attachments, retention i notifications.

---

# PHASE 14 — Reports — DOCUMENTATION GATE

## 14.0 Detailed specification 10.1–10.9 — BLOCKED
Zaključati izvore podataka, metrike, privatnost i PDF/export contract.

---

# PHASE 15 — Finance — DOCUMENTATION GATE

## 15.0 Detailed specification 11.1–11.3 — BLOCKED
Zaključati je li modul samo evidencija ili uključuje račune/naplatu/porezne podatke.

---

# PHASE 16 — Settings, Notifications & Profile — DOCUMENTATION GATE

## 16.0 Detailed specification 12.x–14.x — BLOCKED
Uključuje postavke, notification center, profile menu i preostale auth ekrane.

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
