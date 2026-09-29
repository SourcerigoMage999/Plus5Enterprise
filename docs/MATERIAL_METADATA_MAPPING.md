# Material metadata and curriculum/knowledge mapping

## Status

**FINAL LOCKED — Phase 6.2 — odobreno 2026-09-29**

Ovaj dokument proširuje zaključani `MATERIAL_FOUNDATION.md` isključivo version-bound
pedagoškim metapodacima. Ne uvodi Library/detail/import/edit API ili UI, Task metadata,
Evidence emission ni AI suggestion persistence.

## Snapshot granica

Svi podaci iz ove faze pripadaju konkretnoj `MaterialVersion`, nikad mutable `Material`
identitetu:

```text
Material
└── MaterialVersion
    ├── Program?                  (isti Teacher owner)
    ├── SchoolGrade?
    ├── ProficiencyLevel?         (npr. CEFR B1)
    ├── LearningGoal?
    ├── 0..N MaterialVersionTag
    ├── 0..N CurriculumOutcome
    └── 0..N KnowledgeComponent
```

- Draft metadata i mapping mogu se uređivati.
- Active i Superseded snapshot, uključujući sve veze, immutable je.
- Nova pedagoška promjena stvara novu Draft verziju; ne prepisuje povijesnu verziju.
- Restore stvara novu Draft verziju s istim direct metapodacima. Phase 6.7 orchestration mora
  atomarno kopirati i tag/outcome/component junction retke prije uređivanja ili aktivacije.
- Kolone su nullable i junction skupovi mogu biti prazni jer postojeći 6.1 retci nemaju
  backfill, Draft može biti nepotpun, a production referentni katalog još nije odobren.
  Budući 6.6/6.7 command validation provodi obavezna polja odgovarajućeg korisničkog flowa.

## Osnovni pedagoški metadata

- `ProgramId` je nullable veza na Teacher-owned `Program`. Composite FK
  `(CreatedByTeacherId, ProgramId)` fizički odbija Program drugog Teachera.
- `SchoolGradeId` je nullable veza na globalni `SchoolGrade`; nije CEFR razina.
- `ProficiencyLevelId` je nullable veza na globalni `ProficiencyLevel`. Model ne hardkodira
  CEFR kao jedini framework; CEFR B1 je redak s `FrameworkCode = CEFR`, ne enum ili tekst na
  Materialu.
- `LearningGoal` je nullable trimani opis cilja, najviše 2000 znakova. Cilj opisuje namjeru
  poučavanja i sam po sebi nikad nije dokaz da je Student nešto savladao.

## Tagovi

`MaterialVersionTag` je fleksibilna version-bound oznaka za library search/filter/organization:

- display `Name` je triman i ima najviše 64 znaka;
- `NormalizedName` je uppercase canonical lookup vrijednost;
- composite PK `(MaterialVersionId, NormalizedName)` odbija case-insensitive duplikat unutar
  verzije;
- tag nije `KnowledgeComponent`, CurriculumOutcome ni Evidence target;
- tag nema weight, hierarchy, mastery ili readiness semantiku.

## CurriculumOutcome mapping

`MaterialVersionCurriculumOutcome` je čisti M:N junction:

- composite PK `(MaterialVersionId, CurriculumOutcomeId)`;
- oba FK-a koriste `Restrict`;
- outcome pokazuje na točan redak konkretne `Curriculum` verzije;
- nema weighta, coveragea, required masteryja ili automatskog parent/child širenja.

## KnowledgeComponent mapping

`MaterialVersionKnowledgeComponent` je čisti M:N junction:

- composite PK `(MaterialVersionId, KnowledgeComponentId)`;
- oba FK-a koriste `Restrict`;
- novi mapping prema `Draft` KnowledgeModelu je zabranjen;
- `Published` model je default za novi rad, a `Retired` reference ostaju dopuštene radi
  povijesti i restorea;
- mapping smije ciljati komponentu na bilo kojoj razini stabla jer opisuje što materijal
  poučava. Leaf-only pravilo vrijedi za direct `EvidenceEvent`, ne za instructional Material;
- `Deprecated` komponenta ostaje valjana povijesna referenca, ali budući picker je ne nudi kao
  default za novi mapping.

CurriculumOutcome i KnowledgeComponent mapping su eksplicitni i neovisni. Postojeći
`CurriculumOutcomeKnowledgeComponent` može pomoći budućem UI-ju pri prijedlogu, ali sustav ne
stvara ili prepisuje jednu Material vezu automatski iz druge i ne skriva nedosljednost.

## Persistence zaštita

Migracija `AddMaterialMetadataMapping`:

- aditivno proširuje `MaterialVersions` nullable Program/Grade/Level/LearningGoal kolonama;
- dodaje tri version-bound tablice bez seeda ili backfilla;
- koristi restrictive FK-ove, composite PK-ove i lookup indekse;
- koristi composite same-Teacher Program FK;
- SQL triggeri odbijaju INSERT/UPDATE/DELETE tag/outcome/component retka kada verzija nije
  Draft;
- SQL trigger odbija KnowledgeComponent mapping prema Draft KnowledgeModelu;
- SQL trigger provjerava canonical tag display/normalized par;
- postojeći `TR_MaterialVersions_ProtectLifecycle` odbija direct metadata UPDATE Active ili
  Superseded verzije.

## Granice faze

Nema javnog API-ja, UI-ja, seeda referentnih podataka, automatskog AI mappinga, suggestion
persistencea, Tag kataloga, material-level mastery/readinessa, Task metadata ili Evidence
emisije. Program/Grade/Level nisu spojeni u zajednički `GradeLevel`, a tag nije zamjena za
Knowledge Model.
