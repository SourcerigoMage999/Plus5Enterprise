# Curriculum hierarchy

## Status

**LOCKED contract v1.0 — Phase 5.1 — 2026-09-16**

Ovaj dokument proširuje `CORE_TEACHING_FOUNDATION.md` isključivo modelom kurikularnih ishoda.
Ne odobrava niti uvozi produkcijski curriculum katalog i ne uvodi Knowledge/Evidence semantiku.

## Granica modela

- `Curriculum` ostaje globalni, verzionirani referentni korijen.
- `CurriculumOutcome` pripada točno jednoj konkretnoj `Curriculum` verziji.
- Outcome nije Teacher-owned i nema `TeacherAccountId`.
- Curriculum verzija je jedina temporalna/versioning granica u Phase 5.1.
- Promjena teksta, službenog koda, značenja ili hijerarhije stvara novu Curriculum verziju i
  novi outcome skup; postojeći redak se ne verzionira in-place.
- Nema `ValidFromUtc`/`ValidToUtc` na outcomeu.

## Model

| Polje | Pravilo |
|---|---|
| `Id` | obavezni stabilni PLUS 5 GUID |
| `CurriculumId` | obavezni restriktivni FK na konkretnu Curriculum verziju |
| `ParentOutcomeId` | nullable self-reference; root je `null`, child ima najviše jednog parenta |
| `SupersedesOutcomeId` | nullable povijesni FK na outcome druge verzije iste Curriculum obitelji |
| `OfficialCode` | nullable službeni identifikator, najviše 128 znakova; čuva objavljeni case i sadržaj |
| `SourceAuthority` | nullable authority/source, najviše 200 znakova |
| `SourceReference` | nullable dokument/verzija/URL/reference, najviše 1024 znaka |
| `Title` | obavezni trimani naslov, najviše 500 znakova |
| `Description` | nullable opis, najviše 4000 znakova |
| `SortOrder` | obavezni nenegativni redoslijed među sibling outcomeima |

## Hijerarhija

Koristi se adjacency-list self-reference bez hardkodiranog tipa ili maksimalne pedagoške
dubine:

```text
Curriculum
└── CurriculumOutcome (ParentOutcomeId = null)
    └── CurriculumOutcome (ParentOutcomeId = parent.Id)
```

Zaključane invarijante:

- parent pripada istom `CurriculumId`, odnosno istoj Curriculum verziji
- outcome ne može biti sam sebi parent
- ciklusi su zabranjeni
- child ima najviše jednog parenta; root može imati više djece
- dubina nije hardkodirana
- redoslijed se čita po `SortOrder`, zatim stabilnom `Id`; ne izvodi se iz naslova ili koda
- jednaki `SortOrder` siblinga dopušten je jer nije zaključana business jedinstvenost redoslijeda

Javni domenski konstruktor prima postojeći parent objekt i ne dopušta naknadnu promjenu
parenta, pa valjani domain flow ne može konstruirati ciklus. Baza dodatno koristi composite
same-Curriculum FK, self-parent CHECK i SQL trigger koji odbija proizvoljan višeredni ciklus.

## Službeni identitet i provenance

- `Id` je interni identitet i nikada se ne koristi kao službena šifra.
- `OfficialCode` se ne generira iz naslova, rednog broja ili GUID-a i ne normalizira se u drugi
  case; okolni whitespace nije dio valjanog koda.
- Ako odobreni authoritative source nema kod, `OfficialCode` ostaje `null`.
- Kada `OfficialCode` postoji, `SourceAuthority` i `SourceReference` su obavezni.
- `(CurriculumId, OfficialCode)` je jedinstven samo za retke koji imaju službeni kod.
- Phase 5.1 ne proglašava nijednu datoteku, URL ili screenshot authoritative izvorom.

## Version lineage

`SupersedesOutcomeId` je samo povijesna veza. Mora pokazivati na outcome iz iste Curriculum
obitelji (`Previous.Curriculum.Code == Current.Curriculum.Code`), ali druge verzije
(`Previous.Curriculum.Version != Current.Curriculum.Version`). Druga Curriculum obitelj, ista
verzija, self-reference i identity reuse nisu dopušteni. Veza može ostati `null` kada službeni
izvor ne daje pouzdanu lineage informaciju. Brisanje prethodnika je restriktivno.

## Persistence i query contract

- tablica je `CurriculumOutcomes`
- delete behavior prema Curriculumu, parentu i predecessor outcomeu je `Restrict`
- composite alternate key `(CurriculumId, Id)` podržava same-Curriculum parent FK
- filtrirani unique indeks štiti `(CurriculumId, OfficialCode)` kada code postoji
- `(CurriculumId, ParentOutcomeId, SortOrder, Id)` podržava determinističan read rootova/djece
- `SupersedesOutcomeId` ima lookup indeks
- CHECK constrainti štite sort, self-parent, self-supersession i minimalni code provenance
- SQL hierarchy trigger štiti arbitrary cycle te odbija same-version i cross-family
  supersession pri izravnom upisu
- nema seeda, backfilla, production kataloga ni importa

## Odnos prema Knowledge Modelu

`CurriculumOutcome` opisuje što službeni curriculum očekuje. Nije `KnowledgeComponent` i ne
sadrži mastery/readiness/evidence podatke. Phase 5.2 uvodi eksplicitni M:N mapping
`CurriculumOutcomeKnowledgeComponent`; detalji su u `KNOWLEDGE_COMPONENT_MODEL.md`.

## Izvan Phase 5.1

- produkcijski curriculum katalog i odabir authoritative sourcea
- scraping, DOCX/XLSX parser, ministry import ili ručni masovni unos
- import staging, approval, correction i reconciliation workflow
- Teacher custom curriculum/outcome authoring lifecycle
- `KnowledgeArea`, `KnowledgeComponent`, `KnowledgeModel` i outcome mapping
- mastery, readiness, evidence, weights, difficulty ili Student progress
- javni API, CRUD endpoint ili frontend ekran
