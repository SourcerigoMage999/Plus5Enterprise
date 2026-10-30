# Knowledge Block catalog and applicability contract

## Status

**Phase 9.3 — FINAL LOCKED — odobreno 2026-10-30**

Ovaj dokument zaključava versioned system catalog za KB-001–KB-025 i deterministic
applicability model. Phase 9.3 je `DOCS_ONLY`: ne uvodi domain klase, seed, migraciju, API,
Lesson Builder UI, AI runtime ni balance engine.

Sourceovi:

- `source_specs/LESSON_BUILDER_KNOWLEDGE_BLOCK_CATALOG.md`;
- izvorni KB-001–KB-025 dokumenti iz paketa `Priprema sata`;
- `LESSON_BUILDER_DOMAIN.md`;
- `LESSON_BUILDER_AI.md`;
- SA odluka za Phase 9.3 od 2026-10-30.

`FINAL LOCKED` Phase 9.1 i 9.2 contracti imaju prednost nad starijim KB draft pravilima.

## 1. Canonical roster i početna zrelost

Svih 25 codeova postoje kao stable roots službenog PLUS 5 v1 kataloga:

| Code | Canonical display name | Initial v1 status |
|---|---|---|
| `KB-001` | Small Talk | Published |
| `KB-002` | Aktivacija predznanja | Published |
| `KB-003` | Pregled domaće zadaće | Published |
| `KB-004` | Objašnjenje novog gradiva | Published |
| `KB-005` | Interaktivni zadaci | Published |
| `KB-006` | Mini provjera razumijevanja | Draft |
| `KB-007` | Završna refleksija | Draft |
| `KB-008` | Speaking | Draft |
| `KB-009` | Listening | Draft |
| `KB-010` | Reading | Draft |
| `KB-011` | Writing | Draft |
| `KB-012` | Vocabulary Practice | Draft |
| `KB-013` | Grammar Practice | Draft |
| `KB-014` | Revision | Draft |
| `KB-015` | Domaća zadaća | Draft |
| `KB-016` | Procjena znanja | Draft |
| `KB-017` | Rad na pogreškama | Draft |
| `KB-018` | Motivacija učenika | Draft |
| `KB-019` | Projektni zadatak | Draft |
| `KB-020` | Suradnički zadatak | Draft |
| `KB-021` | Edukativna igra | Draft |
| `KB-022` | Simulacija stvarne situacije | Draft |
| `KB-023` | Izazov | Draft |
| `KB-024` | Priča | Draft |
| `KB-025` | Misija | Draft |

Postojanje root-a u katalogu nije production approval. Draft verzija:

- nije selectable za novi LessonActivity;
- nije AI candidate;
- ne smije završiti u novom Ready Lesson Planu;
- smije se administrativno dovršavati za budući publish.

Status se ne određuje iz codea/naziva niti drugim runtime heuristikama.

## 2. Stable root i versioned snapshot

Canonical model:

```text
KnowledgeBlock
  Id
  Code
  OwnershipKind: System
  LifecycleStatus: Active | Retired
  CurrentPublishedVersionId?
  CreatedAtUtc
  RetiredAtUtc?

KnowledgeBlockVersion
  Id
  KnowledgeBlockId
  VersionNumber
  Status: Draft | Published | Retired
  DisplayName
  Description
  MinDurationMinutes
  OptimalDurationMinutes
  MaxDurationMinutes
  ParticipantApplicability
  CompositionKind
  CreatedAtUtc
  PublishedAtUtc?
  RetiredAtUtc?
```

Root je stable identity. Version je exact pedagogical/applicability snapshot.

Svi KB-001–KB-025 rootovi su `System`. V1 nema Teacher-created KnowledgeBlock. Teacher može
stvarati privatne ActivityTemplatee, ali ne može stvarati code, preimenovati, publishati,
retireati ili uređivati applicability System bloka.

## 3. Immutable code i versioned naziv

`Code` je root-level immutable, unique, case-sensitive canonical business key:

- format `KB-001` – `KB-025` za početni roster;
- nikad se ne reciklira;
- retirement ga ne oslobađa;
- ne prenamjenjuje se za drugi koncept.

Fundamentalno novi koncept zahtijeva novi code kroz buduću odluku.

`DisplayName` je dio verzije. Published v1 se ne preimenuje in-place. Promjena naziva ili
semantike ide `Published v1 → Draft v2 → Published v2`, dok historical reference ostaje na v1.

## 4. Version lifecycle

Samo Draft je mutable. Published i Retired version snapshoti su immutable, uključujući:

- naziv, opis i svrhu;
- duration envelope;
- eligibility/recommendation rules;
- predecessor/successor rules;
- composition/nesting rules;
- grade/proficiency/delivery granice;
- homework/assessment semantiku.

Prvi publish atomski radi:

```text
Draft v1 → Published v1
CurrentPublishedVersionId = v1
```

Kasniji publish atomski radi:

```text
Published v1 → Retired v1
Draft v2     → Published v2
CurrentPublishedVersionId = v2
```

Po rootu postoji najviše jedan Draft i jedan current Published. Retired verzija nije novi
selectable/AI candidate, ne briše se i ostaje čitljiva kroz historical LessonActivity.

Root može `Active → Retired`. Root retirement zabranjuje nove verzije i novi odabir, ali ne
cascade-briše verzije, templatee ni aktivnosti. Neobjavljeni Draft može se administrativno
obrisati samo ako nikad nije bio Published i nema canonical reference. Published/Retired nema
hard delete.

## 5. Exact references

`LessonActivity` i `ActivityTemplateVersion` referenciraju optional exact
`KnowledgeBlockVersionId`, ne root `KnowledgeBlockId` kao canonical semantics.

Za novi LessonActivity/Template mapping verzija mora biti current Published, a root Active.
Historical Ready/Superseded plan i Published/Retired template smiju zadržati kasnije Retired
KB version.

LessonActivity u v1 ima najviše jedan primary KB version. KB klasifikacija ostaje optional;
custom aktivnost bez KB-a potpuno je valjana. KB i KnowledgeComponent mapping nisu isti:

```text
LessonActivity → 0..1 primary KnowledgeBlockVersion
LessonActivity → 0..N explicit KnowledgeComponent mappings
```

KB ne stvara ActivityTemplate i nije pravilo `1 KB = 1 template`. Jedan KB može imati više
templatea, a aktivnost može nastati bez templatea.

## 6. Duration envelope

Svaki Published version ima:

```text
1 <= MinDurationMinutes
MinDurationMinutes <= OptimalDurationMinutes
OptimalDurationMinutes <= MaxDurationMinutes
MaxDurationMinutes <= 480
```

To je pedagogical guidance, ne skup jedinih dopuštenih vrijednosti.
`LessonActivity.DurationMinutes` ostaje canonical planirano trajanje.

Ručna Teacher promjena nikad se automatski ne vraća na KB optimal. AI/regenerate ne smije
silent overwriteati ručno trajanje.

```text
Min <= activity duration <= Max  → normalno
activity duration < Min          → warning
activity duration > Max          → warning
```

Outside-envelope nije hard reject. Hard Phase 9.1 granica ostaje `1..480`.

## 7. Hard eligibility i recommendation

Pedagogical recommendation nije domain eligibility.

### Hard eligibility

Hard rule se koristi samo kad je koncept semantički nemoguć ili krši zaključani contract.
Primjeri:

- version nije current Published ili root nije Active;
- explicit group-only/individual-only participant rule nije zadovoljen;
- `RequiresHomeworkContext` nema homework context;
- Required assessment/task rule nije zadovoljen pri Ready potvrdi;
- Required predecessor nedostaje;
- composition ima cycle, depth ili parent/child violation.

Teacher ne može `ApplyAnyway` na hard-invalid candidate ili plan.

### Recommendation / warning

Primjeri warninga:

- duration izvan envelopea;
- grade/proficiency izvan preporučenog raspona;
- ponovljen activity type ili previše assessment blokova;
- group plan s previše individualnih aktivnosti;
- homework review bez recorded result kada context nije Required;
- Revision bez mnogo povijesnog Evidencea;
- Motivation block bez neutralnog engagement-support taga.

Teacher smije `ApplyAnyway` bez administrator approvala. V1 ne mora trajno auditirati svaki
warning override; budući product requirement to može dodati.

## 8. Typed applicability model

Arbitrary rule JSON, scripts i user-authored executable rules su zabranjeni. Version koristi
strukturirane, bounded rule skupove:

- Participant applicability;
- Grade applicability;
- Proficiency applicability;
- Goal applicability;
- Knowledge applicability;
- Homework applicability;
- Assessment applicability;
- Sequence applicability;
- Composition applicability.

Svako pravilo ima typed `RuleKind`, `Severity: Required | Recommended` i stable `ReasonCode`.
Persistence smije grupirati srodne typed relacije uz 3NF/queryability, ali ne koristiti jedan
generički `RuleType + JsonPayload` model za cijeli katalog.

## 9. Participant, grade i proficiency

Participant applicability:

```text
Individual | Group | Both
```

V1 default je `Both`, osim kada Published snapshot eksplicitno definira drugačije. Naziv
`Suradnički zadatak` sam po sebi ne stvara group-only pravilo.

Applicability nikad ne koristi spol, etnicitet, vjeru, zdravlje, socioekonomski status ili
Guardian podatke.

`MinSchoolGrade?`/`MaxSchoolGrade?` i `MinProficiencyLevel?`/`MaxProficiencyLevel?` su
defaultno Recommended granice. Teacher smije overrideati warning jer se sadržaj može
prilagoditi. Hard granica zahtijeva buduće eksplicitno sigurnosno/product opravdanje.

## 10. Goal i Knowledge applicability

KB može navesti canonical goal categories za koje je Recommended, ali deterministic server
ne radi hard-coded string parsing Teacher-authored cilja. AI smije rangirati prema cilju tek
nad server-approved eligible kandidatima.

Knowledge applicability smije koristiti `KnowledgeArea` ili postojeću canonical capability/
characteristic. Ako takav canonical atribut ne postoji, Phase 9.3 ga ne izmišlja.

System KB version ne nosi stotine konkretnih KnowledgeComponent ID-eva kao univerzalnu
istinu. Knowledge Modeli su versioned, a actual exact KC mapping pripada LessonActivityju.
KB klasifikacija nikada ne stvara automatski KC mapping niti Evidence authority.

## 11. Homework i assessment

Homework mode:

```text
NotRelevant | Optional | RequiresHomeworkContext
```

`RequiresHomeworkContext` bez contexta je hard ineligible. Concrete mode se publish-a samo
iz odobrenog KB version speca; sustav ne izmišlja homework podatke.

Assessment capability:

```text
NotAssessment | MayContainAssessment | AssessmentFocused
```

AssessmentFocused i dalje samo opisuje planiranu aktivnost. Ready validation zahtijeva validni
AssessableTaskVersion jedino ako Published rule to eksplicitno označi Required.

KB-006 i KB-016 se ne pretvaraju u Evidence emittere samo zbog naziva. Assessment block nije
Attempt ni Evidence; to ostaje budući Phase 10 runtime.

## 12. Sequence rules

Version smije definirati typed `PredecessorRecommendation` i `SuccessorRecommendation` s:

```text
Required | Recommended | Discouraged
```

Required se koristi rijetko i može blokirati Ready validation. Recommended/Discouraged daje
warning. Metadata ne preslaguje aktivnosti; server/AI smije predložiti ili upozoriti, a
Teacher potvrđuje konačni order.

Self-reference i sequence cycle koji bi stvorio nemogući Required graph su zabranjeni.

## 13. Composite blocks i nesting

```text
CompositionKind: Atomic | Composite
```

KB-024 Priča i KB-025 Misija mogu se objaviti kao Composite. Version može referencirati
dopuštene child KB versions kao template-level recommendation, ali publish ne generira
automatski LessonActivities.

Concrete plan koristi optional `ParentLessonActivityId` samo kada parentov exact KB version
podržava composition. V1 dopušta samo:

```text
parent
└── child
```

Maksimalna dubina je 2; grandchild nije dopušten. Activity i KB composition graph moraju biti
acyclic, activity ne može parentati samu sebe, a ancestor ne može postati child.

Parent composite duration je ukupno planirano vrijeme. Ready invariant:

```text
sum(child DurationMinutes) <= parent DurationMinutes
```

Manji zbroj ostavlja vrijeme za prijelaz/upute. Veći zbroj blokira Ready kao vremenski
kontradiktoran plan. Svaki child mora sam zadovoljiti hard eligibility.

## 14. Tri-state evaluation i explainability

Applicability rezultat je:

```text
Satisfied | NotSatisfied | Unknown
```

Missing se nikad ne tretira kao low/zero niti lažni false.

- Recommended + Unknown: nema hard rejectiona; može dati `insufficient data` objašnjenje.
- Required context rule + Unknown: Ineligible, npr. homework-required bez homework contexta.

Svaki eligibility rezultat ili warning ima stable reason code, primjerice:

- `KB_DURATION_BELOW_RECOMMENDED`;
- `KB_DURATION_ABOVE_RECOMMENDED`;
- `KB_HOMEWORK_CONTEXT_MISSING`;
- `KB_LEVEL_OUTSIDE_RECOMMENDED_RANGE`;
- `KB_PREDECESSOR_RECOMMENDED`;
- `KB_ASSESSMENT_TASK_REQUIRED`;
- `KB_COMPOSITION_DEPTH_EXCEEDED`;
- `KB_COMPOSITION_CYCLE`.

Frontend i AI ne izmišljaju razlog.

## 15. Deterministic authority i AI handoff

Server-side typed rules jedini su authority za Eligible/Ineligible i warnings:

```text
server applicability rules → eligibility + warnings
AI                         → ranking + suggestion + explanation
```

AI candidate set sadrži samo current Published version, Active root i hard-eligible
kandidate. Draft, Retired i hard-ineligible KB ne šalju se kao selectable kandidati. Warning
metadata smije se poslati za eligible candidate.

AI ne auto-selecta, reorderira, mijenja duration niti potvrđuje plan bez Teacher akcije.

## 16. Phase 9.2 precedence nad legacy draftovima

Ako stariji KB source traži ime/Guardian podatke, free-form notes, raw Evidence, sensitive
podatke, persistent motivation score ili Student-specific memory, to se ne prenosi u formalni
contract.

Legacy pojmovi mapiraju se ovako:

- `interes` → bounded explicit AI-shareable interest tag;
- `motivacija` → neutralni Teacher-set instructional-support indicator ili ordinary warning;
- `slabi rezultati` → bounded canonical readiness/Evidence summary;
- `AI pamti što radi za učenika` → zabranjeno; dopušten je samo opt-in Teacher-style profile
  iz Phase 9.2.

Raw notes, raw Evidence history i sensitive data ostaju zabranjeni. Ovi inputi su najčešće
Recommendation, ne hard eligibility.

## 17. Production catalog deployment

Seed/deployment je idempotentan i explicit:

- 25 stable KnowledgeBlock rootova;
- 25 initial v1 version redaka;
- canonical code i naziv iz roster tablice;
- KB-001–KB-005 Published v1;
- KB-006–KB-025 Draft v1.

Catalog se primjenjuje kroz migration ili explicit catalog deployment, ne startup upsert.
API runtime nije schema/catalog owner i ne mutira katalog pri svakom pokretanju.

Production update je kontrolirana release operacija: novi Draft, review, atomski retire/publish
i current-version update. Obični Lesson Builder API ne publish-a System katalog.

Ako source-of-truth prije seed implementacije formalno promijeni approval status, docs i seed
moraju se uskladiti; migration ne promovira draft heuristikom.

## 18. Historical readability

Stari LessonPlan prikazuje exact historical code, DisplayName, version number i Retired status.
UI ne zamjenjuje v1 naziv ili pravila trenutačnom v2 verzijom samo zato što root ima novi
current Published snapshot.

## 19. Persistence invarianti za buduću implementaciju

Kada se persistence uvede, DB/application sloj mora štititi najmanje:

- unique immutable KnowledgeBlock.Code;
- unique `(KnowledgeBlockId, VersionNumber)`;
- najviše jedan Draft i jedan current Published po rootu;
- root/current-version same-parent i status konzistenciju;
- Published/Retired snapshot immutability i restrictive delete;
- `1 <= Min <= Optimal <= Max <= 480`;
- sequence/composition no-self i acyclic graph;
- max activity nesting depth 2;
- exact KnowledgeBlockVersion reference;
- current Published/Active granicu za nove aktivnosti/templatee;
- historical Retired reference readability.

## 20. Scope granica i handoff

Phase 9.3 ne implementira:

- domain/EF/schema/seed runtime;
- Lesson Plan ili activity picker UI;
- AI/provider runtime ili automatic plan generation;
- balance engine, duration correction ili automatic reorder;
- Student-specific AI memory;
- Attempt, Evidence ili Session runtime;
- auto-KC mapping.

Phase 9.5 implementira duration/variety/KB distribution/sequence/coverage balance ponašanje.
Phase 9.4 može početi Goal Selection workflow nad zatvorenim 9.1–9.3 contractima, ali ne smije
preuranjeno implementirati cijeli balance engine ili runtime AI provider.
