# Lesson Builder AI contract

## Status

**Phase 9.2 — FINAL LOCKED — odobreno 2026-10-30**

Ovaj dokument zaključava opcionalnu AI pomoć u Lesson Builderu prije Knowledge Block
kataloga i user-facing implementacije. Phase 9.2 je `DOCS_ONLY`: ne uvodi provider adapter,
persistence, API, UI ni vanjski AI poziv.

Sourceovi:

- `LESSON_BUILDER_DOMAIN.md`;
- `source_specs/5.0_Priprema_sata_Lesson_Builder.md` – `5.6_Potvrdena_priprema_Sat_je_spreman.md`;
- `source_specs/LESSON_BUILDER_KNOWLEDGE_BLOCK_CATALOG.md`;
- SA odluka za Phase 9.2 od 2026-10-30.

AI je isključivo bounded suggestion servis. Teacher ostaje authority, manualni Lesson Builder
uvijek radi, a AI nikada ne stvara runtime ili knowledge činjenice.

## 1. Provider, trošak i uključivanje

V1 provider contract:

```text
Provider: OpenAI API
API: Responses API
Model: gpt-6.1-sol
Region: EU
store: false
```

Application ovisi samo o provider-neutral portu `ILessonBuilderAiAssistant`. OpenAI adapter
pripada Infrastructure sloju. Model, endpoint, enabled flag, key i prompt template dolaze iz
validirane serverske konfiguracije; model ID nije domain/application konstanta.

Production-safe default je:

```text
LessonBuilderAi:Enabled = false
```

Kad je AI isključen nema provider poziva ni troška, a svi ručni workflowi ostaju dostupni.
PLUS 5 ne uvodi AI billing, credits, token purchase ni in-app naplatu.

Provider poziv nastaje samo nakon eksplicitne Teacher akcije. Nema poziva pri otvaranju
ekrana ili keystrokeu, background regenerationa, auto-refresha ni skrivenog prefetchinga.
Jedna akcija je jedan logički request uz najviše jedan dopušteni transient retry.

## 2. Provider capability boundary

V1 ne omogućuje:

- web ili file search;
- provider files, Threads, Assistants persistence ili vector stores;
- code interpreter, computer use, MCP ili arbitrary tool/function execution;
- background mode ili provider conversation memory;
- tihi fallback na drugi model.

Svaki request je stateless i prima samo strukturirani context koji sastavi PLUS 5 server.
Provider adapter nema samostalan pristup Studentima, bazi ni dodatnim izvorima.

## 3. PII i osjetljivi podaci

Provideru se ne šalju:

- StudentId, ime, e-mail, telefon, datum rođenja, adresa ili school identifier;
- Guardian identitet ili kontakt;
- Teacher-written osobno identificirajući ili drugi free-form notes;
- zdravstveni, dijagnostički, mental-health, disability, vjerski, etnički, seksualni,
  politički, obiteljski ili disciplinski podaci;
- credentials, cookie/token, storage key, signed URL ili binary.

Internal StudentId smije postojati u PLUS 5 auditu, ali ne u serialized provider payloadu.
Individualni request koristi samo request-local oznaku `Learner` ili `Learner-1`.

Group request može koristiti `Learner A`, `Learner B` i slične ephemeral pseudonime koji se
regeneriraju za svaki request. Ne šalju se imena, stabilni ID-evi, kontakti ni identitetski
redoslijed članova. Request-local oznaka nikada ne postaje canonical Student label u UI-ju.

Individualni request smije sadržavati samo podatke tog jednog Studenta. Više anonimnih learner
profila dopušteno je isključivo u group requestu. Server, ne samo prompt, provodi ovu granicu.

## 4. Dopušteni pedagoški context

Za individualni ili anonimni learner profil dopušteni su samo minimizirani, svrhoviti podatci:

- Program, SchoolGrade i ProficiencyLevel;
- target Knowledge Components;
- canonical readiness/mastery summary relevantnih KC-ova;
- bounded recent Evidence summary;
- bounded recent lesson focus summary;
- structured homework completion/performance summary;
- Teacher-defined instructional preferences;
- eksplicitno AI-shareable interest tagovi;
- neutralni structured engagement-support tagovi.

### Readiness i mastery

AI konzumira postojeći canonical Mastery/Readiness v1 output s confidence kontekstom. Ne
računa alternativni AI mastery score iz raw Evidencea i ne zamjenjuje canonical projekciju.
Missing nije `0` ni `Low`.

### Evidence

Raw EvidenceEvent, EvidenceEventId, SourceId, puna provenance i puna povijest zabranjeni su.
Server smije poslati izvedeni summary samo za relevantne KC-ove:

```text
KnowledgeComponent
RecentEvidenceCount
RecentEvidenceTypes
RecentPerformanceSummary
RecentAssistanceSummary
LastObservedAtRelative
```

V1 granica je zadnjih 30 dana i najviše 10 summarized items/aggregates po learneru. Veći skup
server agregira prije provider poziva.

### Prethodni sati

Dopušten je structured summary najviše pet relevantnih prethodnih satova: covered KC,
planned-vs-completed aktivnosti, duration summary i kratki Teacher-confirmed outcome tag.
Puni Lesson Plan history i Teacher notes se ne šalju.

### Homework

Dopušteni su samo structured completion state, relevantni KC, canonical bounded rezultat i
canonical assistance context. Student free text, uploaded file, scan/photo i privatni Teacher
komentar se ne šalju.

### Interesi i instructional support

Interes je dopušten samo kao bounded Teacher-maintained tag eksplicitno označen
AI-shareable. Ne izvodi se automatski iz Evidencea ili bilješki.

Persistent subjective `Motivation = Low` ili motivation score je zabranjen. Dopušteni su
neutralni, konkretni Teacher-set instructional tagovi poput `NeedsShorterActivities`,
`BenefitsFromVariety` i `BenefitsFromFrequentChecks`.

Generic/free-form Teacher notes nisu AI input. Buduća AI-shareable note zahtijeva zasebno,
strukturirano polje i novi privacy contract.

## 5. Group fairness i leakage zaštita

Group context sadrži group aggregate i bounded anonymous learner profilee. Plan mora uzeti u
obzir ciljanu razinu grupe, potrebu za scaffoldom i extension za naprednije učenike; ne smije
optimizirati samo prosjek ni izmišljati fairness score.

Provider output ne smije uspoređivati stvarne identitete niti otkrivati podatke jednog učenika
drugome. Dopušten je Teacher-only anonymous razlog poput `Learner B treba dodatni scaffold`,
ali user-facing sadržaj koristi neutralnu formulaciju poput `Nekim učenicima treba dodatni
scaffold`.

Response se ne smije canonicalno povezati s pogrešnim Studentom. Pseudonym mapping je
request-local server state i ne postaje provider memory ili trajni profil.

## 6. Provider data policy i retention

Production OpenAI project mora imati data-sharing/training opt-in isključen, `store=false` i
EU processing. Zero Data Retention se preferira kada je dostupan, ali funkcionalnost ne smije
ovisiti o dostupnosti ZDR-a. Standardni provider abuse-monitoring retention može trajati do
30 dana kada stroži ZDR/Modified Abuse Monitoring ugovor nije dostupan; PLUS 5 zato dodatno
minimizira svaki request i ne šalje identitete. Provider-side persistence i file storage su
zabranjeni.

Normalni PLUS 5 log ne sadrži prompt, provider response, learner profile, Teacher free text,
interese, readiness/evidence payload ni API key. Dopušten operational metadata:

- AiRequestId, TeacherId, LessonPlanId i LessonPlanVersionId;
- SuggestionKind, provider, model i PromptTemplateVersion;
- duration, input/output token count te success/failure category.

## 7. Server-side context assembler

`ILessonBuilderAiContextBuilder` je jedini application boundary koji sastavlja provider
context. Mora provoditi:

- PII i sensitive-data stripping;
- single-learner ili group pseudonym scope;
- relevant-KC filter;
- Evidence agregaciju i vremenski/item limit;
- previous-lesson/homework granice;
- notes exclusion i interest allowlist;
- payload-size budget.

Provider adapter prima već sastavljen bounded DTO i ne dohvaća domenske podatke.

Maksimalni application-built provider payload je `64 KiB UTF-8`.

## 8. Dopušteni suggestion use caseovi

AI smije predlagati:

- refinement cilja sata;
- activity sequence i duration distribution;
- ActivityTemplate i KnowledgeBlock classification;
- instructional KnowledgeComponent focus;
- postojeći eligible MaterialVersion;
- postojeći eligible AssessableTaskVersion / Quick Check;
- novi textual activity draft;
- differentiation.

Material/Task ID smije biti izabran samo iz bounded server-provided eligible kandidata. AI ne
smije izmišljati ID. Canonical write ponovno provjerava ownership/use permission, current
Active/Clean Material granicu, exact version i domenske invariante.

Generated textual content ostaje draft unutar LessonActivityja. Ne stvara automatski novi
Material. Teacher ga kasnije može ručno prenijeti kroz zasebni dopušteni workflow.

V1 nema image, PDF, PPTX, audio, video ili drugi binary generation/upload/return. Suggestion
content je privremeni SQL application data, ne object-storage file.

PLUS 5 ne tvrdi da je AI output garantirano originalan ili bez copyright rizika i ne zapisuje
implicitni `CopyrightOwner = Teacher`. Teacher mora pregledati sadržaj prije uporabe ili
distribucije; postojeća prava/licence vanjskih i bundled materijala ostaju mjerodavna.

## 9. Structured output i explainability

Svaki use case ima vlastiti versioned strict schema contract, primjerice:

- `LessonPlanSuggestionV1`;
- `ActivitySuggestionV1`;
- `DifferentiationSuggestionV1`;
- `KnowledgeMappingSuggestionV1`;
- `MaterialSuggestionV1`.

Unknown property ili schema version se odbija. Raw arbitrary JSON nije domain authority.

Svaka važna preporuka mora imati strukturirano objašnjenje:

```text
reasonCodes[]
supportingContext[]
missingData[]
cautions[]
```

Objašnjenje smije koristiti samo podatke stvarno poslane u requestu i ne smije izmišljati
learner preference ili fact. `Missing` se razlikuje od `Low`/`0`. Ne traži se niti sprema
hidden chain-of-thought; Teacher dobiva kratko user-facing objašnjenje.

Provenance bilježi korištene kategorije konteksta, provider, model, PromptTemplateVersion i
GeneratedAtUtc, bez originalnog prompta. Teacher može vidjeti je li recommendation koristio
readiness, Evidence summary, prethodne sate ili drugi dopušteni context.

## 10. Moderation

Teacher free-text AI instruction i generated textual output prolaze odgovarajući moderation
signal preko `omni-moderation-latest` uz EU processing prije prikaza/canonical writea. Jasno
flagged relevantna hard-safety kategorija blokira prikaz i write; unsafe raw output ne
prikazuje se ni Teacher accountu.

Neutralna poruka upućuje Teachera da nastavi ručno ili pokuša s drugačijim zahtjevom.
Benigni obrazovni kontekst osjetljivih tema ne proglašava se automatski business-invalidnim;
v1 policy mapira hard-safety kategorije, a ostalo ostavlja provider safetyju i Teacher reviewu.
Moderation nije pedagoški authority.

## 11. Suggestion persistence i lifecycle

Minimalni `LessonBuilderAiSuggestion` contract:

```text
Id
TeacherId
LessonPlanId
LessonPlanVersionId
SuggestionKind
Provider
Model
PromptTemplateVersion
UsedContextCategories
CreatedAtUtc
ExpiresAtUtc
ResolvedAtUtc?
Status
GeneratedContentJson?
ExplanationJson?
```

Lifecycle:

```text
Pending
→ Accepted | Edited | Rejected | Expired | Replaced
Accepted | Edited
→ Undone  # audit state; undo je normalna canonical mutation
```

Pending content traje najviše 24 sata, zatim postaje Expired i briše se. Rejected content se
briše odmah. Nakon Accepted/Edited canonical sadržaj postoji u LessonPlanVersionu, raw
generated content se briše, a ostaju minimalni provenance metadata i bounded explanation.
Raw provider request/response ne čuva se dugoročno.

Regenerate uvijek stvara novi suggestion ID i troši novu kvotu. Prethodni Pending prelazi u
`Replaced`; nema silent overwritea.

## 12. Teacher confirmation i concurrency

Tok je:

```text
provider response
→ schema/domain/moderation validation
→ Pending suggestion
→ Teacher review
→ Accept | Edit | Reject
→ canonical write tek nakon eksplicitne potvrde
```

AI nikada ne piše izravno u DbContext ili LessonPlanVersion. Accept/Edit šalje aktualni
`LessonPlanVersion.RowVersion` i ponovno provjerava owner, target, schema i sve domain
invariante. Ako se plan promijenio nakon generiranja, vraća se `409 concurrency_conflict` i
Teacher mora pregledati/regenerirati suggestion.

Undo nije AI poziv. To je normalna, rowversion-safe Draft mutation koja vraća prethodnu
canonical vrijednost ako je još sigurno. Audit čuva slijed request → Accept/Edit → canonical
write → Undo.

Nijedna AI odluka ne smije automatski reorderati plan, zamijeniti material, promijeniti KC,
trajanje ili Ready status.

## 13. Lokalno učenje iz Teacher odluka

Phase 9.2 `AI uči iz odluka` znači isključivo lokalne Teacher-style preference signale iz
Accept/Edit/Reject događaja. Ne znači provider fine-tuning, OpenAI training feedback,
Student-specific persistent model, embedding profil, hidden adaptive model ili Student memory.

Dopuštene Teacher preference kategorije uključuju activity duration/type, količinu Quick
Checkova, individual/pair/group balance, explanation depth i exercise count. Zabranjena je
Student-specific procjena poput `Teacher smatra Student X slabim`.

Default je:

```text
UseMyAiFeedback = false
```

Teacher ga mora eksplicitno uključiti, može ga isključiti i resetirati learned preferences.
Isključivanje odmah prestaje koristiti profil u novim requestovima. Budući Student-specific
learning zahtijeva novu privacy/legal/consent odluku. Ovaj Teacher-only preference model ne
uvodi zaseban Student consent flow jer ne sadrži Student profile ni osobnu povijest učenika.

## 14. Audit

Audit mora odgovoriti:

- tko je zatražio suggestion;
- za koji LessonPlan i LessonPlanVersion;
- kada, kojim providerom/modelom i prompt-template verzijom;
- koje kategorije inputa su korištene;
- je li rezultat Accepted, Edited, Rejected, Expired, Replaced ili Undone;
- kada je canonical mutation napravljena.

Audit ne mora i ne smije čuvati raw prompt, raw response ni sensitive learner context.

## 15. Operational granice

Dedicated v1 limit po Teacheru:

```text
10 requestova / 10 minuta
50 requestova / dan
```

Provider timeout je najviše 30 sekundi. Dopušten je najviše jedan retry samo za transient
network problem ili provider 5xx. Nema automatskog retryja za 400, 401/403, 429, malformed
structured output, moderation block ili domain-invalid output.

Timeout/failure ne stvara suggestion ni canonical promjenu. Teacher dobiva AI unavailable /
suggestion failed stanje i nastavlja ručno. AI failure nikada ne blokira Lesson Builder.

## 16. Authority i zabranjene runtime činjenice

AI ne smije:

- stvoriti ili pokrenuti LessonSession;
- označiti Session Held ili attendance;
- stvoriti StudentAttempt ili EvidenceEvent;
- promijeniti Mastery/Readiness;
- ocijeniti učenika;
- automatski potvrditi Ready plan;
- direct-writeati bilo koji aggregate.

`ILessonBuilderAiAssistant` vraća `AiSuggestion<T>` application rezultatu. Canonical write
prolazi kroz postojeće typed domain/application servise nakon Teacher potvrde.

## 17. Acceptance zahtjevi buduće implementacije

Kada se AI runtime implementira, dedicated testovi moraju provjeriti actual serialized
provider payload i dokazati odsutnost najmanje:

- StudentId, Student name/e-mail i Guardian podataka;
- drugog učenika u individualnom requestu;
- raw EvidenceEventId i Teacher notes;
- binary/storage identifikatora;
- PII u explanationu.

Group testovi moraju potvrditi request-local pseudonime, server-enforced scope i zabranu
canonicalnog povezivanja outputa s pogrešnim Studentom. Testovi moraju pokriti retention,
confirmation, concurrency, regenerate/replace, undo audit, moderation i cost limits.

## 18. Izvan Phase 9.2

Nisu implementirani niti dopušteni preuranjeno:

- provider port/adapter, configuration, persistence, API ili UI;
- Student-specific AI memory/profiling ili provider fine-tuning;
- raw notes, sensitive/Guardian data ili raw homework files;
- web/tool/agent pristup i provider/binary storage;
- AI-generated binary assets;
- autonomous grading, publish, Ready transition ili lesson start;
- Attempt/Evidence emission i Mastery/Readiness mutation;
- Student-facing AI.

## 19. Handoff

Phase 9.3 sada smije formalizirati versioned KB-001–KB-025 katalog i applicability pravila
nad zaključanim 9.1 domain i 9.2 AI/privacy boundaryjima. Runtime AI implementira se samo u
podfazi koja ga eksplicitno zahtijeva i mora ostati default-disabled, suggestion-only i bez
obavezne naplate.
