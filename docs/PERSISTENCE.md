# Database & persistence foundation

## Status

**LOCKED — implementirano i runtime provjereno 2026-08-25**

Ovaj dokument definira persistence contract Phase 1.2 prema ADR-0003 i obaveznim database, security i Docker standardima.

## Stack i granice

- SQL Server 2025 (17.x) za lokalni Compose, službeni Microsoft image pinnan digestom
- Entity Framework Core SQL Server + Design `10.0.11`
- `Plus5DbContext` u Infrastructure sloju
- EF Core migracije su jedini schema source of truth
- Phase 1.6 dodaje samo Teacher identity/session/token tablice; nema Student/Guardian/Admin accounta, role matrice ni feature business tablica
- DbContext je scoped; command timeout je 30 sekundi

## Početna migracija

`InitialPersistenceFoundation` je namjerno prazna business migracija. Ona zaključava migration assembly, EF provider/version i reproducibilan clean migration path, a u bazi stvara samo EF `__EFMigrationsHistory` infrastrukturu.

Nova schema promjena mora dobiti novu smislenu migraciju. API nikada ne poziva `Database.Migrate()` na startupu. Migration se izvršava kao odvojeni kontrolirani korak prije pokretanja verzije API-ja koja ovisi o shemi.

## Identiteti i ovlasti

Lokalni Compose koristi tri odvojena identiteta:

| Identitet | Namjena | Ovlasti |
|---|---|---|
| `sa` | inicijalno stvaranje baze i loginova | bootstrap samo; API ga ne koristi |
| `plus5_migrator` | EF Core schema migracije | `db_owner` lokalne Plus5 baze |
| `plus5_app` | API runtime | `db_datareader` + `db_datawriter`, bez schema-owner ovlasti |

Produkcijski deployment mora zasebno dostaviti migration i runtime credentials kroz secrets sloj. DB port ne smije biti javno izložen.

## Local Compose workflow

1. Kopirati `.env.example` u necommitani `.env`.
2. Postaviti tri različite snažne SQL lozinke. Zbog SQLCMD lokalnog init boundaryja ne koristiti `'` ni `;`.
3. Pokrenuti `docker compose up --build --wait`.
4. Compose čeka SQL health, idempotentno inicijalizira bazu/logine, izvršava jednokratnu EF migraciju te tek onda pokreće API i frontend.
5. `docker compose down` uklanja containere i mrežu, ali čuva named volume `plus5-sql-data`.

Brisanje volumea briše lokalnu bazu i nije normalan shutdown workflow.

## Host migration workflow

Za kontroliranu migraciju iz hosta:

```powershell
dotnet tool restore
$env:PLUS5_MIGRATION_CONNECTION_STRING = "<migration connection string>"
dotnet tool run dotnet-ef -- database update --project .\backend\src\Plus5.Infrastructure\Plus5.Infrastructure.csproj
Remove-Item Env:PLUS5_MIGRATION_CONNECTION_STRING
```

Lokalni container s nepouzdanim development certifikatom dodatno zahtijeva privremeni `PLUS5_MIGRATION_ALLOW_UNTRUSTED_CERTIFICATE=true`. Ta zastavica nije dopuštena za Staging/Production.

## Readiness

- `/health/live` provjerava samo proces i ne ovisi o SQL Serveru.
- `/health/ready` koristi EF Core DbContext probe i vraća healthy samo kada se može pristupiti bazi i nema pending migracija.
- Vanjski DB kvar ne smije uzrokovati liveness restart storm.

## Migration quality gate

Prije prihvaćanja svake schema promjene obavezno je:

- Release build + test
- `dotnet ef migrations has-pending-model-changes`
- pregled generiranog migration C# i idempotent SQL scripta
- clean SQL Server apply
- ponovljeni/idempotent apply
- upgrade provjera kada postoje prethodni podaci
- constraint/index/delete/concurrency/security review prema stvarnom modelu

Phase 1.2 clean apply, ponovljeni idempotent apply, named-volume restart, readiness i least-privilege provjere izvršene su na stvarnom SQL Server 2025 containeru.

## Phase 4.6 materialization schema

Migracija `AddScheduleMaterializationReplenishment` dodaje dvije ograničene tablice bez seeda
ili backfilla:

- `ScheduleMaterializationIssues` — durable per-series/per-date/per-type evidence s restriktivnim
  FK-om, CHECK constraintima za tip/brojač/vremenski slijed, unique issue identitetom i
  filtered indeksom za neriješene issuee
- `ScheduleMaterializationLeases` — globalni SQL-backed expiring lease za multi-instance
  `Plus5.Api` worker; ne sadrži business payload.

Filtered work-queue indeks na aktivnim open-ended `RecurringSessionSeries` podupire bounded
scan po `CreatedAtUtc, Id`. Postojeći `UX_Sessions_Series_Occurrence` ostaje zadnja DB zaštita
idempotentnosti. Detaljni operational contract je u `RECURRENCE_MATERIALIZATION.md`.

## Phase 1.6 identity schema

Migracija `AddTeacherAuthenticationFoundation` dodaje:

- `UserAccounts` — immutable ID, canonical/normalized unique e-mail, framework password hash, ograničeni `AccountStatus`, security stamp i UTC audit vremena
- `AuthenticatedSessions` — server-side opoziva session identity, konačan expiry i security-stamp snapshot
- `AccountTokens` — purpose, expiry, consumption i samo SHA-256 representation CSPRNG verification/reset tokena; raw secret nije spremljen
- `DataProtectionKeys` — framework-managed shared ASP.NET Core Data Protection key ring za cookie encryption/signing preko restarta i više API instanci; key XML se izvan Developmenta štiti deployment certifikatom

Obje child tablice imaju restriktivni FK prema `UserAccounts`. Indeksi pokrivaju normalized e-mail lookup, token hash lookup, najviše jedan nepotrošen token po account/purpose kombinaciji te account/purpose/expiry i account/session expiry queryje. Check constrainti ograničavaju statuse i token purpose vrijednosti. Migracija ne stvara roleove ni tablice za Student, Guardian ili Administrator account.

## Phase 2.1 core teaching reference schema

Phase 2.1 dodaje četiri odvojena 3NF korijena:

- `Programs` — Teacher-owned pedagoška ponuda; restriktivni FK na `UserAccounts` i case-insensitive jedinstveni normalizirani naziv unutar Teacher scopea
- `SchoolGrades` — globalni code/name/sort referentni katalog bez seed pretpostavki
- `ProficiencyLevels` — globalni framework/code/name/sort katalog; CEFR nije hardkodiran kao jedini okvir
- `Curricula` — globalni code/name/version korijen s jedinstvenom code/version kombinacijom

Program nema SchoolGrade, ProficiencyLevel ni Curriculum FK. Phase 2.1 nije uvodio
Student/Group/Material veze, CurriculumOutcome hijerarhiju, seed podatke ni feature API.
Detaljni foundation contract je u `CORE_TEACHING_FOUNDATION.md`.

## Phase 5.1 curriculum outcome hierarchy schema

Migracija `AddCurriculumOutcomeHierarchy` dodaje globalnu `CurriculumOutcomes` adjacency-list
tablicu bez seeda ili backfilla. Composite same-Curriculum parent FK, restriktivni deleteovi,
CHECK constrainti, filtrirani official-code unique indeks i hierarchy trigger štite version
ownership, provenance, self/cycle zabrane te lineage samo između različitih verzija iste
`Curriculum.Code` obitelji. Query indeks
`CurriculumId + ParentOutcomeId + SortOrder + Id` podržava determinističan traversal bez
hardkodirane dubine. Nema Knowledge/Evidence mapiranja ni public write surfacea. Detalji su u
`CURRICULUM_HIERARCHY.md`.

## Phase 5.2 knowledge component schema

Migracija `AddKnowledgeComponentModel` dodaje `KnowledgeModels`, `KnowledgeAreas`,
`KnowledgeComponents` i čisti M:N `CurriculumOutcomeKnowledgeComponents`. Model/Area/parent
composite FK-ovi, restriktivni deleteovi, status/sort/self CHECK constrainti i SQL triggeri
štite Draft–Published–Retired lifecycle, immutable objavljenu strukturu, tree cycle zabranu te
same-family/different-version lineage. Junction lifecycle trigger dopušta CurriculumOutcome
mapping INSERT/UPDATE/DELETE samo dok je pripadajući `KnowledgeModel` Draft te odbija mutaciju
Published/Retired mappinga. Tree i mapping indeksi podržavaju deterministične read projekcije
i leaf query bez spremanja izvedenog `EvidenceEligible` polja. Nema seeda, produkcijskog
kataloga, Evidencea ni API write surfacea. Detalji su u
`KNOWLEDGE_COMPONENT_MODEL.md`.

## Phase 5.3 evidence event schema

Migracija `AddEvidenceEventModel` dodaje `EvidenceEvents` i čisti M:N
`EvidenceEventKnowledgeComponents` bez seeda ili backfilla. Restriktivni Student/Knowledge FK-ovi,
composite source-chain alternate key, filtered unique root/successor indeksi i CHECK constrainti
štite Student-specific provenance, jedan root po source rezultatu, lifecycle shape, bounded kodove
i UTC vremena. Composite self-FK zahtijeva isti Student/source identitet u cijelom chainu.

SQL triggeri zabranjuju event UPDATE/DELETE, mapping UPDATE/DELETE, fork/cycle, successor nakon
Invalidationa te Invalidation, Draft ili parent Knowledge target. Observation/Correction target
kardinalnost i atomski zapis eventa s mappingom provodi interni server-controlled emission service
u Serializable transakciji; nema javnog generic Evidence write surfacea. Nema scorea, masteryja,
readinessa ni production emitera. Detalji su u `EVIDENCE_EVENT.md`.

## Phase 5.4 evidence metadata schema

Migracija `AddEvidenceMetadata` aditivno proširuje `EvidenceEvents` nullable persistence kolonama
`Difficulty`, `EvidenceType`, `AssistanceLevel` i `EvidenceContext`, bez seeda, defaulta ili
backfilla. Conditional lifecycle CHECK zahtijeva sva četiri polja za Observation/Correction i sva
četiri `NULL` za Invalidation. Dodatni CHECK constrainti ograničavaju Difficulty na `1..5` i
string vrijednosti na zaključane bounded canonical kataloge. Postojeći append-only trigger štiti
i metadata kolone od in-place UPDATE-a. Correction sprema potpuni zamjenski snapshot, dok stari
event ostaje nepromijenjen. Detalji su u `EVIDENCE_METADATA.md`.

## Phase 2.2 Student profile schema

Phase 2.2 dodaje dva 3NF modela bez feature endpointa:

- `Students` — Teacher-owned profil osobe bez accounta, s obaveznim SchoolGradeom, opcionalnim paired Program/DeliveryMode podacima, tri operativna statusa i arhivskim UTC vremenom
- `Guardians` — opcionalni Student-owned kontakti bez accounta, uz filtered unique zaštitu najviše jednog primarnog kontakta po Studentu

Svi FK-ovi su restriktivni. Composite Student/Program FK uključuje `TeacherAccountId` i zato na razini baze odbija povezivanje Studenta s Programom drugog Teachera. Nema Group/GroupMembership, fotografije, messaginga, bilješki, knowledge/readiness polja, seeda ni backfilla. Detaljni contract je u `STUDENT_FOUNDATION.md`.

## Phase 2.3 Group schema

Phase 2.3 dodaje dva normalizirana modela bez feature endpointa:

- `Groups` — Teacher-owned grupa s obaveznim same-Teacher Programom, globalnim SchoolGradeom, pozitivnim kapacitetom, operativnim statusom, arhivskim vremenom i SQL Server `rowversion` concurrency tokenom
- `GroupMemberships` — vremenski zapis članstva Studenta u grupi s početkom i opcionalnim završetkom

Composite FK-ovi s mirrored `TeacherAccountId` fizički odbijaju cross-Teacher Program, Group i Student veze. Filtered unique indeks nad aktivnim članstvom dopušta najviše jednu grupu po Studentu, a CHECK constrainti štite kapacitet, statuse, arhiviranje i valjan vremenski interval. Brojanje članova naspram kapaciteta ostaje transakcijska poslovna invarijanta: budući use case mora zaključati/izmijeniti `Group` u istoj transakciji kako bi `rowversion` detektirao konkurentne upise. Nema rasporeda, termina, lokacije, materijala, ciljeva, bilješki, seeda ni backfilla. Detaljni contract je u `GROUP_FOUNDATION.md`.

## Phase 2.4 Schedule/session schema

Phase 2.4 dodaje tri Teacher-owned modela bez feature endpointa:

- `Locations` — ponovno upotrebljiva lokacija s jedinstvenim normaliziranim nazivom unutar Teacher scopea i arhiviranjem koje čuva povijesne veze
- `RecurringSessionSeries` — verzionirano tjedno pravilo za redoviti Group raspored ili individualnu Student recurrence, s lokalnim wall-clock vremenom, vremenskom zonom i vezom na prethodnu seriju
- `Sessions` — konkretan UTC termin s točno jednim Group/Student kontekstom, eksplicitnim statusom, opcionalnim series occurrence identitetom i oznakom iznimke

Composite FK-ovi fizički odbijaju Group, Student, Location ili Series drugog Teachera. CHECK constrainti štite context XOR, enum vrijednosti, vremenske intervale, cancellation audit, upareni series occurrence i zabranu istodobne fizičke/online lokacije. `rowversion` štiti izmjene Sessiona i Seriesa, a filtered unique `(RecurringSessionSeriesId, SeriesOccurrenceDate)` indeks čini materijalizaciju idempotentnom. Teacher-first indeksi podržavaju kalendar i buduće serializable overlap provjere. Migracija nema seed, backfill ni automatsko generiranje Session redaka. Detaljni contract je u `SCHEDULING_FOUNDATION.md`.
