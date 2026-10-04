# Assessable task metadata within Material

## Status

**Phase 6.5 — IMPLEMENTED / REVIEW READY.**

Ovaj dokument proširuje zaključane Material i Evidence contracte verzioniranim metapodacima
procjenjivog zadatka. Ne uvodi Attempt runtime ni automatsko stvaranje Evidence Eventa.

## Aggregate i verzijska granica

`AssessableTask` je stabilni identitet zadatka unutar jednog `Materiala`.
`AssessableTaskVersion` je potpuni pedagoški snapshot vezan uz točno jednu `MaterialVersion`:

```text
Material
├── MaterialVersion 1
│   └── AssessableTaskVersion 1
└── MaterialVersion 2
    └── AssessableTaskVersion 2
```

- jedan stabilni Task ima najviše jednu verziju unutar iste MaterialVersion;
- version number je pozitivan i jedinstven unutar stabilnog Taska;
- composite FK-ovi fizički jamče da Task, TaskVersion i MaterialVersion pripadaju istom
  Material identitetu;
- Task metadata može se pripremati samo unutar Draft MaterialVersion;
- aktivacija MaterialVersion zaključava TaskVersion i njegove Knowledge mappinge;
- pedagoški značajna promjena objavljenog prompta, tipa, odgovora/kriterija, bodovanja,
  difficultyja, EvidenceTypea ili Knowledge mappinga zahtijeva novu TaskVersion u novoj Draft
  MaterialVersion; povijest se ne prepisuje.

Phase 6.5 ne uvodi zaseban Task status. Objavljivost i immutability slijede postojeću
`Draft → Active → Superseded` granicu MaterialVersiona. Strukturirane opcije odgovora i
authoring/runtime payload dolaze sa svojim editor/runtime contractom; kada budu uvedeni, moraju
ostati dio istog version-bound snapshota.

## Minimalni metadata snapshot

Svaki `AssessableTaskVersion` sadrži:

- stabilni Task ID i vlastiti TaskVersion ID/version number;
- redoslijed unutar MaterialVersion;
- obavezni prompt;
- obavezni canonical `TaskTypeCode` do 64 znaka. Phase 6.5 ne izmišlja zatvoreni katalog tipova;
  code je uppercase tehnički discriminator za budući odobreni editor/runtime;
- `Difficulty 1..5` iz zaključanog Evidence metadata contracta;
- jedan zaključani v1 `EvidenceType`: Recognition, Understanding, Application ili Production;
- pozitivan `MaxPoints` decimalni iznos;
- najmanje jedno od: točan odgovor ili kriterij vrednovanja;
- jednu ili više kontroliranih Knowledge Component veza.

`AssistanceLevel` i `EvidenceContext` nisu Task metadata. Oni opisuju konkretni runtime
evidence trenutak i ostaju na EvidenceEvent snapshotu.

## Knowledge mapping i Evidence granica

`AssessableTaskVersionKnowledgeComponent` je čisti M:N mapping bez weighta ili scorea.

- svaki procjenjivi Task prije aktivacije MaterialVersion mora imati barem jedan target;
- target može biti samo leaf `KnowledgeComponent` iz Published ili Retired KnowledgeModel
  verzije;
- Draft model i parent komponenta nisu valjani direct evidence target;
- različite KnowledgeModel verzije ostaju eksplicitne i ne stapaju se;
- sam Material, Task metadata, otvaranje ili pregled sadržaja ne stvara EvidenceEvent;
- budući valjani StudentAttempt može biti ulaz za eksplicitni server-controlled Evidence
  emission, uz snapshot Difficulty/EvidenceTypea iz točne TaskVersion i runtime
  AssistanceLevel/EvidenceContext;
- Phase 6.5 nema Attempt, response, grading, Evidence emitter, mastery recalculation ili javni
  generic Evidence write API.

## Read-only Material detail

Postojeći `GET /api/v1/materials/{materialId}` zadržava isti owner/share SQL authorization i
dodaje task snapshotove current Active/Clean MaterialVersiona. Za svaki Task vraća samo
Teacher-visible metadata, version, scoring, answer/criterion i točne versioned Knowledge
targete. Storage i scan detalji i dalje se ne izlažu.

Screen 4.2 prikazuje `Zadaci i procjena` listu ili pošteno prazno stanje. Prikaz ne računa
accuracy, mastery, readiness, activity history ni preporuke i ne tvrdi da postoji Evidence.

## SQL integrity

Migracija `AddAssessableTaskMetadata` uvodi tri tablice bez seeda ili backfilla. Baza štiti:

- same-Material composite FK-ove i restrictive delete;
- unique Task version number i najviše jednu TaskVersion po MaterialVersion;
- positive version/order/points, Difficulty range, EvidenceType katalog i answer/criterion shape;
- canonical task type code;
- Draft-only TaskVersion i mapping mutacije;
- Published/Retired leaf-only Knowledge targete;
- minimalno jedan Knowledge target pri aktivaciji MaterialVersion.

## Izvan Phase 6.5

Nema create/edit/import UI-ja, write endpointa, Task option/response runtimea, StudentAttempta,
automatskog ocjenjivanja, Evidence emissiona, teacher-assessed runtimea, binary storage adaptera,
Lesson/Homework/Board integracije, usage analitike, AI authoringa ni production task type kataloga.
