# Phase 5.1 — Curriculum hierarchy

## Status

`DONE — FINAL LOCK`

## Datum

`2026-09-16`

## Cilj faze

Uvesti čvrst model kurikularnih ishoda unutar konkretne `Curriculum` verzije, s proizvoljno
dubokom adjacency hijerarhijom, službenim identifier/provenance granicama i opcionalnim
cross-version lineageom, bez izmišljanja produkcijskog kataloga ili preuranjenog Knowledge
Modela.

## Implementirano

- globalni `CurriculumOutcome` entitet koji nije Teacher-owned
- nullable self-reference `ParentOutcomeId`; `null` označava root, a child ima najviše jednog
  parenta
- obavezni interni GUID odvojen od nullable službenog `OfficialCode`
- obavezni `SourceAuthority` i `SourceReference` kada službeni kod postoji
- eksplicitni nenegativni `SortOrder` i stabilni sekundarni redoslijed po `Id`
- opcionalni `SupersedesOutcomeId` samo prema outcomeu druge verzije iste `Curriculum.Code`
  obitelji
- domenske zaštite za identitet, sadržaj, parent scope, provenance i lineage
- composite same-Curriculum parent FK, restriktivni delete behavior i filtrirani unique code
  indeks
- SQL trigger koji pri izravnom ili višerednom upisu odbija arbitrary cycle, same-version i
  cross-family supersession
- indeks za determinističan root/children traversal i indeks za lineage lookup
- EF migracija, model snapshot te stvarni SQL migration, integrity i query testovi
- zaključani contract `CURRICULUM_HIERARCHY.md` i ADR-0018

## Namjerno nije implementirano

- produkcijski curriculum/outcome katalog, seed ili backfill
- scraping, parser, ministry import, staging, approval ili masovni ručni unos
- Teacher custom outcome authoring ili javni CRUD/API
- `KnowledgeComponent`, outcome-to-knowledge mapping, mastery, readiness ili evidence
- frontend ekran ili promjena postojećeg korisničkog toka
- hardkodirani outcome tipovi, pedagoška dubina ili generirani službeni kodovi

## Promijenjene / dodane datoteke

| Područje | Datoteke / promjena |
|---|---|
| Domain | novi `CurriculumOutcome` entitet i zaključane invarijante |
| Persistence | `DbSet`, EF konfiguracija, traversal/lineage indeksi i relational constraints |
| Migracije | `AddCurriculumOutcomeHierarchy`, model snapshot i SQL hierarchy trigger |
| Testovi | domain/model testovi i stvarni SQL hierarchy/migration/integrity gateovi |
| Dokumentacija | curriculum contract, ADR-0018, foundation/persistence/open questions, ROADMAP i manifest |

## Domain / database promjene

- nova tablica `CurriculumOutcomes`; nema promjene ili backfilla postojećih podataka
- PK je interni `Id`; `(CurriculumId, Id)` je alternate key za same-version parent FK
- `(CurriculumId, OfficialCode)` jedinstven je samo kada `OfficialCode` postoji
- Curriculum, parent i predecessor veze koriste `Restrict`
- CHECK constrainti štite sort, self-parent, self-supersession i minimalni code provenance
- trigger štiti proizvoljne cikluse i zahtijeva isti `Curriculum.Code`, ali različit
  `Curriculum.Version`, čak i pri izravnom SQL upisu
- migracija `20260916193850_AddCurriculumOutcomeHierarchy`; EF potvrđuje da nema pending model
  promjena

## API / frontend promjene

Nema novog javnog endpointa, request/response contracta, routea ni UI-ja. Phase 5.1 je namjerno
model-only foundation.

## Security / authorization

- outcome je globalni referentni podatak i nema `TeacherAccountId`
- nema javnog write surfacea ni promjene postojećih authorization pravila
- authoritative code ne može postojati bez zapisa authorityja i reference izvora
- nema seedanih poslovnih podataka, pristupnih podataka ni tajni u migraciji ili testnim
  fixtureima

## Testovi i evidence

| Naredba / suite | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Phase 5.1 domain/model/stvarni SQL | PASS — 10/10 |
| Puni Backend/API/domain/persistence suite sa stvarnim SQL-om | PASS — 165/165, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| `dotnet format --verify-no-changes --no-restore` | PASS — 0 promijenjenih datoteka |
| EF pending-model check | PASS — nema promjena nakon migracije |
| Idempotentni migration script | PASS — sadrži tablicu i hierarchy trigger |
| Frontend component testovi | PASS — 59/59 |
| Frontend lint, TypeScript i production build | PASS |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Docker API/migrations/frontend build | PASS |
| Compose migration i runtime health | PASS — migration/init exit 0; API, baza i frontend healthy |
| HTTP smoke | PASS — `/health/live`, `/health/ready` i frontend vraćaju 200 |
| Non-root runtime | PASS — API UID 1654; frontend UID 101 `nginx` |
| Runtime schema evidence | PASS — tablica i trigger postoje; production outcome count je 0 |

Windows Application Control na hostu blokira izvršavanje svježe generiranih test DLL-ova s
`0x800711C7`. Zato su Phase 5.1 i puni regression suite izvršeni u lokalnom pinned .NET SDK
Docker imageu, sa sourceom montiranim read-only, protiv stvarnog lokalnog SQL Servera. Host
Release build i format provjera normalno prolaze.

## Self-review

- [x] scope ostaje unutar zaključanog model-only 5.1 contracta
- [x] nema izmišljenog kataloga, službenih kodova ili authoritative izvora
- [x] nema hardkodirane dubine ili outcome tipova
- [x] domain i stvarni SQL štite ownership, parent, cycle, provenance i lineage pravila
- [x] migracija prolazi upgrade/idempotency gate i nema pending model promjena
- [x] puni backend/frontend/architecture regression ostaje zelen
- [x] security auditi, Docker health i non-root runtime prolaze
- [x] dokumentacija i audit trail su usklađeni

## Arhitekturne odluke

Implementacija izvršava ADR-0018 i zaključani `CURRICULUM_HIERARCHY.md`. Curriculum verzija je
immutability granica, `CurriculumOutcome` je odvojen od budućeg `KnowledgeComponent` modela, a
buduća veza može nastati samo kao eksplicitni M:N mapping u zasebno odobrenoj fazi.

## Poznati rizici / tehnički dug

- Windows Application Control ograničenje hosta ostaje okolišni problem; reproducibilni testni
  container daje završni evidence bez promjene produkcijskog koda.
- Authoritative katalog, import i correction workflow namjerno nisu odabrani; to nije skriveni
  dio 5.1.

## Otvorena pitanja

Nema otvorenog business ponašanja unutar Phase 5.1. Knowledge Component contract mora se
zasebno zaključati prije Phase 5.2; ova faza ne pretpostavlja njegov model ili semantiku.

## Točna početna točka za sljedeću fazu

Phase 5.1 dobila je završni SA acceptance 2026-09-16. Phase 5.2 može se otvoriti kao zasebna
podfaza isključivo prema zaključanom Knowledge Component contractu iz projektne dokumentacije.
