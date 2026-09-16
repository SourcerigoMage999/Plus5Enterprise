# Phase 3.7 — Screen 2.9 Edit group

## Status i datum

**DONE — implementacija i interni gateovi završeni, 2026-09-11.** Spremno za završni SA
review; nije samostalno proglašeno SA-approved/LOCKED. Phase 4 nije započeta.

## Cilj i rezultat

Implementiran je owner-scoped edit iste `Group` instance, njezina aktualnog canonical
rasporeda i eksplicitnog membership workflowa. ADR-0016 blokira promjenu Programa dok
postoji aktivno članstvo, bez masovne izmjene Studenata ili automatskog zatvaranja
članstava. Capacity, status, reference, unique naziv i rowversion autoritativno provjerava
backend.

Promjena rasporeda versionira buduću seriju: stara pravila se supersedeaju, njihovi budući
Scheduled termini otkazuju, povijest ostaje, successor serije nose prethodnika i početnih
12 tjedana Sessiona nastaje atomski. Konflikt i DST greška odbijaju cijelu transakciju.

Frontend daje canonical četverostupčani desktop ekran, responsive mobile prikaz, live
sažetak, Program lock s objašnjenjem, članove/kandidate, single-flight save, potvrdu i
potpuni dirty-navigation gate. Neutralne buduće zone ne spremaju lažne podatke.

## Namjerno nije implementirano

Hard delete, archive workflow s posljedicama za raspored, location CRUD, conflict override,
arbitrary recurrence, Phase 4.6 replenishment, Knowledge/readiness, materials/goals/notes i
postavke privatnosti. Nema nove migracije, entiteta, dependencyja, commita ili pusha.

## Glavne datoteke

| Područje | Datoteke |
|---|---|
| Contract | `backend/src/Plus5.Application/Groups/GroupEditingContracts.cs` |
| Domain | `backend/src/Plus5.Domain/Groups/Group.cs` |
| Persistence | `EfGroupEditingQuery.cs`, `EfGroupEditingService.cs`, `GroupScheduleConflictQuery.cs`, DI |
| API | `GroupEditingEndpoints.cs`, `GroupEndpoints.cs` |
| Backend testovi | `GroupFoundationTests.cs`, `AuthenticationApiTests.cs`, `GroupEditingSqlTests.cs` |
| Frontend | `GroupEditPage.tsx`, `GroupEditPage.css`, route/list/API contracti |
| Frontend testovi | `GroupEdit.test.tsx`, `tests/visual/phase37.mjs` |
| Dokumentacija | `GROUP_EDITING.md`, ADR-0016, ROADMAP, OPEN_QUESTIONS, visual acceptance |

## Sigurnost i konzistentnost

Teacher identity dolazi isključivo iz session claims; missing/cross-owner/arhivirani zapis
je 404. PUT zahtijeva CSRF. Reference i lokacija su owner-scoped. Serializable transakcija,
SQL rowversion i kontrolirana 409 mapa štite race/deadlock/unique konflikt. Response DTO je
eksplicitan i ne izlaže EF entitete, Teacher ID ili osjetljive podatke.

SQL test pokriva ADR-0016 u oba smjera, capacity, stale rowversion, cross-owner referencu,
rollback kod vanjskog schedule konflikta, supersede, otkazivanje buduće instance,
successor vezu, 12 Sessiona i potpuno uklanjanje budućeg rasporeda. API/component testovi pokrivaju auth, CSRF, route, Program
lock, save, konflikt, članstvo uz dirty formu i navigation guard.

## Visual acceptance

[Zapis i četiri snimke](../visual-acceptance/phase-3.7/README.md) potvrđuju canonical
desktop 1536×1024 i mobile 390×844 bez horizontalnog overflowa, stvarni Program lock i
dirty-navigation modal. Dokumentirane su razlike za archive granicu, stvarne podatke,
neutralne buduće zone i native date/time locale.

## Testovi i završni rezultati

| Provjera | Rezultat |
|---|---|
| Release solution build | PASS — 0 warninga, 0 grešaka |
| Backend/API/domain/persistence sa stvarnim lokalnim SQL-om | PASS — 140/140, 0 skipped |
| Architecture dependency granice | PASS — 4/4 |
| Phase 3.7 SQL atomicity/conflict/versioning | PASS |
| `dotnet format --verify-no-changes --no-restore` | PASS |
| Frontend component testovi | PASS — 42/42 |
| Frontend lint, TypeScript i production build | PASS |
| Canonical desktop/mobile i dirty-navigation browser gate | PASS — 0 pageerrora, bez overflowa |
| Docker rebuild, migracije i runtime | PASS — API/frontend/database healthy |
| API live/ready i frontend HTTP | PASS — 200/200/200 |
| Non-root runtime | PASS — API UID 1654, frontend `nginx` |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Novi dependencyji / migracije | nema |

Završni rezultati su dobiveni nakon posljednjih izmjena. Raniji sandbox-denied pokušaji
audita nisu predstavljeni kao PASS; oba audita ponovljena su s potrebnim pristupom.

## Lokalni demo podatak

Na praznoj demo grupi spremljen je isključivo opis `Phase 3.7 funkcionalni pregled.` radi
provjere stvarnog PUT→list confirmation flowa. Članstva i raspored nisu mijenjani.
Odvojene SQL testne baze uklanjaju se u `finally`.

## Točna sljedeća točka

Završni SA review 3.7, zatim zasebno odobrenje commita/pusha. Nakon prihvaćanja sljedeća
je Phase 4.1 Calendar prema ROADMAP-u; ova predaja je ne pokreće automatski.
