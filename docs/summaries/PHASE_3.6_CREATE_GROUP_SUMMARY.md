# Phase 3.6 — Screen 2.8 Create group

## Status i datum
**DONE — implementacija i interni gateovi završeni, 2026-09-11.**
Spremno za završni SA review; nije samostalno proglašeno SA-approved/LOCKED.
Pripremni dio ranije je izričito APPROVED. Phase 3.7 nije započeta.

## Cilj i implementirano
Dovršiti postojeći obrazac prema sourceu 2.8 i zaključanim SA odlukama ADR-0015.
Grupa može nastati Active s 0 učenika i bez rasporeda. Program/razred i opcionalna lokacija
dolaze iz postojećih kataloga. Početna članstva, canonical serije i bounded početni
Sessioni spremaju se u jednoj Serializable transakciji.

EndsOn je opcionalan; početni horizont 12 tjedana nije limit trajanja serije.
Server provjerava ownership, capacity, unique naziv, aktivno članstvo, Student rowversion,
DST i Teacher/Location konflikt. Provjeravaju se i još nematerializirana pravila unutar
početnog prozora uz poštovanje postojećih occurrence exceptions. Nema conflict overridea.

UI ima odabir kroz stranice, sažetak, raspored, retry/error/empty/loading/pending,
single-flight submit i potvrdu uspjeha s odabranom novom grupom. Native dialog/useBlocker
štiti interne poveznice i Back/Forward; Escape ostavlja obrazac. Beforeunload pokriva
refresh/zatvaranje. Uspješan save i obavezni auth redirect ne ostaju blokirani.

## Namjerno nije implementirano
Replenishment/background worker (Phase 4), MinimumStudents, automatska deaktivacija
(rejected), Group edit (3.7), notes/goals/materials/Knowledge/file uploads/sharing,
lokacijski CRUD, konflikt override ili novi draft model. Nema limita 12 mjeseci.
Nema novih dependencyja, produkcijskog deploymenta, commita ili pusha.

## Promijenjene / dodane datoteke
| Datoteke | Vrsta / razlog |
|---|---|
| backend/src/Plus5.Application/Groups/GroupCreationContracts.cs | novi read/write contract i bounded generator |
| backend/src/Plus5.Infrastructure/Groups/EfGroupCreationQuery.cs | kandidati i lokacije |
| backend/src/Plus5.Infrastructure/Groups/EfGroupCreationService.cs | atomski create |
| backend/src/Plus5.Infrastructure/Groups/GroupScheduleConflictQuery.cs | nematerializirane serije i exceptions |
| backend/src/Plus5.Api/Groups/GroupCreationEndpoints.cs, GroupEndpoints.cs | GET/POST, DTO, CSRF, route |
| backend/src/Plus5.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs | DI |
| backend/src/Plus5.Domain/Scheduling/RecurringSessionSeries.cs | nullable EndsOn i supersede guard |
| backend/src/Plus5.Infrastructure/Persistence/SchedulingPersistenceConfigurations.cs | nullable mapping |
| backend/src/Plus5.Infrastructure/Groups/EfGroupQuery.cs | otvorene serije u list/detailu |
| backend/src/Plus5.Infrastructure/Persistence/Migrations/20260911143606_AllowOpenEndedGroupSeries.cs, .Designer.cs, Plus5DbContextModelSnapshot.cs | migracija i siguran Down |
| backend/tests/Plus5.Api.Tests/Groups/GroupCreationQueryTests.cs | owner/rank/paging/read-only |
| backend/tests/Plus5.Api.Tests/Groups/GroupScheduleGeneratorTests.cs | 12 tjedana, kraj, DST |
| backend/tests/Plus5.Api.Tests/Groups/GroupCreationSqlTests.cs | upgrade, save, rollback, race, future rule, Down guard |
| backend/tests/Plus5.Api.Tests/Identity/AuthenticationApiTests.cs | auth/CSRF/validation/201/409 |
| frontend/src/groups/GroupCreatePage.tsx, GroupCreatePage.css | forma, save i canonical layout |
| frontend/src/groups/GroupCandidatePicker.tsx, GroupScheduleEditor.tsx, groupCreationModels.ts, GroupUnsavedGuard.tsx | odabir, raspored/lokacije, navigacija |
| frontend/src/App.tsx, frontend/src/app/AppRoutes.tsx | postojeći React Router data adapter i route |
| frontend/src/groups/GroupListPage.tsx, frontend/src/api/apiClient.ts | ulazna navigacija, potvrda, poruke |
| frontend/tests/GroupCreate.test.tsx, Groups.test.tsx | 8 create testova i navigacijski regression |
| frontend/tests/visual/phase36.mjs | full local write journey i read-only capture mode |
| docs/GROUP_CREATION.md, GROUP_FOUNDATION.md, SCHEDULING_FOUNDATION.md | zaključani contract |
| docs/DECISION_LOG.md, OPEN_QUESTIONS.md, ROADMAP.md | ADR-0015, zatvoreni create gateovi i status |
| docs/FRONTEND_FOUNDATION.md, GROUP_LIST.md | aktualna navigacija |
| docs/visual-acceptance/phase-3.6/*, docs/visual-acceptance/README.md | dokazi i iznimke |
| ovaj summary | završna predaja |

## Domain / baza / migracija
Nema novih entiteta. EndsOn postaje nullable bez backfilla i bez promjene starih datuma.
SQL test migrira prethodnu shemu s postojećom serijom i provjerava očuvanje datuma.
Down odbija povratak dok postoje otvorene serije (51000), ne izmišlja datum i ne briše ih.
Postojeći SQL suite provjerava clean migration path. 3NF, FK, unique occurrence i
one-active-membership indeksi ostaju. Nema Student/Group kopije rasporeda.

## API i sigurnost
GET create-candidates, GET create-locations i POST /api/v1/groups; 201 s ID-em i brojem
početnih Sessiona. Teacher policy + session owner, rowversion, CSRF, bounded paging i
tehnički request limiti 100 početnih učenika / 14 slotova. Capacity nije ograničen tim
limitom zahtjeva. 400/401/403/404/409 contract u [GROUP_CREATION](../GROUP_CREATION.md).
Nema client ownershipa, izloženih tajni ili mutiranja korisničkog fixturea kroz testove.

## Testovi i stvarni rezultati
| Provjera | Rezultat |
|---|---|
| Release solution build | PASS, 0 warninga / 0 grešaka |
| dotnet test Plus5Enterprise.sln -c Release --no-restore s lokalnim SQL env | 138 API/domain/persistence PASS + 4 architecture PASS; 0 SKIP |
| SQL upgrade, atomic create/rollback, race, otvorene serije | PASS |
| dotnet format --verify-no-changes --no-restore | PASS |
| npm test | 37/37 PASS |
| npm run lint / npm run build | PASS |
| Docker build/migration/runtime | PASS, API/frontend/database healthy |
| Stvarni UI: član + otvoreni raspored | 201, 12 Sessiona |
| Stvarni UI: konflikt, zatim prazna grupa bez rasporeda | 409, zatim 201 s 0 Sessiona |
| Canonical desktop / mobile / dirty navigation | PASS uz dokumentirane iznimke |
| NuGet vulnerability audit | PASS — 0 poznatih ranjivosti |
| npm audit | PASS — 0 ranjivosti |
| Novi dependencyji | nema novih dependencyja |

Prvi SQL race otkrio je EF wrapper oko deadlocka; mapiran je u kontrolirani conflict i
suite je ponovljen uspješno. Browser test korigirao je odabir kontrole dana; zatim je
uspješno odradio full save journey. Format generatorove migracije je usklađen i provjeren.
Nijedan prethodni FAIL/SKIP nije predstavljen kao završni PASS bez ponavljanja.

## Visual acceptance
[Zapis i snimke](../visual-acceptance/phase-3.6/README.md): canonical layout i action
hierarchy, desktop 1536×1024, mobile 390×844 bez document overflowa. Lokacija je u
srednjem panelu; sr-only nazivi ne zauzimaju prostor; modal koristi DS akcije.
Izričito su navedene razlike zbog SA odluka, postojećeg shella, native kontrola i
odobrenih Knowledge/materials/notes granica. Pripremne slike ostaju povijesni PARTIAL
dokazi, ne zamjena za finalni create review.

## Self-review / ADR / rizici
ADR-0015 zaključava business i operational odluke; data-router koristi postojeću
biblioteku radi navigacijskog gatea. Nema scopea 3.7/Phase 4 UI-ja. Replenishment i
future-window revalidation ostaju Phase 4; bez toga se kalendar ne dopunjava automatski.
Nema tvrdnje o produkcijskom load/security locku.

Lokalni demo sadrži dvije nove označene grupe, 12 termina i dva označena testna učenika
(jedan ostao iz prvog browser pokušaja). Raniji stvarni učenici nisu mijenjani.
Zasebne SQL testne baze uklonjene su u finally. Aplikacija je ostala pokrenuta za review.

## Točna početna točka
Završni SA review 3.6, zatim zasebno odobrenje commita/pusha. Nakon prihvaćanja sljedeća
je 3.7 Edit group, uz njezin postojeći gate promjene programa s aktivnim članovima.
