# Knowledge Component model

## Status

**LOCKED contract v1.0 — Phase 5.2 — 2026-09-16**

Ovaj dokument zaključava globalni, verzionirani PLUS 5 Knowledge Model i njegovu kontroliranu
hijerarhiju. Ne uvodi produkcijski katalog, Student evidence, mastery, readiness ili API/UI.

## Granica modela

```text
KnowledgeModel
└── KnowledgeArea
    └── KnowledgeComponent
        └── KnowledgeComponent
            └── ...
```

- `KnowledgeArea` je organizacijska kategorija i nije `KnowledgeComponent`.
- `KnowledgeComponent` je konkretna kontrolirana jedinica znanja ili vještine.
- komponenta pripada točno jednom `KnowledgeArea` i jednoj konkretnoj `KnowledgeModel`
  verziji.
- model nije Teacher-owned; komponenta nema Curriculum, SchoolGrade, ProficiencyLevel,
  Program ni Teacher FK.
- `Tag` nije `KnowledgeComponent`.

## KnowledgeModel i lifecycle

| Polje | Pravilo |
|---|---|
| `Id` | obavezni stabilni PLUS 5 GUID |
| `Code` | obavezni normalizirani family code, najviše 64 znaka |
| `Version` | obavezna normalizirana verzija, najviše 64 znaka |
| `Status` | `Draft`, `Published` ili `Retired` |

`(Code, Version)` je globalno jedinstven. `KnowledgeModel` verzija je jedina versioning i
immutability granica; nema `ValidFrom`, `ValidTo` ili `RevisionNumber` na Area/Component retku.

Lifecycle je jednosmjeran:

```text
Draft → Published → Retired
```

- `Draft`: dopušteno je dodavanje i uređivanje strukture.
- `Published`: Area/Component semantika i hijerarhija su immutable.
- `Retired`: povijesne reference ostaju valjane, ali verzija nije default za novi rad.
- `Published` i `Retired` model ne smiju se fizički brisati.
- nema povratka statusa unatrag ni preskakanja iz `Draft` izravno u `Retired`.

## KnowledgeArea

| Polje | Pravilo |
|---|---|
| `Id` | obavezni GUID |
| `KnowledgeModelId` | obavezni restriktivni FK na konkretnu model verziju |
| `Name` | obavezni trimani naziv, najviše 200 znakova |
| `Description` | nullable opis, najviše 2000 znakova |
| `SortOrder` | obavezni nenegativni redoslijed |

Phase 5.2 ne hardkodira šest područja iz screen primjera i ne seeda nazive poput Grammar,
Vocabulary ili Algebra. Production katalog ostaje prazan do odobrenog source/catalog
contracta.

## KnowledgeComponent

| Polje | Pravilo |
|---|---|
| `Id` | obavezni stabilni PLUS 5 GUID |
| `KnowledgeModelId` | obavezni FK na konkretnu model verziju |
| `KnowledgeAreaId` | obavezni FK na Area iste model verzije |
| `ParentComponentId` | nullable self-reference unutar istog modela i Area |
| `SupersedesKnowledgeComponentId` | nullable lineage prema drugoj verziji iste model obitelji |
| `Name` | obavezni trimani naziv, najviše 300 znakova |
| `Description` | nullable opis, najviše 4000 znakova |
| `SortOrder` | obavezni nenegativni sibling redoslijed |
| `Status` | `Active` ili `Deprecated` |

`Deprecated` komponenta ostaje valjana za postojeće povijesne reference, ali ne treba biti
default target za novi mapping/task authoring. Nema `ArchivedAtUtc` ni hard-deletea povijesti
objavljenog modela.

## Tree invarijante

- komponenta ima najviše jednog parenta; koristi se stablo, ne DAG
- root component ima `ParentComponentId = null`
- parent mora pripadati istom `KnowledgeModelId` i `KnowledgeAreaId`
- self-parent i proizvoljni ciklusi su zabranjeni
- dubina nije hardkodirana
- redoslijed je `SortOrder`, zatim stabilni `Id`
- cross-cutting odnosi pripadaju eksplicitnim mapping tablicama, ne višestrukim parentima

Domena provjerava scope i self-reference. Composite FK-ovi te SQL trigger štite same-model,
same-area i arbitrary-cycle integritet čak i pri izravnom SQL upisu.

## Component lineage

Lineage je optional jer se ekvivalent ne smije izmišljati. Kada postoji:

```text
Previous.KnowledgeModel.Code == Current.KnowledgeModel.Code
Previous.KnowledgeModel.Version != Current.KnowledgeModel.Version
```

Same-version, cross-family i self supersession su zabranjeni. Novi model dobiva novi Component
identitet; stari redak se ne mijenja niti ponovno koristi.

## Leaf i budući Evidence

Leaf je strukturna činjenica: komponenta je leaf kada nema child komponenti u istoj model
verziji. Phase 5.2 ne sprema `EvidenceEligible`, `MasteryScore`, `ReadinessScore` ili
`EvidenceCount`.

Budući direct `EvidenceEvent` smije ciljati samo leaf. Parent mastery/readiness kasnije se
izvodi iz djece prema zasebno zaključanom algoritmu; jedan rezultat ne smije se duplicirati na
svim precima. Phase 5.2 samo osigurava query foundation za određivanje leafova.

## CurriculumOutcome mapping

Phase 5.2 uvodi eksplicitni M:N:

```text
CurriculumOutcome
        *
        |
CurriculumOutcomeKnowledgeComponent
        |
        *
KnowledgeComponent
```

Junction sadrži samo:

- `CurriculumOutcomeId`
- `KnowledgeComponentId`

Composite PK `(CurriculumOutcomeId, KnowledgeComponentId)` sprječava duplikat. Oba FK-a su
restriktivna i pokazuju na konkretne verzionirane retke. Nema weighta, contribution postotka,
required masteryja, importancea, evidence strengtha ni readiness thresholda.

Mapping je dio objavljene semantike konkretne `KnowledgeModel` verzije:

- `Draft`: mapping INSERT, UPDATE i DELETE su dopušteni
- `Published` ili `Retired`: mapping INSERT, UPDATE i DELETE su zabranjeni
- promjena mappinga objavljenog modela zahtijeva novu Draft verziju modela, nove/preslikane
  komponente i mapping u toj verziji

SQL lifecycle guard provodi isto pravilo i kada se junction mijenja izravnim SQL-om.

## Persistence i query contract

- tablice: `KnowledgeModels`, `KnowledgeAreas`, `KnowledgeComponents` i
  `CurriculumOutcomeKnowledgeComponents`
- svi poslovno važni delete behaviori su `Restrict`
- composite alternate/FK ključevi štite model/Area ownership parenta
- indeksi podržavaju determinističan Area i Component tree traversal te lineage/mapping lookup
- CHECK constrainti štite statuse, nenegativni sort, self-parent i self-supersession
- SQL triggeri štite lifecycle, objavljenu strukturu i mapping, arbitrary cycle te lineage
  family/version
- nema seeda, backfilla, production kataloga ni importa

## Izvan Phase 5.2

- `EvidenceEvent`, Student evidence i correction lifecycle
- mastery/readiness/confidence izračun, weights, decay i thresholds
- direct evidence enforcement prije Evidence faze
- AI inference ili automatski Curriculum mapping
- production Knowledge katalog ili authoritative import
- Teacher custom Knowledge Model
- Tag mapping, task/material mapping, API, CRUD i frontend ekran
- konačni `KnowledgeBlock` contract
