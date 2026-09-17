# Phase 5.2 — Knowledge Component model

## Status

`FINAL LOCK — odobreno 2026-09-18`

## Datum

`2026-09-16`; M-01 SQL runtime gate `2026-09-18`

## Cilj faze

Uvesti globalni, verzionirani PLUS 5 Knowledge Model s odvojenim područjima, kontroliranim
single-parent stablom komponenti i eksplicitnim CurriculumOutcome M:N mappingom, bez
produkcijskog kataloga ili preuranjenog Evidence/Mastery/Readiness scopea.

## Implementirano

- `KnowledgeModel` kao globalna versioning granica s jednosmjernim
  `Draft → Published → Retired` lifecycleom
- odvojeni `KnowledgeArea` organizacijski entitet
- `KnowledgeComponent` koji pripada točno jednom modelu i Area
- nullable single-parent adjacency tree bez hardkodirane dubine
- `Active` i `Deprecated` component status
- optional `SupersedesKnowledgeComponentId` samo između različitih verzija iste
  `KnowledgeModel.Code` obitelji
- domain zaštita Draft uređivanja i Published/Retired immutabilityja
- composite FK-ovi za same-model/same-area parent i SQL trigger za arbitrary cycle
- SQL lifecycle triggeri koji štite objavljenu strukturu i sprječavaju hard-delete povijesti
- leaf query foundation bez spremanja izvedenog eligibility polja
- čisti M:N `CurriculumOutcomeKnowledgeComponent` s composite PK-om i restriktivnim FK-ovima
- SQL lifecycle guard koji dopušta mapping INSERT/UPDATE/DELETE samo za Draft KnowledgeModel
- migracija, model snapshot te domain/EF/stvarni SQL testovi
- zaključani contract `KNOWLEDGE_COMPONENT_MODEL.md` i ADR-0019

## Namjerno nije implementirano

- produkcijski KnowledgeModel, Area ili Component katalog, seed, backfill ili import
- `EvidenceEvent`, Student evidence i correction lifecycle
- mastery, readiness, confidence, weights, decay, thresholds ili postotci
- spremljeni `EvidenceEligible`; budući direct evidence leaf pravilo ostaje za Evidence fazu
- AI inference ili automatski Curriculum mapping
- Teacher custom model, Tag mapping, task/material mapping ili KnowledgeBlock model
- javni API, CRUD endpoint ili frontend ekran

## Promijenjene / dodane datoteke

| Područje | Datoteke / promjena |
|---|---|
| Domain | `KnowledgeModel`, `KnowledgeArea`, `KnowledgeComponent`, lifecycle/statusi i Curriculum mapping |
| Persistence | četiri `DbSet`a, EF konfiguracije, composite ključevi/FK-ovi, indeksi i trigger metadata |
| Migracije | `AddKnowledgeComponentModel`, aditivni `AddKnowledgeMappingLifecycleGuard`, model snapshot i četiri SQL integrity triggera |
| Testovi | domain/EF lifecycle/tree testovi i stvarni SQL migration/integrity/query gateovi |
| Dokumentacija | Knowledge contract, ADR-0019, foundation/curriculum/persistence, pitanja, backlog, ROADMAP i manifest |

## Domain / database promjene

- nove tablice `KnowledgeModels`, `KnowledgeAreas`, `KnowledgeComponents` i
  `CurriculumOutcomeKnowledgeComponents`
- `(KnowledgeModel.Code, Version)` je unique business key
- Area i Component imaju nenegativni sort; statusi imaju CHECK constrainte
- composite alternate/FK ključevi sprječavaju cross-model i cross-area parent
- M:N composite PK sprječava duplicate mapping
- svi poslovno važni delete behaviori su `Restrict`
- triggeri odbijaju arbitrary cycle, same-version/cross-family lineage, izmjenu objavljene
  strukture ili mappinga, nevaljani lifecycle i hard-delete Published/Retired modela ili
  komponenti
- leaf je komponenta bez child retka i dobiva se queryjem
- početna migracija `20260916205538_AddKnowledgeComponentModel` i aditivna
  `20260917224652_AddKnowledgeMappingLifecycleGuard`; nema backfilla ni produkcijskih podataka
- EF potvrđuje da nema pending model promjena

## API / frontend promjene

Nema novog javnog endpointa, request/response contracta, routea ni UI-ja. Phase 5.2 je namjerno
model-only foundation.

## Security / authorization

- model je globalni kontrolirani referentni podatak i nema `TeacherAccountId`
- nema javnog write surfacea niti promjene postojećih authorization pravila
- nema PII-ja, seeda, pristupnih podataka ni tajni u modelu, migraciji ili fixtureima
- DB integritet ostaje aktivan i pri izravnom SQL upisu koji zaobiđe aplikaciju

## Testovi i evidence

| Naredba / suite | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Phase 5.2 domain/model/stvarni SQL | PASS — 13/13, 0 skipped |
| M-01 mapping lifecycle regression | PASS — cijeli migration chain na disposable SQL bazi; Draft add/remove te Published add/update/remove i Retired mutation potvrđeni, SQL 51107 gdje je očekivan |
| Aktualni host Backend/API/domain/persistence suite | PASS — 152/178; 26 opt-in SQL testova očekivano skipped bez lokalnog SQL-a |
| Raniji puni Backend/API/domain/persistence suite sa stvarnim SQL-om | PASS — 177/177, 0 skipped prije M-01 korekcije |
| Architecture dependency granice | PASS — 4/4 |
| `dotnet format --verify-no-changes --no-restore` | PASS — 0 promijenjenih datoteka |
| EF pending-model check | PASS — nema promjena nakon migracije |
| Idempotentni migration script | PASS — sadrži četiri tablice i četiri integrity triggera |
| Frontend component testovi | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker API/migrations/frontend build | PASS |
| Compose migration i runtime health | PASS — migration/init exit 0; API, baza i frontend healthy |
| HTTP smoke | PASS — `/health/live`, `/health/ready` i frontend vraćaju 200 |
| Non-root runtime | PASS — API UID 1654; frontend UID 101 `nginx` |
| Raniji runtime schema evidence | PASS — četiri tablice i tri početna triggera postoje; svi catalog/mapping countovi su 0 |
| M-01 migration/runtime evidence | PASS — aditivna migracija i četvrti mapping trigger primijenjeni su na disposable stvarnoj SQL bazi; ponašanje 51107 potvrđeno |

Windows Application Control na hostu blokira izvršavanje svježe generiranih test DLL-ova s
`0x800711C7`. Zato su Phase 5.2 i puni regression suite izvršeni u lokalnom pinned .NET SDK
Docker imageu, sa sourceom montiranim read-only, protiv stvarnog lokalnog SQL Servera. Host
Release build i format provjera normalno prolaze.

## Self-review

- [x] scope ostaje unutar zaključanog model-only 5.2 contracta
- [x] KnowledgeArea i KnowledgeComponent ostaju odvojeni
- [x] nema DAG-a, cross-model/cross-area parenta, ciklusa ili hardkodirane dubine
- [x] Published/Retired struktura i povijest zaštićene su u domeni i SQL-u
- [x] lineage je optional, same-family i cross-version
- [x] M:N nema algoritamska weighting/readiness polja i DB sprječava duplikat
- [x] Published/Retired KnowledgeModel zaključava CurriculumOutcome mapping na SQL razini
- [x] leaf je izvedena strukturna činjenica; Evidence nije preuranjeno uveden
- [x] nema izmišljenog produkcijskog kataloga ili Teacher custom modela
- [x] migracija prolazi upgrade/idempotency gate i nema pending model promjena
- [x] puni backend/frontend/architecture regression ostaje zelen
- [x] security auditi, Docker health i non-root runtime prolaze
- [x] dokumentacija i audit trail su usklađeni

## Arhitekturne odluke

Implementacija izvršava ADR-0019 i `KNOWLEDGE_COMPONENT_MODEL.md`. Knowledge Model verzija je
immutability granica, cross-cutting odnosi koriste mappinge umjesto DAG parenta, a Curriculum
alignment je normalizirani M:N prema konkretnim verzioniranim retcima.

## Poznati rizici / tehnički dug

- Windows Application Control ograničenje hosta ostaje okolišni problem; reproducibilni testni
  container daje završni evidence bez promjene produkcijskog koda.
- Production catalog/import source nije odabran; model namjerno ostaje prazan.
- Evidence correction i agregacijska matematika ostaju zasebni blocking gateovi kasnijih faza.

## Otvorena pitanja

Nema otvorenog business ponašanja unutar Phase 5.2. `KnowledgeBlock` i `EvidenceEvent`
emission/correction contract ostaju otvoreni u `OPEN_QUESTIONS.md` za svoje faze.

## Točna početna točka za sljedeću fazu

Phase 5.2 dobila je FINAL LOCK nakon prolaska M-01 SQL runtime gatea. Phase 5.3 može početi tek
nakon zasebno zaključanog Evidence Event lifecycle/correction contracta.
