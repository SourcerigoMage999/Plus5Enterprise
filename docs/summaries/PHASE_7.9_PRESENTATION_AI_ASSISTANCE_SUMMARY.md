# Phase 7.9 — Presentation AI assistance

## Status

**FINAL LOCKED — odobreno 2026-10-25.**

## Implementirano

- provider-neutralni application port i OpenAI Responses infrastructure adapter;
- canonical `gpt-6.1-sol`, EU endpoint, `store=false`, no-tools i strict JSON-schema output;
- default-disabled serverska konfiguracija s nula provider poziva/troška dok owner izričito
  ne uključi integraciju vlastitim API ključem;
- suggestion flow za presentation draft, tekstualni element, Quick Check i bounded Knowledge
  mapping;
- 24-satni Pending lifecycle, Accept/Edit/Reject/Expired i minimalni provenance;
- Teacher ownership, Draft/version target, document concurrency, CSRF, 8 KiB body i dedicated
  20/10-min Teacher rate limit;
- server-side typed canonical write tek nakon eksplicitne Teacher potvrde;
- responsive AI panel s jasnim `AI prijedlog · nije spremljeno` stanjem i uvijek dostupnim
  ručnim editorom;
- nova migracija `20261024122849_AddPresentationAiAssistance` i SQL integrity constrainti.

## Sačuvane odluke

- Student/Guardian/Attempt/Evidence/mastery/readiness i privatne bilješke nikad ne ulaze u AI
  context;
- binary asseti, credentials, signed URL/storage key i scan metadata ne šalju se provideru;
- prompt je server-owned, Teacher/Material tekst nepouzdani je data context;
- provider output nije authority, ne piše izravno u EF i ne može publishati;
- Quick Check i mapping ponovno koriste postojeće Phase 7.5/7.6 validatore;
- nema model fallbacka, provider conversation/file persistence ni automatske regeneracije;
- PLUS 5 ne naplaćuje AI u aplikaciji i ne uključuje ga automatski.

## Validacija

- backend Release build: PASS — 0 warninga i 0 grešaka;
- targeted Phase 7.9 domain/provider/API: PASS — 9/9;
- stvarni SQL migration/lifecycle: PASS — 1/1;
- EF pending model changes: PASS — nema model drifta;
- dependency-boundary review: PASS za Phase 7.9 provider-neutral port; global architecture
  suite je 3/4 zbog pre-existing Phase 7.5 `EvidenceType` DTO reference, zabilježene zasebno u
  `OPEN_QUESTIONS.md` bez scope creepa;
- targeted frontend editor/AI: PASS — 11/11;
- frontend lint, TypeScript i production build: PASS;
- targeted .NET format: PASS;
- visual acceptance: PASS — jedan desktop 1536×1024 i jedan mobile 390×844 screenshot,
  stvarni login/API i default-disabled no-cost state, bez document overflowa i browser/page
  grešaka;
- live paid OpenAI request: N/A — acceptance ne uključuje vanjski trošak;
- full regression: N/A — targeted Phase 7.9 validation je dovoljna;
- dependency audit: N/A — dependency graph/lockfile nisu promijenjeni;
- Compose config: PASS; Docker infrastructure re-certification: N/A — Dockerfile, startup i
  runtime identity nisu promijenjeni. Full image build zaustavio je postojeći transitive .NET
  crypto advisory koji nije uveden ovom fazom.

## Migration/API napomene

- prethodno zaključana Phase 7.5 migracija vraćena je bit-for-bit iz Git-a nakon što je stale
  `dotnet ef migrations remove --no-build` učitao staru kompilaciju;
- Phase 7.9 migracija zatim je ponovno generirana iz svježe kompiliranog modela;
- API dodaje `/presentation/ai/status`, create/get/resolve suggestion endpointove;
- provider key ostaje samo u server environment/secret storeu.

## Security i self-review

- no-charge default je enforced konfiguracijom i testom koji dokazuje nula provider requestova;
- ownership je i query i composite-FK granica;
- Pending content nestaje pri svakom terminalnom stanju;
- mapping output i Teacher-edited mapping ponovno se ograničavaju na bounded eligible candidate
  skup;
- prompt/response/free text/key ne ulaze u normalne logove;
- postojeći transitive .NET crypto advisory nije uveden ovom fazom i ostaje zaseban maintenance
  finding u `OPEN_QUESTIONS.md`.

## Scope namjerno nije implementiran

- Student personalization, grading, Evidence, mastery/readiness;
- OCR/document extraction, image/audio/video AI i asset generation;
- web search, tools, agents i provider-side persistence;
- automatic publish ili `generate everything and save`;
- in-app AI billing, credits ili subscription;
- Lesson Builder/model-improvement contract.

## Review handoff

Phase 7.9 je Solution Architect odobrio i označena je kao FINAL LOCKED.
