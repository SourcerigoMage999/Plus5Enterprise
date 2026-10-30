# Lesson Builder formal domain contract

## Status

**Phase 9.1 — FINAL LOCKED — odobreno 2026-10-30**

Ovaj dokument zaključava formalni model pripreme sata prije persistence, API i UI
implementacije Phase 9.4–9.9. Phase 9.1 je `DOCS_ONLY`: ne uvodi runtime klase, tablice,
migraciju, endpoint ili ekran.

Sourceovi:

- `source_specs/5.0_Priprema_sata_Lesson_Builder.md`
- `source_specs/5.1_Cilj_sata.md` – `5.6_Potvrdena_priprema_Sat_je_spreman.md`
- `source_specs/LESSON_BUILDER_KNOWLEDGE_BLOCK_CATALOG.md`
- SA odluka za Phase 9.1 od 2026-10-30

Lesson Builder planira budući rad. Ne stvara izvedeni sat, odgovor učenika ni dokaz znanja.

## 1. Aggregate i canonical pojmovi

```text
ActivityTemplateVersion
        ↓ instantiate / deep copy
LessonActivity
        ↓ Phase 10 runtime materialization
LessonSessionActivity
        ↓ stvarni rad učenika
StudentAttempt
        ↓ zasebni Evidence emitter
EvidenceEvent
```

- `ActivityTemplate` je stabilni identitet ponovno iskoristivog pedagoškog predloška.
- `ActivityTemplateVersion` je versioned sadržaj predloška.
- `LessonActivity` je konkretni planirani snapshot unutar jedne `LessonPlanVersion`.
- `LessonSessionActivity` je buduća runtime instanca i nije dio Phase 9.1.
- `KnowledgeBlock` je versioned pedagoška kategorija/strategija KB-001–KB-025.
- `KnowledgeComponent` je kontrolirana jedinica znanja.

Vrijedi:

```text
KnowledgeBlock != KnowledgeComponent
KnowledgeBlock != ActivityTemplate
KnowledgeBlock != LessonActivity
ActivityTemplate != LessonActivity
LessonActivity != LessonSessionActivity
```

Knowledge Block klasifikacija je opcionalna i ne mapira automatski Knowledge Components.
Formalni katalog i njegova pravila pripadaju Phase 9.3.

## 2. LessonPlan root i version boundary

`LessonPlan` je stabilni aggregate root:

```text
LessonPlan
  Id
  TeacherId
  StudentId?
  GroupId?
  SessionId?
  LifecycleStatus: Active | Archived
  CurrentReadyVersionId?
  RowVersion
  CreatedAtUtc
  ArchivedAtUtc?
```

`LessonPlanVersion` je konkretni snapshot:

```text
LessonPlanVersion
  Id
  LessonPlanId
  VersionNumber
  Status: Draft | Ready | Superseded
  Goal
  RowVersion
  CreatedAtUtc
```

Novi plan uvijek nastaje kao `Version 1 / Draft`. Ne postoji implicitni Ready plan.

Po planu smije postojati najviše jedan otvoreni `Draft` i jedan trenutačni `Ready`.
`CurrentReadyVersionId` pokazuje samo na Ready verziju istog plana.

## 3. Participant i Session kontekst

Plan ima točno jedan participant kontekst:

```text
Student XOR Group
```

Nikad oba i nikad nijedan. Student ili Group mora pripadati autentificiranom Teacheru.

`SessionId` je opcionalan. Kada postoji:

- Session mora pripadati istom Teacheru;
- individualni Session mora pokazivati na isti Student;
- grupni Session mora pokazivati na istu Group;
- cross-context attachment se odbija.

Povezivanje plana ne mijenja lifecycle Sessiona.

## 4. Version lifecycle

Samo `Draft` je mutable. `Ready` i `Superseded` su immutable, uključujući njihove child i
junction retke.

Draft dopušta promjenu cilja, plan-level Knowledge Components, aktivnosti, redoslijeda,
trajanja, materijala, Task/Quick Check referenci, bilješki i instructional metapodataka.

Prvi confirm atomski radi:

```text
Draft v1 → Ready v1
CurrentReadyVersionId = v1
```

Confirm kasnije verzije atomski radi:

```text
Ready v1      → Superseded v1
Draft v2      → Ready v2
CurrentReadyVersionId = v2
```

Edit Ready plana nikada ne otvara Ready snapshot in-place, nego ga duboko klonira u sljedeću
Draft verziju. Prethodni Ready ostaje trenutačni Ready do uspješnog confirma novog Drafta.

Restore Superseded verzije također radi deep clone u novu, kasniju Draft verziju. Stari
snapshot ne mijenja status natrag u Ready.

`LessonPlan.RowVersion` štiti root/lifecycle operacije: stvaranje Drafta iz Ready verzije,
confirm, archive i promjenu current-version reference. `LessonPlanVersion.RowVersion` štiti
Draft authoring. Stale write vraća `409 concurrency_conflict`; last-write-wins je zabranjen.

## 5. LessonPlan Knowledge mapping

`LessonPlanVersion` ima eksplicitni M:N prema `KnowledgeComponent`.

Semantika je instructional focus — ono što Teacher planira obrađivati. Za novi mapping
dopušten je `Published` KnowledgeModel i `Active` KnowledgeComponent; leaf-only nije uvjet.
Retired/Deprecated historical target ostaje vidljiv u starom snapshotu, ali nije novi izbor.

Plan-level ili activity-level Knowledge mapping nije Evidence target. Mapping
`AssessableTaskVersion → KnowledgeComponent` ostaje zasebni, leaf-only authority za ono što
konkretni procjenjivi task mjeri. Skupovi se ne kopiraju automatski jedan u drugi.

## 6. ActivityTemplate

```text
ActivityTemplate
  Id
  OwnerKind: System | Teacher
  TeacherId?
  LifecycleStatus: Active | Archived
  CurrentPublishedVersionId?

ActivityTemplateVersion
  Id
  ActivityTemplateId
  VersionNumber
  Status: Draft | Published | Retired
```

- System template je globalan; Teacher template je privatan konkretnom Teacheru.
- Phase 9.1 ne uvodi sharing Teacher templatea.
- Draft verzija je mutable; Published i Retired su immutable.
- Promjena Published templatea stvara novu verziju.
- Retired verzija nije novi izbor, ali povijesni LessonActivity snapshot ostaje valjan.
- Teacher template može prijeći `Active → Archived`; System template se ne arhivira običnim
  Teacher API-jem.
- Published povijest nema hard delete.

Instanciranje templatea kopira njegov sadržaj i instructional KC mapping u novi
`LessonActivity`. Kasnija promjena templatea ne mijenja postojeću aktivnost. Template može
imati version-aware preporuku materijala, ali ne posjeduje obaveznu live `MaterialId`
referencu; Teacher pri instanciranju potvrđuje exact MaterialVersion.

## 7. LessonActivity snapshot

Minimalni relacijski model:

```text
LessonActivity
  Id
  LessonPlanVersionId
  SourceActivityTemplateVersionId?
  KnowledgeBlockVersionId?       # FK se uvodi tek s Phase 9.3 katalogom
  SortOrder
  Name
  ActivityTypeCode
  DurationMinutes
  DeliveryMode / WorkMode
  Description
  TeacherInstructions
  StudentInstructions
  Notes
  BoardVisibility
```

Aktivnost ima zasebne veze prema:

- `KnowledgeComponent`;
- exact `MaterialVersion`;
- exact `AssessableTaskVersion`.

Core aktivnost nije nevalidirani JSON blob. Ownership se izvodi kroz
`LessonActivity → LessonPlanVersion → LessonPlan → TeacherId`; zasebni TeacherId na aktivnosti
se ne duplicira.

`DurationMinutes` je cijeli broj `1..480`. Phase 9.1 ne uvodi KB min/optimal/max vrijednosti.
Zbroj aktivnosti ne mora točno odgovarati trajanju Sessiona; mismatch je budući balance
warning, ne DB-invalid state.

`SortOrder` je contiguous `0..N-1` unutar verzije plana. Add, delete, duplicate i reorder
moraju biti atomski i ne smije postojati duplicate order.

Draft smije privremeno imati nula aktivnosti. Ready zahtijeva najmanje jednu aktivnost i
pozitivno ukupno trajanje.

Duplicate u Draftu stvara novi `LessonActivity.Id` i duboko kopira tekst, trajanje, Knowledge
mapping, MaterialVersion i TaskVersion reference te template/KB provenance. Kopija se umeće
odmah iza izvornika i nema zajednički mutable child state.

Remove je dopušten samo u Draftu i smije fizički ukloniti draft child retke. LessonActivity
nema zasebni archive lifecycle. Ready/Superseded aktivnost ostaje dio immutable snapshota.

## 8. Material i AssessableTask reference

Aktivnost može imati `0..N` exact `MaterialVersion` i `0..N` exact
`AssessableTaskVersion` referenci.

Za novi Material attachment dopušten je current `Active` MaterialVersion s usable/Clean
sadržajem prema Materials contractu. Ako kasnije nastane nova verzija materijala, plan ostaje
na staroj verziji dok Teacher eksplicitno ne promijeni Draft plan. Ready/Superseded plan
zadržava povijesnu, i eventualno kasnije Superseded, verziju radi reproducibilnosti.

Procjenjiva aktivnost referencira exact `AssessableTaskVersion`, ne stabilni
`AssessableTask`. Svaki TaskVersion mora pripadati exact MaterialVersionu koji je attached
istoj aktivnosti. Novi TaskVersion nikada se ne preuzima automatski.

## 9. Quick Check, Attempt i Evidence granica

Planirani Quick Check je samo namjera korištenja exact `AssessableTaskVersion` tijekom sata.

On:

- ne stvara `StudentAttempt`;
- ne stvara `EvidenceEvent`;
- ne mijenja Mastery/Readiness;
- ne planira budući `PerformanceScore`, `AssistanceLevel` ili stvarni `EvidenceContext` kao
  već nastalu činjenicu.

Difficulty i EvidenceType dolaze iz exact TaskVersion snapshot-a. Runtime činjenice nastaju
tek u Phase 10+ nakon konkretnog rada učenika.

## 10. Ready validation

Prije `Draft → Ready` server ponovno provjerava najmanje:

- owner scope;
- Student XOR Group i optional Session same-context pravila;
- goal nije prazan;
- postoji najmanje jedan LessonActivity;
- order je contiguous;
- sva trajanja su pozitivna i ukupno trajanje je pozitivno;
- sve MaterialVersion reference postoje i predstavljaju dopuštene exact snapshotove;
- svaki TaskVersion pripada MaterialVersionu attached istoj aktivnosti;
- Task i Knowledge mapping invarianti i dalje vrijede;
- nema dangling referencea.

Browser nije authority. `Ready` znači samo da je plan potvrđen i spreman za izvođenje. Ne
stvara `LessonSession`, ne pokreće Session, ne bilježi attendance, Attempt, Evidence niti Held/
Completed stanje.

## 11. Archive i delete

`LessonPlan` može prijeći `Active → Archived`. Archive skriva plan iz aktivnih planner lista,
ali ne briše verzije, aktivnosti, Material/Task reference, povezani Session ni buduću runtime
povijest. Hard delete LessonPlana nije dio Phase 9.1.

Draft child retci smiju se ukloniti tijekom authoringa. Ready/Superseded aktivnosti i
Published template history nemaju hard delete.

## 12. Persistence invarianti za implementacijske faze

Kada Phase 9.4–9.9 uvedu persistence, SQL sloj mora štititi najmanje:

- Teacher ownership i Student XOR Group;
- optional Session same-Teacher/same-participant kontekst;
- unique `(LessonPlanId, VersionNumber)`;
- najviše jedan Draft i jedan current Ready po planu;
- dopuštene root/version statuse i current-version konzistenciju;
- Ready/Superseded snapshot immutability;
- contiguous/unique activity order i trajanje `1..480`;
- same-plan/version child konzistenciju;
- exact MaterialVersion i AssessableTaskVersion FK-ove;
- TaskVersion → attached owning MaterialVersion konzistenciju;
- Draft-only junction mutation;
- restrictive delete za povijest.

Kritični invarianti ne smiju ostati samo u browser validatoru.

## 13. Security i privacy granica

Svi budući read/write use caseovi su Teacher-only i owner-scoped. ID iz requesta nije dokaz
ownershipa. Cross-owner i cross-participant reference se odbijaju na serveru i DB granici gdje
je izvedivo. Free text se tretira kao nepouzdan sadržaj i ne logira nepotrebno.

Ovaj contract ne daje AI sustavu pristup Student/Group podacima. AI input, explainability,
retention, audit, group fairness, provider prompt boundary i učenje iz Teacher odluka ostaju
zasebni BLOCKED gate Phase 9.2.

## 14. Izvan Phase 9.1

Nisu implementirani ni dopušteni preuranjeno:

- domain/EF/schema/API/UI runtime implementacija;
- AI provider, personalization, recommendation scoring ili model improvement;
- KB katalog i min/optimal/max applicability pravila;
- `LessonSessionActivity`, attendance, Board runtime ili autosave;
- StudentAttempt ili Evidence emission;
- mastery/readiness mutation;
- automatski Material/Task upgrade;
- automatski Knowledge expansion.

## 15. Zaključani handoff

Phase 9.2 mora zasebno zaključati Lesson Builder AI privacy/explainability/audit contract.
Phase 9.3 tek nakon 9.1 i 9.2 smije formalizirati versioned KB-001–KB-025 katalog.
Persistence i user-facing workflow počinju u svojim ROADMAP podfazama 9.4–9.9.
